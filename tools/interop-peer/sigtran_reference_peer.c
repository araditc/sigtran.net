#define _POSIX_C_SOURCE 200809L

/*
 * Independent SCTP/M3UA/SCCP/TCAP/MAP SMS reference peer used by the
 * end-to-end interoperability lab. This program does not link to Sigtran.NET.
 * It parses each protocol envelope and creates its own response bytes.
 */

#include <arpa/inet.h>
#include <errno.h>
#include <netinet/in.h>
#include <netinet/sctp.h>
#include <signal.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/socket.h>
#include <sys/time.h>
#include <time.h>
#include <unistd.h>

#define BUFFER_SIZE 65535
#define M3UA_PPID 3U
#define M3UA_CLASS_TRANSFER 1U
#define M3UA_CLASS_ASPSM 3U
#define M3UA_CLASS_ASPTM 4U
#define M3UA_TYPE_DATA 1U
#define M3UA_TAG_ROUTING_CONTEXT 0x0006U
#define M3UA_TAG_PROTOCOL_DATA 0x0210U
#define REFERENCE_CONTEXT 100U

struct tlv_view {
    uint8_t tag;
    const uint8_t *value;
    size_t value_length;
    size_t total_length;
};

struct protocol_data_view {
    uint32_t routing_context;
    int has_context;
    uint32_t opc;
    uint32_t dpc;
    uint8_t service_indicator;
    uint8_t network_indicator;
    uint8_t message_priority;
    uint8_t signalling_link_selection;
    const uint8_t *payload;
    size_t payload_length;
};

struct sccp_udt_view {
    uint8_t protocol_class;
    const uint8_t *called_party;
    size_t called_party_length;
    const uint8_t *calling_party;
    size_t calling_party_length;
    const uint8_t *user_data;
    size_t user_data_length;
};

struct tcap_invoke_view {
    const uint8_t *originating_transaction_id;
    size_t originating_transaction_id_length;
    uint8_t invoke_id;
    uint8_t operation_code;
    const uint8_t *application_context;
    size_t application_context_length;
    const uint8_t *parameters;
    size_t parameters_length;
};

static volatile sig_atomic_t stop_requested = 0;
static int quiet_traffic = 0;
static int reference_profile = 0;
static int wrong_activation_context = 0;
static int asp_client = 0;
static uint32_t reference_context = REFERENCE_CONTEXT;

enum map_reply { REPLY_RESULT, REPLY_ERROR, REPLY_REJECT, REPLY_MALFORMED, REPLY_SILENCE };

/* Fixed synthetic cases, in the order exercised by the local host driver. */
static const struct { uint8_t code; enum map_reply reply; } reference_cases[] = {
    {45U, REPLY_RESULT}, {46U, REPLY_RESULT}, {44U, REPLY_RESULT}, {47U, REPLY_RESULT}, {64U, REPLY_RESULT},
    {44U, REPLY_ERROR}, {64U, REPLY_REJECT}, {45U, REPLY_MALFORMED}, {47U, REPLY_SILENCE}, {64U, REPLY_RESULT}
};

static void handle_signal(int signal_number)
{
    (void)signal_number;
    stop_requested = 1;
}

static uint16_t read_u16_be(const uint8_t *value)
{
    return (uint16_t)(((uint16_t)value[0] << 8) | value[1]);
}

static uint32_t read_u32_be(const uint8_t *value)
{
    uint32_t result;
    memcpy(&result, value, sizeof(result));
    return ntohl(result);
}

static void write_u16_be(uint8_t *destination, uint16_t value)
{
    destination[0] = (uint8_t)(value >> 8);
    destination[1] = (uint8_t)value;
}

static void write_u32_be(uint8_t *destination, uint32_t value)
{
    uint32_t encoded = htonl(value);
    memcpy(destination, &encoded, sizeof(encoded));
}

static void log_event(const char *event_name, const char *detail)
{
    if (quiet_traffic
        && (strcmp(event_name, "sctp-receive") == 0
            || strcmp(event_name, "m3ua-receive") == 0
            || strcmp(event_name, "map-invoke") == 0
            || strcmp(event_name, "map-result") == 0
            || strcmp(event_name, "heartbeat-ack") == 0)) {
        return;
    }

    time_t now = time(NULL);
    struct tm value;
    char timestamp[32];
    gmtime_r(&now, &value);
    strftime(timestamp, sizeof(timestamp), "%Y-%m-%dT%H:%M:%SZ", &value);
    printf("%s event=%s %s\n", timestamp, event_name, detail);
    fflush(stdout);
}

static int read_ber_tlv(
    const uint8_t *data,
    size_t data_length,
    struct tlv_view *result)
{
    size_t value_length;
    size_t header_length;

    if (data_length < 2U) {
        return -1;
    }

    if ((data[1] & 0x80U) == 0U) {
        value_length = data[1];
        header_length = 2U;
    } else if (data[1] == 0x81U) {
        if (data_length < 3U) {
            return -1;
        }
        value_length = data[2];
        header_length = 3U;
    } else if (data[1] == 0x82U) {
        if (data_length < 4U) {
            return -1;
        }
        value_length = read_u16_be(data + 2U);
        header_length = 4U;
    } else {
        return -1;
    }

