#define _POSIX_C_SOURCE 200809L

/*
 * Independent RFC 4165 M2PA reference peer for SIGTRAN.NET interoperability
 * evidence. This program is implemented in C/lksctp and does not link to or
 * reuse Sigtran.NET protocol code.
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
#define M2PA_PPID 5U
#define M2PA_VERSION 1U
#define M2PA_CLASS 11U
#define M2PA_USER_DATA 1U
#define M2PA_LINK_STATUS 2U
#define M2PA_HEADER_LENGTH 16U
#define M2PA_MAX_SEQUENCE 0x00FFFFFFU
#define M2PA_STREAM_STATUS 0U
#define M2PA_STREAM_DATA 1U

#define M2PA_STATUS_ALIGNMENT 1U
#define M2PA_STATUS_PROVING_NORMAL 2U
#define M2PA_STATUS_PROVING_EMERGENCY 3U
#define M2PA_STATUS_READY 4U
#define M2PA_STATUS_PROCESSOR_OUTAGE 5U
#define M2PA_STATUS_PROCESSOR_RECOVERED 6U
#define M2PA_STATUS_BUSY 7U
#define M2PA_STATUS_BUSY_ENDED 8U
#define M2PA_STATUS_OUT_OF_SERVICE 9U

static volatile sig_atomic_t stop_requested = 0;

static void handle_signal(int signal_number)
{
    (void)signal_number;
    stop_requested = 1;
}

static uint32_t read_u24_be(const uint8_t *value)
{
    return ((uint32_t)value[0] << 16)
        | ((uint32_t)value[1] << 8)
        | (uint32_t)value[2];
}

static void write_u24_be(uint8_t *destination, uint32_t value)
{
    destination[0] = (uint8_t)(value >> 16);
    destination[1] = (uint8_t)(value >> 8);
    destination[2] = (uint8_t)value;
}

static uint32_t read_u32_be(const uint8_t *value)
{
    uint32_t result;
    memcpy(&result, value, sizeof(result));
    return ntohl(result);
}

static void write_u32_be(uint8_t *destination, uint32_t value)
{
    uint32_t encoded = htonl(value);
    memcpy(destination, &encoded, sizeof(encoded));
}

static uint32_t next_sequence(uint32_t value)
{
    return value == M2PA_MAX_SEQUENCE ? 0U : value + 1U;
}

static void log_event(const char *event_name, const char *detail)
{
    time_t now = time(NULL);
    struct tm value;
    char timestamp[32];

    gmtime_r(&now, &value);
    strftime(timestamp, sizeof(timestamp), "%Y-%m-%dT%H:%M:%SZ", &value);
    printf("%s event=%s %s\n", timestamp, event_name, detail);
    fflush(stdout);
}

static int build_common(
    uint8_t *destination,
    size_t capacity,
    uint8_t type,
    uint32_t bsn,
    uint32_t fsn,
    const uint8_t *payload,
    size_t payload_length,
    size_t *written)
{
    size_t length = M2PA_HEADER_LENGTH + payload_length;

    if (length > capacity || bsn > M2PA_MAX_SEQUENCE || fsn > M2PA_MAX_SEQUENCE) {
        return -1;
    }

    memset(destination, 0, length);
    destination[0] = M2PA_VERSION;
    destination[2] = M2PA_CLASS;
    destination[3] = type;
    write_u32_be(destination + 4U, (uint32_t)length);
    destination[8] = 0U;
    write_u24_be(destination + 9U, bsn);
    destination[12] = 0U;
    write_u24_be(destination + 13U, fsn);
    if (payload_length > 0U) {
        memcpy(destination + M2PA_HEADER_LENGTH, payload, payload_length);
    }

    *written = length;
    return 0;
}

static int send_message(
    int socket_fd,
    const uint8_t *message,
    size_t message_length,
    uint16_t stream_id)
{
    int sent = sctp_sendmsg(
        socket_fd,
        message,
        message_length,
        NULL,
        0,
        htonl(M2PA_PPID),
        0,
        stream_id,
        0,
        0);

    return sent == (int)message_length ? 0 : -1;
}

static int send_link_status(
    int socket_fd,
    uint32_t bsn,
    uint32_t fsn,
    uint32_t status,
    uint16_t stream_id)
{
    uint8_t message[32];
    uint8_t status_bytes[4];
    size_t written = 0U;

    write_u32_be(status_bytes, status);
    if (build_common(
            message,
            sizeof(message),
            M2PA_LINK_STATUS,
            bsn,
            fsn,
            status_bytes,
            sizeof(status_bytes),
            &written) != 0) {
        return -1;
    }

    return send_message(socket_fd, message, written, stream_id);
}

static int send_user_data(
    int socket_fd,
    uint32_t bsn,
    uint32_t fsn,
    const uint8_t *payload,
    size_t payload_length)
{
    uint8_t message[BUFFER_SIZE];
    size_t written = 0U;

    if (build_common(
            message,
            sizeof(message),
            M2PA_USER_DATA,
            bsn,
            fsn,
            payload,
            payload_length,
            &written) != 0) {
        return -1;
    }

    return send_message(socket_fd, message, written, M2PA_STREAM_DATA);
}

int main(int argc, char **argv)
{
    const char *bind_ip = argc > 1 ? argv[1] : "127.0.0.1";
    int bind_port = argc > 2 ? atoi(argv[2]) : 2907;
    unsigned int expected_data =
        argc > 3 ? (unsigned int)strtoul(argv[3], NULL, 10) : 2U;

    int listener_fd;
    int connection_fd;
    int reuse_address = 1;
    int no_delay = 1;
    struct sockaddr_in address;
    struct sctp_initmsg init_message;
    struct sctp_event_subscribe events;
    struct timeval receive_timeout;
    uint8_t message[BUFFER_SIZE];
    char detail[256];

    uint32_t last_received = M2PA_MAX_SEQUENCE;
    uint32_t last_sent = M2PA_MAX_SEQUENCE;
    unsigned int data_count = 0U;
    unsigned int ack_count = 0U;
    int saw_alignment = 0;
    int saw_initial_ready = 0;
    int saw_busy = 0;
    int saw_busy_ended = 0;
    int saw_processor_outage = 0;
    int saw_processor_recovered = 0;
    int saw_recovery_ready = 0;
    int saw_out_of_service = 0;

    signal(SIGINT, handle_signal);
    signal(SIGTERM, handle_signal);

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
            sizeof(reuse_address)) != 0
        || setsockopt(
            listener_fd,
            IPPROTO_SCTP,
            SCTP_NODELAY,
            &no_delay,
            sizeof(no_delay)) != 0) {
        perror("setsockopt");
        close(listener_fd);
        return 1;
    }

    memset(&init_message, 0, sizeof(init_message));
    init_message.sinit_num_ostreams = 2U;
    init_message.sinit_max_instreams = 2U;
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
    address.sin_port = htons((uint16_t)bind_port);
    if (inet_pton(AF_INET, bind_ip, &address.sin_addr) != 1
        || bind(listener_fd, (struct sockaddr *)&address, sizeof(address)) != 0
        || listen(listener_fd, 1) != 0) {
        perror("bind/listen");
        close(listener_fd);
        return 1;
    }

    snprintf(detail, sizeof(detail), "endpoint=%s:%d", bind_ip, bind_port);
    log_event("listening", detail);

    connection_fd = accept(listener_fd, NULL, NULL);
    close(listener_fd);
    if (connection_fd < 0) {
        perror("accept");
        return 1;
    }
    log_event("association-accepted", detail);

    receive_timeout.tv_sec = 2;
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

        if (received == 0) {
            break;
        }
        if (received < 0) {
            if (errno == EINTR && stop_requested) {
                break;
            }
            if (errno == EAGAIN || errno == EWOULDBLOCK) {
                if (data_count >= expected_data && saw_processor_recovered) {
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

        if (received < (int)M2PA_HEADER_LENGTH
            || message[0] != M2PA_VERSION
            || message[1] != 0U
            || message[2] != M2PA_CLASS
            || read_u32_be(message + 4U) != (uint32_t)received
            || message[8] != 0U
            || message[12] != 0U
            || (receive_info.sinfo_ppid != M2PA_PPID
                && ntohl(receive_info.sinfo_ppid) != M2PA_PPID)
            || (receive_info.sinfo_flags & SCTP_UNORDERED) != 0U) {
            log_event("validation-failed", "layer=SCTP/M2PA");
            close(connection_fd);
            return 1;
        }

        uint8_t type = message[3];
        uint32_t bsn = read_u24_be(message + 9U);
        uint32_t fsn = read_u24_be(message + 13U);
        size_t payload_length = (size_t)received - M2PA_HEADER_LENGTH;

        snprintf(
            detail,
            sizeof(detail),
            "type=%u stream=%u bsn=%u fsn=%u bytes=%d ppid=%u",
            type,
            receive_info.sinfo_stream,
            bsn,
            fsn,
            received,
            ntohl(receive_info.sinfo_ppid));
        log_event("m2pa-receive", detail);

        if (type == M2PA_LINK_STATUS) {
            uint32_t status;
            if (payload_length < 4U) {
                log_event("validation-failed", "reason=link-status-too-short");
                close(connection_fd);
                return 1;
            }

            status = read_u32_be(message + M2PA_HEADER_LENGTH);
            snprintf(detail, sizeof(detail), "status=%u stream=%u", status, receive_info.sinfo_stream);
            log_event("link-status", detail);

            switch (status) {
                case M2PA_STATUS_OUT_OF_SERVICE:
                    if (data_count >= expected_data) {
                        saw_out_of_service = 1;
                        stop_requested = 1;
                    }
                    break;

                case M2PA_STATUS_ALIGNMENT:
                    saw_alignment = 1;
                    if (receive_info.sinfo_stream != M2PA_STREAM_STATUS
                        || send_link_status(
                            connection_fd,
                            last_received,
                            last_sent,
                            M2PA_STATUS_ALIGNMENT,
                            M2PA_STREAM_STATUS) != 0) {
                        log_event("send-failed", "status=alignment");
                        close(connection_fd);
                        return 1;
                    }
                    log_event("link-status-send", "status=alignment stream=0");
                    break;

                case M2PA_STATUS_PROVING_NORMAL:
                case M2PA_STATUS_PROVING_EMERGENCY:
                    if (send_link_status(
                            connection_fd,
                            last_received,
                            last_sent,
                            status,
                            M2PA_STREAM_STATUS) != 0) {
                        log_event("send-failed", "status=proving");
                        close(connection_fd);
                        return 1;
                    }
                    log_event("link-status-send", "status=proving stream=0");
                    break;

                case M2PA_STATUS_READY:
                    if (receive_info.sinfo_stream == M2PA_STREAM_STATUS
                        && !saw_initial_ready) {
                        saw_initial_ready = 1;
                        if (send_link_status(
                                connection_fd,
                                last_received,
                                last_sent,
                                M2PA_STATUS_READY,
                                M2PA_STREAM_STATUS) != 0) {
                            log_event("send-failed", "status=ready-alignment");
                            close(connection_fd);
                            return 1;
                        }
                        log_event("link-status-send", "status=ready stream=0");
                    } else if (receive_info.sinfo_stream == M2PA_STREAM_DATA) {
                        saw_recovery_ready = 1;
                        log_event("processor-recovery-complete", "ready-on-stream=1");
                    }
                    break;

                case M2PA_STATUS_BUSY:
                    saw_busy = 1;
                    break;

                case M2PA_STATUS_BUSY_ENDED:
                    saw_busy_ended = 1;
                    break;

                case M2PA_STATUS_PROCESSOR_OUTAGE:
                    saw_processor_outage = 1;
                    break;

                case M2PA_STATUS_PROCESSOR_RECOVERED:
                    saw_processor_recovered = 1;
                    if (receive_info.sinfo_stream != M2PA_STREAM_DATA
                        || send_link_status(
                            connection_fd,
                            last_received,
                            last_sent,
                            M2PA_STATUS_READY,
                            M2PA_STREAM_DATA) != 0) {
                        log_event("send-failed", "status=ready-recovery");
                        close(connection_fd);
                        return 1;
                    }
                    log_event("link-status-send", "status=ready stream=1");
                    break;

                default:
                    log_event("validation-failed", "reason=unsupported-link-status");
                    close(connection_fd);
                    return 1;
            }

            continue;
        }

        if (type != M2PA_USER_DATA || receive_info.sinfo_stream != M2PA_STREAM_DATA) {
            log_event("validation-failed", "reason=unexpected-message-type-or-stream");
            close(connection_fd);
            return 1;
        }

        if (payload_length == 0U) {
            ack_count++;
            snprintf(detail, sizeof(detail), "bsn=%u fsn=%u ackCount=%u", bsn, fsn, ack_count);
            log_event("ack-receive", detail);
            continue;
        }

        if (fsn != next_sequence(last_received)) {
            log_event("validation-failed", "reason=out-of-order-user-data");
            close(connection_fd);
            return 1;
        }

        last_received = fsn;
        if (send_user_data(
                connection_fd,
                last_received,
                last_sent,
                NULL,
                0U) != 0) {
            log_event("send-failed", "type=ack");
            close(connection_fd);
            return 1;
        }
        log_event("ack-send", "result=success");

        last_sent = next_sequence(last_sent);
        if (send_user_data(
                connection_fd,
                last_received,
                last_sent,
                message + M2PA_HEADER_LENGTH,
                payload_length) != 0) {
            log_event("send-failed", "type=echo");
            close(connection_fd);
            return 1;
        }

        data_count++;
        snprintf(
            detail,
            sizeof(detail),
            "operation=%u peerFsn=%u clientFsn=%u payloadBytes=%zu",
            data_count,
            last_sent,
            last_received,
            payload_length);
        log_event("user-data-echo", detail);
    }

    {
        int passed =
            data_count == expected_data
            && ack_count >= expected_data
            && saw_alignment
            && saw_initial_ready
            && saw_busy
            && saw_busy_ended
            && saw_processor_outage
            && saw_processor_recovered
            && saw_recovery_ready;

        snprintf(
            detail,
            sizeof(detail),
            "data=%u expected=%u acks=%u alignment=%d initialReady=%d busy=%d busyEnded=%d processorOutage=%d processorRecovered=%d recoveryReady=%d outOfService=%d passed=%s",
            data_count,
            expected_data,
            ack_count,
            saw_alignment,
            saw_initial_ready,
            saw_busy,
            saw_busy_ended,
            saw_processor_outage,
            saw_processor_recovered,
            saw_recovery_ready,
            saw_out_of_service,
            passed ? "true" : "false");
        log_event("complete", detail);
        close(connection_fd);
        return passed ? 0 : 1;
    }
}