    if (header_length + value_length > data_length) {
        return -1;
    }

    result->tag = data[0];
    result->value = data + header_length;
    result->value_length = value_length;
    result->total_length = header_length + value_length;
    return 0;
}

static int append_ber_tlv(
    uint8_t *destination,
    size_t capacity,
    size_t *offset,
    uint8_t tag,
    const uint8_t *value,
    size_t value_length)
{
    size_t header_length;

    if (value_length < 0x80U) {
        header_length = 2U;
    } else if (value_length <= 0xFFU) {
        header_length = 3U;
    } else if (value_length <= 0xFFFFU) {
        header_length = 4U;
    } else {
        return -1;
    }

    if (*offset + header_length + value_length > capacity) {
        return -1;
    }

    destination[(*offset)++] = tag;
    if (header_length == 2U) {
        destination[(*offset)++] = (uint8_t)value_length;
    } else if (header_length == 3U) {
        destination[(*offset)++] = 0x81U;
        destination[(*offset)++] = (uint8_t)value_length;
    } else {
        destination[(*offset)++] = 0x82U;
        write_u16_be(destination + *offset, (uint16_t)value_length);
        *offset += 2U;
    }

    if (value_length > 0U) {
        memcpy(destination + *offset, value, value_length);
        *offset += value_length;
    }
    return 0;
}

static int parse_m3ua_protocol_data(
    const uint8_t *message,
    size_t message_length,
    struct protocol_data_view *result)
{
    size_t offset = 8U;
    int found_data = 0;
    memset(result, 0, sizeof(*result));

    if (message_length < 8U || message[0] != 1U
        || message[2] != M3UA_CLASS_TRANSFER
        || message[3] != M3UA_TYPE_DATA
        || read_u32_be(message + 4U) != message_length) {
        return -1;
    }

    while (offset + 4U <= message_length) {
        uint16_t tag = read_u16_be(message + offset);
        uint16_t parameter_length = read_u16_be(message + offset + 2U);
        size_t padded_length;
        const uint8_t *value;
        size_t value_length;

        if (parameter_length < 4U
            || offset + parameter_length > message_length) {
            return -1;
        }

        value = message + offset + 4U;
        value_length = parameter_length - 4U;
        if (tag == M3UA_TAG_ROUTING_CONTEXT) {
            if (value_length != 4U || result->has_context) {
                return -1;
            }
            result->routing_context = read_u32_be(value);
            result->has_context = 1;
        } else if (tag == M3UA_TAG_PROTOCOL_DATA) {
            if (value_length < 12U || found_data) {
                return -1;
            }

            result->opc = read_u32_be(value);
            result->dpc = read_u32_be(value + 4U);
            result->service_indicator = value[8];
            result->network_indicator = value[9];
            result->message_priority = value[10];
            result->signalling_link_selection = value[11];
            result->payload = value + 12U;
            result->payload_length = value_length - 12U;
            found_data = 1;
        }

        padded_length = (parameter_length + 3U) & ~3U;
        offset += padded_length;
    }

    return found_data && offset == message_length ? 0 : -1;
}

static int read_sccp_variable(
    const uint8_t *message,
    size_t message_length,
    size_t pointer_index,
    const uint8_t **value,
    size_t *value_length)
{
    size_t start;
    if (pointer_index >= message_length) {
        return -1;
    }

    start = pointer_index + message[pointer_index];
    if (start >= message_length
        || start + 1U + message[start] > message_length) {
        return -1;
    }

    *value_length = message[start];
    *value = message + start + 1U;
    return 0;
}

static int parse_sccp_udt(
    const uint8_t *message,
    size_t message_length,
    struct sccp_udt_view *result)
{
    if (message_length < 8U || message[0] != 0x09U) {
        return -1;
    }

    result->protocol_class = message[1];
    if (read_sccp_variable(
            message,
            message_length,
            2U,
            &result->called_party,
            &result->called_party_length) != 0
        || read_sccp_variable(
            message,
            message_length,
            3U,
            &result->calling_party,
            &result->calling_party_length) != 0
        || read_sccp_variable(
            message,
            message_length,
            4U,
            &result->user_data,
            &result->user_data_length) != 0) {
        return -1;
    }

    return 0;
}

/* Q.773 structured dialogue abstract syntax, encoded as an OID TLV. */
static const uint8_t dialogue_oid[] = {0x06U, 0x07U, 0x00U, 0x11U, 0x86U, 0x05U, 0x01U, 0x01U, 0x01U};

static int parse_dialogue_context(const struct tlv_view *dialogue, struct tcap_invoke_view *result)
{
    struct tlv_view external, single, aarq, context, oid;
    if (read_ber_tlv(dialogue->value, dialogue->value_length, &external) != 0
        || external.tag != 0x28U || external.total_length != dialogue->value_length
        || external.value_length <= sizeof(dialogue_oid)
        || memcmp(external.value, dialogue_oid, sizeof(dialogue_oid)) != 0
        || read_ber_tlv(external.value + sizeof(dialogue_oid), external.value_length - sizeof(dialogue_oid), &single) != 0
        || single.tag != 0xA0U || single.total_length != external.value_length - sizeof(dialogue_oid)
        || read_ber_tlv(single.value, single.value_length, &aarq) != 0
        || aarq.tag != 0x60U || aarq.total_length != single.value_length
        || read_ber_tlv(aarq.value, aarq.value_length, &context) != 0
        || context.tag != 0xA1U || context.total_length != aarq.value_length
        || read_ber_tlv(context.value, context.value_length, &oid) != 0
        || oid.tag != 0x06U || oid.value_length == 0U || oid.total_length != context.value_length) {
        return -1;
    }

    result->application_context = aarq.value;
    result->application_context_length = context.total_length;
    return 0;
}

static int parse_tcap_invoke(
    const uint8_t *message,
    size_t message_length,
    struct tcap_invoke_view *result)
{
    struct tlv_view transaction;
    size_t offset = 0U;
    const uint8_t *component = NULL;
    size_t component_length = 0U;
    struct tlv_view invoke;
    struct tlv_view field;

    memset(result, 0, sizeof(*result));
    if (read_ber_tlv(message, message_length, &transaction) != 0
        || transaction.tag != 0x62U
        || transaction.total_length != message_length) {
        return -1;
    }

    while (offset < transaction.value_length) {
        struct tlv_view item;
        if (read_ber_tlv(
                transaction.value + offset,
                transaction.value_length - offset,
                &item) != 0) {
            return -1;
        }

        /* Q.773 assigns transaction IDs and the component portion to the application class. */
        if (item.tag == 0x48U) {
            result->originating_transaction_id = item.value;
            result->originating_transaction_id_length = item.value_length;
        } else if (item.tag == 0x6BU) {
            if (result->application_context != NULL || parse_dialogue_context(&item, result) != 0) {
                return -1;
            }
        } else if (item.tag == 0x6CU) {
            component = item.value;
            component_length = item.value_length;
        }
        offset += item.total_length;
    }

    if (result->originating_transaction_id == NULL || component == NULL
        || read_ber_tlv(component, component_length, &invoke) != 0
        || invoke.tag != 0xA1U) {
        return -1;
    }

    offset = 0U;
    if (read_ber_tlv(
            invoke.value + offset,
            invoke.value_length - offset,
            &field) != 0
        || field.tag != 0x02U
        || field.value_length != 1U) {
        return -1;
    }
    result->invoke_id = field.value[0];
    offset += field.total_length;

    if (read_ber_tlv(
            invoke.value + offset,
            invoke.value_length - offset,
            &field) != 0
        || field.tag != 0x02U
        || field.value_length != 1U) {
        return -1;
    }
    result->operation_code = field.value[0];
    offset += field.total_length;

    if (read_ber_tlv(
            invoke.value + offset,
            invoke.value_length - offset,
            &field) != 0
        || field.tag != 0x30U
        || field.total_length != invoke.value_length - offset) {
        return -1;
    }
    result->parameters = field.value;
    result->parameters_length = field.value_length;
    return 0;
}

static int validate_map_parameters(const struct tcap_invoke_view *invoke)
{
    size_t offset = 0U;
    uint8_t tags[3];
    size_t required_count = 3U;

    /* Each supported TS 29.002 argument has an ordered mandatory field list. */
    switch (invoke->operation_code) {
    case 44U:
        tags[0] = 0x80U; tags[1] = 0x84U; tags[2] = 0x04U;
        break;
    case 46U:
        tags[0] = 0x84U; tags[1] = 0x82U; tags[2] = 0x04U;
        break;
    case 45U:
        tags[0] = 0x80U; tags[1] = 0x81U; tags[2] = 0x82U;
        break;
    case 47U:
        tags[0] = 0x04U; tags[1] = 0x04U; tags[2] = 0x0AU;
        break;
    case 64U:
        tags[0] = 0x04U; tags[1] = 0x04U; required_count = 2U;
        break;
    default:
        return -1;
    }

    for (size_t index = 0U; index < required_count; index++) {
        struct tlv_view parameter;
        if (read_ber_tlv(
                invoke->parameters + offset,
                invoke->parameters_length - offset,
                &parameter) != 0
            || parameter.tag != tags[index] || parameter.value_length == 0U) {
            return -1;
        }

        if ((invoke->operation_code == 45U && index == 1U && parameter.value_length != 1U)
            || (invoke->operation_code == 47U && index == 2U
                && (parameter.value_length != 1U || parameter.value[0] > 2U))
            || ((invoke->operation_code == 44U || invoke->operation_code == 46U)
                && index == 2U && parameter.value_length > 200U)) {
            return -1;
        }
        offset += parameter.total_length;
    }

    if (invoke->operation_code == 45U && invoke->parameters_length - offset == 2U
        && invoke->parameters[offset] == 0x87U && invoke->parameters[offset + 1U] == 0U) {
        offset += 2U;
    }
    return offset == invoke->parameters_length ? 0 : -1;
}

static int append_dialogue_response(const struct tcap_invoke_view *invoke,
    uint8_t *destination, size_t capacity, size_t *offset)
{
    uint8_t aare_content[128], aare[132], external_content[145], external[149];
    const uint8_t accepted[] = {0xA2U, 3U, 2U, 1U, 0U, 0xA3U, 5U, 0xA1U, 3U, 2U, 1U, 0U};
    size_t aare_length = 0U, external_content_length = sizeof(dialogue_oid), external_length = 0U;
    size_t context_length = invoke->application_context_length;
    if (context_length == 0U) {
        return 0;
    }
    if (context_length > sizeof(aare_content) - sizeof(accepted)) {
        return -1;
    }
    memcpy(aare_content, invoke->application_context, context_length);
    memcpy(aare_content + context_length, accepted, sizeof(accepted));
    memcpy(external_content, dialogue_oid, sizeof(dialogue_oid));
    if (append_ber_tlv(aare, sizeof(aare), &aare_length, 0x61U, aare_content, context_length + sizeof(accepted)) != 0
        || append_ber_tlv(external_content, sizeof(external_content), &external_content_length, 0xA0U, aare, aare_length) != 0
        || append_ber_tlv(external, sizeof(external), &external_length, 0x28U, external_content, external_content_length) != 0) {
        return -1;
    }

    return append_ber_tlv(destination, capacity, offset, 0x6BU, external, external_length);
}

static int build_tcap_result(
    const struct tcap_invoke_view *invoke,
    enum map_reply reply,
    uint8_t *destination,
    size_t capacity,
    size_t *written)
{
    uint8_t component_content[512];
    uint8_t component[520];
    uint8_t transaction_content[1024];
    size_t component_content_length = 0U;
    size_t component_length = 0U;
    size_t transaction_content_length = 0U;
    uint8_t component_tag = reply == REPLY_ERROR ? 0xA3U : reply == REPLY_REJECT ? 0xA4U : 0xA2U;
    const uint8_t absent_subscriber[] = {0x02U, 1U, 6U};
    const uint8_t unrecognized_operation[] = {0x81U, 1U, 1U};
    /* The optional result SEQUENCE contains the local opcode and the encoded result. */
    const uint8_t routing_result[] = {
        0x02U, 0x01U, 45U, 0x30U, 0x12U,
        0x04U, 0x08U, 0x34U, 0x12U, 0x90U, 0x78U, 0x56U, 0x34U, 0x12U, 0xF0U,
        0xA0U, 0x06U, 0x81U, 0x04U, 0x91U, 0x44U, 0x21U, 0x43U
    };

    if (append_ber_tlv(
            component_content,
            sizeof(component_content),
            &component_content_length,
            0x02U,
            &invoke->invoke_id,
            1U) != 0
        || (reply == REPLY_RESULT && invoke->operation_code == 45U && append_ber_tlv(
            component_content,
            sizeof(component_content),
            &component_content_length,
            0x30U,
            routing_result,
            sizeof(routing_result)) != 0)) {
        return -1;
    }

    if (reply == REPLY_ERROR) {
        memcpy(component_content + component_content_length, absent_subscriber, sizeof(absent_subscriber));
        component_content_length += sizeof(absent_subscriber);
    } else if (reply == REPLY_REJECT) {
        memcpy(component_content + component_content_length, unrecognized_operation, sizeof(unrecognized_operation));
        component_content_length += sizeof(unrecognized_operation);
    }

    if (append_ber_tlv(
            component,
            sizeof(component),
            &component_length,
            component_tag,
            component_content,
            component_content_length) != 0
        || append_ber_tlv(
            transaction_content,
            sizeof(transaction_content),
            &transaction_content_length,
            0x49U,
            invoke->originating_transaction_id,
            invoke->originating_transaction_id_length) != 0
        || append_dialogue_response(invoke, transaction_content, sizeof(transaction_content), &transaction_content_length) != 0
        || append_ber_tlv(
            transaction_content,
            sizeof(transaction_content),
            &transaction_content_length,
            0x6CU,
            component,
            component_length) != 0) {
        return -1;
    }

    *written = 0U;
    return append_ber_tlv(
        destination,
        capacity,
        written,
        0x64U,
        transaction_content,
        transaction_content_length);
}

static int build_sccp_udt_response(
    const struct sccp_udt_view *request,
    const uint8_t *user_data,
    size_t user_data_length,
    uint8_t *destination,
    size_t capacity,
    size_t *written)
{
    size_t required = 5U
        + 1U + request->calling_party_length
        + 1U + request->called_party_length
        + 1U + user_data_length;
    size_t offset = 5U;

    if (required > capacity || required > 255U
        || request->called_party_length > 255U
        || request->calling_party_length > 255U
        || user_data_length > 255U) {
        return -1;
    }

    destination[0] = 0x09U;
    destination[1] = request->protocol_class;
    destination[2] = 3U;
    destination[3] =
        (uint8_t)(3U + request->calling_party_length);
    destination[4] =
        (uint8_t)(3U + request->calling_party_length
            + request->called_party_length);
    destination[offset++] = (uint8_t)request->calling_party_length;
    memcpy(
        destination + offset,
        request->calling_party,
        request->calling_party_length);
    offset += request->calling_party_length;
    destination[offset++] = (uint8_t)request->called_party_length;
    memcpy(
        destination + offset,
        request->called_party,
        request->called_party_length);
    offset += request->called_party_length;
    destination[offset++] = (uint8_t)user_data_length;
    memcpy(destination + offset, user_data, user_data_length);
    offset += user_data_length;
    *written = offset;
    return 0;
}

static int build_m3ua_data_response(
    const struct protocol_data_view *request,
    const uint8_t *payload,
    size_t payload_length,
    uint8_t *destination,
    size_t capacity,
    size_t *written)
{
    size_t parameter_value_length = 12U + payload_length;
    size_t parameter_length = 4U + parameter_value_length;
    size_t padded_parameter_length = (parameter_length + 3U) & ~3U;
    size_t protocol_offset = request->has_context ? 16U : 8U;
    size_t message_length = protocol_offset + padded_parameter_length;
    uint8_t *value;

    if (message_length > capacity || parameter_length > 0xFFFFU) {
        return -1;
    }

    memset(destination, 0, message_length);
    destination[0] = 1U;
    destination[2] = M3UA_CLASS_TRANSFER;
    destination[3] = M3UA_TYPE_DATA;
    write_u32_be(destination + 4U, (uint32_t)message_length);
    if (request->has_context) {
        write_u16_be(destination + 8U, M3UA_TAG_ROUTING_CONTEXT);
        write_u16_be(destination + 10U, 8U);
        write_u32_be(destination + 12U, request->routing_context);
    }
    write_u16_be(destination + protocol_offset, M3UA_TAG_PROTOCOL_DATA);
    write_u16_be(destination + protocol_offset + 2U, (uint16_t)parameter_length);
    value = destination + protocol_offset + 4U;
    write_u32_be(value, request->dpc);
    write_u32_be(value + 4U, request->opc);
    value[8] = request->service_indicator;
    value[9] = request->network_indicator;
    value[10] = request->message_priority;
    value[11] = request->signalling_link_selection;
    memcpy(value + 12U, payload, payload_length);
    *written = message_length;
    return 0;
}

static int send_m3ua(
    int socket_fd,
    const uint8_t *message,
    size_t message_length,
    uint16_t stream_id)
{
    int result = sctp_sendmsg(
        socket_fd,
        message,
        message_length,
        NULL,
        0,
        htonl(M3UA_PPID),
        0,
        stream_id,
        0,
        0);
    return result == (int)message_length ? 0 : -1;
}

static int send_ack(
    int socket_fd,
    const uint8_t *request,
    size_t request_length,
    uint8_t acknowledgement_type,
    uint16_t stream_id)
{
    uint8_t response[BUFFER_SIZE];
    if (request_length > sizeof(response)) {
        return -1;
    }

    memcpy(response, request, request_length);
    response[3] = acknowledgement_type;
    return send_m3ua(socket_fd, response, request_length, stream_id);
}

/* Respond with an explicit RC, optionally mismatched for the activation rejection run. */
static int send_reference_activation_ack(int socket_fd, const uint8_t *request, size_t length)
{
    uint8_t response[] = {1U, 0U, 4U, 3U, 0U, 0U, 0U, 24U, 0U, 0x0BU, 0U, 8U, 0U, 0U, 0U, 2U,
        0U, 6U, 0U, 8U, 0U, 0U, 0U, 0U};
    size_t offset = 8U;
    int valid = 0;
    while (offset + 4U <= length) {
        uint16_t tag = read_u16_be(request + offset);
        uint16_t size = read_u16_be(request + offset + 2U);
        if (size < 4U || offset + size > length) {
            return -1;
        }
        if (tag == M3UA_TAG_ROUTING_CONTEXT) {
            if (valid || size != 8U || read_u32_be(request + offset + 4U) != REFERENCE_CONTEXT) {
                return -1;
            }
            valid = 1;
        }
        offset += (size + 3U) & ~3U;
    }

    if (!valid || offset != length) {
        return -1;
    }
    write_u32_be(response + 20U, REFERENCE_CONTEXT + (wrong_activation_context ? 1U : 0U));
    return send_m3ua(socket_fd, response, sizeof(response), 0U);
}

/* The transit test waits for the real STP error before closing its SCTP leg. */
static int is_invalid_context_error(const uint8_t *message, size_t length)
{
    for (size_t offset = 8U; offset + 4U <= length;) {
        uint16_t size = read_u16_be(message + offset + 2U);
        if (size < 4U || offset + size > length) return 0;
        if (read_u16_be(message + offset) == 0x000CU && size == 8U) {
            return read_u32_be(message + offset + 4U) == 0x19U;
        }
        offset += (size + 3U) & ~3U;
    }
    return 0;
}

static int send_map_reply(
    int socket_fd,
    const struct protocol_data_view *protocol_data,
    const struct sccp_udt_view *sccp,
    const struct tcap_invoke_view *invoke,
    enum map_reply reply,
    uint16_t stream_id)
{
    uint8_t tcap_response[2048];
    uint8_t sccp_response[4096];
    uint8_t m3ua_response[8192];
    size_t tcap_response_length;
    size_t sccp_response_length;
    size_t m3ua_response_length;
    if (build_tcap_result(
            invoke,
            reply,
            tcap_response,
            sizeof(tcap_response),
            &tcap_response_length) != 0
        || build_sccp_udt_response(
            sccp,
            tcap_response,
            tcap_response_length,
            sccp_response,
            sizeof(sccp_response),
            &sccp_response_length) != 0
        || build_m3ua_data_response(
            protocol_data,
            sccp_response,
            sccp_response_length,
            m3ua_response,
            sizeof(m3ua_response),
            &m3ua_response_length) != 0
        || send_m3ua(
            socket_fd,
            m3ua_response,
            m3ua_response_length,
            stream_id) != 0) {
        return -1;
    }

    return 0;
}

static int handle_data(int socket_fd, const uint8_t *message, size_t message_length,
    uint16_t stream_id, unsigned int *operation_count)
{
    struct protocol_data_view protocol_data;
    struct sccp_udt_view sccp;
    struct tcap_invoke_view invoke;
    enum map_reply reply = REPLY_RESULT;
    const char *events[] = {"map-result", "map-error", "map-reject", "map-malformed-result", "map-no-reply"};
    char detail[256];

    if (parse_m3ua_protocol_data(message, message_length, &protocol_data) != 0
        || protocol_data.service_indicator != 3U || stream_id == 0U
        || (reference_profile && (!protocol_data.has_context || protocol_data.routing_context != reference_context))
        || parse_sccp_udt(protocol_data.payload, protocol_data.payload_length, &sccp) != 0
        || parse_tcap_invoke(sccp.user_data, sccp.user_data_length, &invoke) != 0
        || validate_map_parameters(&invoke) != 0) {
        log_event("validation-failed", "layer=M3UA/SCCP/TCAP/MAP");
        return -1;
    }

    if (reference_profile) {
        if (*operation_count >= sizeof(reference_cases) / sizeof(reference_cases[0])
            || reference_cases[*operation_count].code != invoke.operation_code) {
            log_event("validation-failed", "reason=unexpected-reference-case");
            return -1;
        }
        reply = reference_cases[*operation_count].reply;
    }

    snprintf(detail, sizeof(detail), "case=%u operationCode=%u invokeId=%u opc=%u dpc=%u sccpBytes=%zu mapBytes=%zu",
        *operation_count + 1U, invoke.operation_code, invoke.invoke_id, protocol_data.opc, protocol_data.dpc,
        protocol_data.payload_length, invoke.parameters_length);
    log_event("map-invoke", detail);
    if (reference_profile && *operation_count == 9U) {
        struct protocol_data_view foreign = protocol_data;
        foreign.routing_context++;
        if (send_map_reply(socket_fd, &foreign, &sccp, &invoke, REPLY_ERROR, stream_id) != 0) {
            return -1;
        }
        log_event("foreign-context-response", detail);
    }
    if (reply != REPLY_SILENCE && send_map_reply(socket_fd, &protocol_data, &sccp, &invoke, reply, stream_id) != 0) {
        return -1;
    }
    (*operation_count)++;
    log_event(events[reply], detail);
    return 0;
}

static int self_test(void)
{
    struct tcap_invoke_view invoke = {.originating_transaction_id = (const uint8_t[]){0xA0U},
        .originating_transaction_id_length = 1U, .invoke_id = 1U, .operation_code = 45U};
    static const uint8_t error[] = {0x64U, 13U, 0x49U, 1U, 0xA0U, 0x6CU, 8U, 0xA3U, 6U, 2U, 1U, 1U, 2U, 1U, 6U};
    static const uint8_t reject[] = {0x64U, 13U, 0x49U, 1U, 0xA0U, 0x6CU, 8U, 0xA4U, 6U, 2U, 1U, 1U, 0x81U, 1U, 1U};
    static const uint8_t malformed[] = {0x64U, 10U, 0x49U, 1U, 0xA0U, 0x6CU, 5U, 0xA2U, 3U, 2U, 1U, 1U};
    uint8_t bytes[128], wire[256];
    size_t written, encoded;
    struct protocol_data_view request = {.opc = 1U, .dpc = 2U, .service_indicator = 3U,
        .routing_context = REFERENCE_CONTEXT, .has_context = 1};
    struct protocol_data_view decoded;

    if (build_tcap_result(&invoke, REPLY_ERROR, bytes, sizeof(bytes), &written) != 0
        || written != sizeof(error) || memcmp(bytes, error, written) != 0
        || build_tcap_result(&invoke, REPLY_REJECT, bytes, sizeof(bytes), &written) != 0
        || written != sizeof(reject) || memcmp(bytes, reject, written) != 0
        || build_tcap_result(&invoke, REPLY_MALFORMED, bytes, sizeof(bytes), &written) != 0
        || written != sizeof(malformed) || memcmp(bytes, malformed, written) != 0
        || build_m3ua_data_response(&request, bytes, written, wire, sizeof(wire), &encoded) != 0
        || parse_m3ua_protocol_data(wire, encoded, &decoded) != 0 || !decoded.has_context
        || decoded.routing_context != REFERENCE_CONTEXT || decoded.opc != 2U || decoded.dpc != 1U) {
        return 1;
    }
    puts("Reference peer fixtures passed.");
    return 0;
}

int main(int argc, char **argv)
{
    if (argc == 2 && strcmp(argv[1], "--self-test") == 0) {
        return self_test();
    }
    const char *bind_ip = argc > 1 ? argv[1] : "127.0.0.1";
    int bind_port = argc > 2 ? atoi(argv[2]) : 2906;
    unsigned int expected_operations =
        argc > 3 ? (unsigned int)strtoul(argv[3], NULL, 10) : 5U;
    int listener_fd;
    int connection_fd;
    int reuse_address = 1;
    int no_delay = 1;
    struct sockaddr_in address;
    struct sctp_initmsg init_message;
    struct sctp_event_subscribe events;
    struct timeval receive_timeout;
    uint8_t message[BUFFER_SIZE];
    unsigned int operation_count = 0U;
    int invalid_context_acknowledged = 0;
    char detail[128];

    signal(SIGINT, handle_signal);
    signal(SIGTERM, handle_signal);
    quiet_traffic = argc > 4 && strcmp(argv[4], "quiet") == 0;
    asp_client = argc > 4 && strcmp(argv[4], "reference-asp") == 0;
    reference_profile = argc > 4 && (strcmp(argv[4], "reference") == 0 || asp_client);
    reference_context = asp_client ? 200U : REFERENCE_CONTEXT;
    wrong_activation_context = argc > 4 && strcmp(argv[4], "wrong-context") == 0;
    if ((reference_profile || wrong_activation_context) && strcmp(bind_ip, "127.0.0.1") != 0) {
        fprintf(stderr, "The reference fault fixtures require an isolated IPv4 loopback endpoint.\n");
        return 2;
    }
    if (reference_profile) expected_operations = sizeof(reference_cases) / sizeof(reference_cases[0]);
    if (wrong_activation_context) expected_operations = 0U;

    listener_fd = socket(AF_INET, SOCK_STREAM, IPPROTO_SCTP);
    if (listener_fd < 0) {
        perror("socket");
        return 1;
    }
    if (setsockopt(
            listener_fd,
            SOL_SOCKET,
            SO_REUSEADDR,
            &reuse_address,
            sizeof(reuse_address)) != 0) {
        perror("setsockopt SO_REUSEADDR");
        close(listener_fd);
        return 1;
    }
    if (setsockopt(
            listener_fd,
            IPPROTO_SCTP,
            SCTP_NODELAY,
            &no_delay,
            sizeof(no_delay)) != 0) {
        perror("setsockopt SCTP_NODELAY");
        close(listener_fd);
        return 1;
    }

    memset(&init_message, 0, sizeof(init_message));
    init_message.sinit_num_ostreams = 4U;
    init_message.sinit_max_instreams = 4U;
    init_message.sinit_max_attempts = 4U;
    if (setsockopt(
            listener_fd,
            IPPROTO_SCTP,
            SCTP_INITMSG,
            &init_message,
            sizeof(init_message)) != 0) {
        perror("setsockopt SCTP_INITMSG");
        close(listener_fd);
        return 1;
    }

    memset(&events, 0, sizeof(events));
    events.sctp_data_io_event = 1U;
    events.sctp_association_event = 1U;
    events.sctp_shutdown_event = 1U;
    if (setsockopt(
            listener_fd,
            IPPROTO_SCTP,
            SCTP_EVENTS,
            &events,
            sizeof(events)) != 0) {
        perror("setsockopt SCTP_EVENTS");
        close(listener_fd);
        return 1;
    }

    memset(&address, 0, sizeof(address));
    address.sin_family = AF_INET;
    address.sin_port = htons((uint16_t)(asp_client ? 33062 : bind_port));
    if (inet_pton(AF_INET, bind_ip, &address.sin_addr) != 1
        || bind(
            listener_fd,
            (struct sockaddr *)&address,
            sizeof(address)) != 0
        || (!asp_client && listen(listener_fd, 1) != 0)) {
        perror("bind/listen");
        close(listener_fd);
        return 1;
    }

    snprintf(detail, sizeof(detail), "endpoint=%s:%d", bind_ip, bind_port);
    if (asp_client) {
        address.sin_port = htons((uint16_t)bind_port);
        if (connect(listener_fd, (struct sockaddr *)&address, sizeof(address)) != 0) {
            perror("connect STP");
            close(listener_fd);
            return 1;
        }
        connection_fd = listener_fd;
        const uint8_t asp_up[] = {1U, 0U, 3U, 1U, 0U, 0U, 0U, 8U};
        if (send_m3ua(connection_fd, asp_up, sizeof(asp_up), 0U) != 0) return 1;
        log_event("asp-client-connected", detail);
    } else {
        log_event("listening", detail);
        connection_fd = accept(listener_fd, NULL, NULL);
        close(listener_fd);
    }
    if (connection_fd < 0) {
        perror("accept");
        return 1;
    }
    log_event("association-accepted", detail);

    receive_timeout.tv_sec = 1;
    receive_timeout.tv_usec = 0;
    (void)setsockopt(
        connection_fd,
        SOL_SOCKET,
        SO_RCVTIMEO,
        &receive_timeout,
        sizeof(receive_timeout));

    while (!stop_requested) {
        struct sockaddr_in peer_address;
        socklen_t peer_length = sizeof(peer_address);
        struct sctp_sndrcvinfo receive_info;
        int flags = 0;
        int received = sctp_recvmsg(
            connection_fd,
            message,
            sizeof(message),
            (struct sockaddr *)&peer_address,
            &peer_length,
            &receive_info,
            &flags);
        uint8_t message_class;
        uint8_t message_type;

        if (received == 0) {
            break;
        }
        if (received < 0) {
            if (errno == EINTR && stop_requested) {
                break;
            }
            if (errno == EAGAIN || errno == EWOULDBLOCK) {
                if (expected_operations > 0U
                    && operation_count >= expected_operations) {
                    break;
                }
                continue;
            }
            perror("sctp_recvmsg");
            close(connection_fd);
            return 1;
        }
        if ((flags & MSG_NOTIFICATION) != 0) {
            continue;
        }
        snprintf(
            detail,
            sizeof(detail),
            "received=%d version=%u class=%u type=%u declared=%u stream=%u ppidRaw=%u ppidNetwork=%u flags=%d",
            received,
            received > 0 ? message[0] : 0U,
            received > 2 ? message[2] : 0U,
            received > 3 ? message[3] : 0U,
            received >= 8 ? read_u32_be(message + 4U) : 0U,
            receive_info.sinfo_stream,
            receive_info.sinfo_ppid,
            ntohl(receive_info.sinfo_ppid),
            flags);
        log_event("sctp-receive", detail);
        if (received < 8 || message[0] != 1U
            || read_u32_be(message + 4U) != (uint32_t)received
            || (receive_info.sinfo_ppid != M3UA_PPID
                && ntohl(receive_info.sinfo_ppid) != M3UA_PPID)) {
            log_event("validation-failed", "layer=SCTP/M3UA");
            close(connection_fd);
            return 1;
        }

        message_class = message[2];
        message_type = message[3];
        snprintf(
            detail,
            sizeof(detail),
            "class=%u type=%u bytes=%d stream=%u ppid=%u",
            message_class,
            message_type,
            received,
            receive_info.sinfo_stream,
            ntohl(receive_info.sinfo_ppid));
        log_event("m3ua-receive", detail);

        if (asp_client && message_class == M3UA_CLASS_ASPSM && message_type == 4U) {
            uint8_t active[] = {1U, 0U, 4U, 1U, 0U, 0U, 0U, 24U, 0U, 0x0BU, 0U, 8U, 0U, 0U, 0U, 2U,
                0U, 6U, 0U, 8U, 0U, 0U, 0U, 0U};
            write_u32_be(active + 20U, reference_context);
            if (send_m3ua(connection_fd, active, sizeof(active), 0U) != 0) return 1;
        } else if (asp_client && message_class == M3UA_CLASS_ASPTM && message_type == 3U) {
            log_event("asp-client-active", "routingContext=200");
        } else if (message_class == M3UA_CLASS_ASPSM && message_type == 1U) {
            if (send_ack(
                    connection_fd,
                    message,
                    (size_t)received,
                    4U,
                    receive_info.sinfo_stream) != 0) {
                break;
            }
            log_event("asp-up-ack", detail);
        } else if (message_class == M3UA_CLASS_ASPTM && message_type == 1U) {
            if ((reference_profile || wrong_activation_context
                    ? send_reference_activation_ack(connection_fd, message, (size_t)received)
                    : send_ack(
                    connection_fd,
                    message,
                    (size_t)received,
                    3U,
                    receive_info.sinfo_stream)) != 0) {
                break;
            }
            log_event("asp-active-ack", detail);
            if (wrong_activation_context) {
                log_event("wrong-activation-context", "sent=101 expected=100");
                close(connection_fd);
                return 0;
            }
        } else if (message_class == M3UA_CLASS_ASPSM && message_type == 3U) {
            if (send_ack(
                    connection_fd,
                    message,
                    (size_t)received,
                    6U,
                    receive_info.sinfo_stream) != 0) {
                break;
            }
            log_event("heartbeat-ack", detail);
        } else if (message_class == M3UA_CLASS_ASPTM && message_type == 2U) {
            if (send_ack(
                    connection_fd,
                    message,
                    (size_t)received,
                    4U,
                    receive_info.sinfo_stream) != 0) {
                break;
            }
            log_event("asp-inactive-ack", detail);
        } else if (message_class == M3UA_CLASS_ASPSM && message_type == 2U) {
            if (send_ack(
                    connection_fd,
                    message,
                    (size_t)received,
                    5U,
                    receive_info.sinfo_stream) != 0) {
                break;
            }
            log_event("asp-down-ack", detail);
        } else if (asp_client && message_class == 0U && message_type == 0U
            && is_invalid_context_error(message, (size_t)received)) {
            invalid_context_acknowledged = 1;
            log_event("stp-invalid-context-error", "code=25");
            if (operation_count >= expected_operations) break;
        } else if (message_class == M3UA_CLASS_TRANSFER
            && message_type == M3UA_TYPE_DATA) {
            if (handle_data(
                    connection_fd,
                    message,
                    (size_t)received,
                    receive_info.sinfo_stream,
                    &operation_count) != 0) {
                close(connection_fd);
                return 1;
            }
            if (expected_operations > 0U && operation_count >= expected_operations
                && (!asp_client || invalid_context_acknowledged)) {
                break;
            }
        }
    }

    int passed = ((expected_operations == 0U && operation_count > 0U)
        || operation_count == expected_operations) && (!asp_client || invalid_context_acknowledged);
    snprintf(
        detail,
        sizeof(detail),
        "operations=%u expected=%u passed=%s",
        operation_count,
        expected_operations,
        passed ? "true" : "false");
    log_event("complete", detail);
    close(connection_fd);
    return passed ? 0 : 1;
}
