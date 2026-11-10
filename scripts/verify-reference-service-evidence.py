"""Compare reference-host HTTP outcomes with the independent M3UA/SCCP/TCAP wire trace."""

import argparse
import csv
import hashlib
import io
import json
from pathlib import Path
import re
import subprocess


FIELDS = ("frame.number", "sctp.srcport", "sctp.dstport", "sctp.data_tsn",
          "sctp.data_sid", "sctp.data_payload_proto_id", "data.data")
CODES = (45, 46, 44, 47, 64, 44, 64, 45, 47, 64)
OUTCOMES = ("Result",) * 5 + ("Error", "Reject", "MalformedResult", "TimedOut", "Result")
STATUSES = (200,) * 5 + (422, 502, 502, 504, 200)
CONTEXTS = {44: 25, 45: 20, 46: 21, 47: 20, 64: 23}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def tlvs(data):
    """Read bounded, definite-length BER octets without relying on SDK codecs."""
    result = []
    while data:
        require(len(data) >= 2 and data[0] & 31 != 31, "Unsupported BER tag")
        tag, length, offset = data[0], data[1], 2
        if length & 128:
            count = length & 127
            require(1 <= count <= 2 and len(data) >= 2 + count, "Invalid BER length")
            length, offset = int.from_bytes(data[2:2 + count], "big"), 2 + count
        require(offset + length <= len(data), "Truncated BER value")
        result.append((tag, data[offset:offset + length]))
        data = data[offset + length:]
    return result


def one(data, tag):
    values = [value for actual, value in tlvs(data) if actual == tag]
    require(len(values) == 1, f"Missing or duplicated BER tag {tag:#x}")
    return values[0]


def dialogue(data, expected_tag, context_number):
    external = one(data, 0x28)
    require(one(external, 6) == bytes.fromhex("00118605010101"), "Wrong TCAP dialogue abstract syntax")
    application = one(one(external, 0xA0), expected_tag)
    expected = bytes((4, 0, 0, 1, 0, context_number, 2 if context_number == 23 else 3))
    require(one(one(application, 0xA1), 6) == expected, "Wrong MAP application context")
    if expected_tag == 0x61:
        require(one(one(application, 0xA2), 2) == b"\0", "The peer did not accept the dialogue")


def sccp(data):
    require(len(data) >= 8 and data[0] == 9, "Expected SCCP UDT")
    values = []
    for field in (2, 3, 4):
        offset = field + data[field]
        require(offset >= 5 and offset < len(data), "SCCP pointer outside variable fields")
        length = data[offset]
        require(offset + length + 1 <= len(data), "SCCP variable field outside UDT")
        values.append(data[offset + 1:offset + 1 + length])
    return values


def messages(table, port, endpoint=None):
    """Extract independently decoded M3UA messages, deduplicating identical SCTP retransmissions."""
    reader = csv.DictReader(io.StringIO(table), delimiter="\t")
    require(tuple(reader.fieldnames or ()) == FIELDS, "Unexpected capture field schema")
    seen, result = {}, []
    for row in reader:
        frame, src, dst = (int(row[key]) for key in FIELDS[:3])
        require((src == port) != (dst == port), "Unexpected SCTP endpoints")
        if endpoint is not None and endpoint not in (src, dst):
            continue
        vectors = [row[key].split(",") for key in FIELDS[3:]]
        require(len({len(value) for value in vectors}) == 1, "Bundled chunk fields do not align")
        for tsn, stream, ppid, value in zip(*vectors):
            raw = bytes.fromhex(value)
            key = src, dst, int(tsn, 0)
            require(int(ppid, 0) == 3, "M3UA PPID differs")
            if key in seen:
                require(seen[key] == raw, "SCTP retransmission changed bytes")
                continue
            seen[key] = raw
            require(len(raw) >= 8 and raw[:2] == b"\x01\x00"
                    and int.from_bytes(raw[4:8], "big") == len(raw), "Invalid M3UA header")
            parameters, offset = {}, 8
            while offset < len(raw):
                require(offset + 4 <= len(raw), "Truncated M3UA parameter")
                tag = int.from_bytes(raw[offset:offset + 2], "big")
                length = int.from_bytes(raw[offset + 2:offset + 4], "big")
                require(length >= 4 and offset + length <= len(raw) and tag not in parameters, "Invalid or duplicate M3UA parameter")
                parameters[tag] = raw[offset + 4:offset + length]
                offset += (length + 3) & ~3
            require(offset == len(raw), "Invalid M3UA padding")
            result.append((frame, dst == port, int(stream, 0), raw[2], raw[3], parameters))
    return result


def verify_wire(wire, profile):
    """Require explicit activation and transaction-correlated positive and negative outcomes."""
    activation = [entry for entry in wire if entry[3:5] == (4, 3) and not entry[1]]
    require(len(activation) == 1 and activation[0][5].get(6) == (101 if profile == "wrong-context" else 100).to_bytes(4, "big"),
            "Missing or unexpected ASP Active Ack Routing Context")
    data = [entry for entry in wire if entry[3:5] == (1, 1)]
    if profile == "wrong-context":
        require(not data, "A rejected activation admitted DATA")
        return {"dataMessages": 0, "activationRejected": True}

    pending, case, responses, foreign = {}, 0, [], 0
    for frame, request, stream, _, _, parameters in data:
        require(stream != 0 and 6 in parameters and len(parameters[6]) == 4 and 0x210 in parameters,
                "DATA lacks stream, Routing Context or Protocol Data")
        context = int.from_bytes(parameters[6], "big")
        protocol = parameters[0x210]
        require(len(protocol) >= 12 and protocol[8:10] == b"\x03\x02", "DATA is not NI=2 SCCP")
        origin, destination = int.from_bytes(protocol[:4], "big"), int.from_bytes(protocol[4:8], "big")
        require((origin, destination) == ((1, 2) if request else (2, 1)), "Wrong MTP3 point-code pair")
        called, calling, transaction = sccp(protocol[12:])
        outer = tlvs(transaction)
        require(len(outer) == 1 and outer[0][0] in {0x62, 0x64}, "Expected a TCAP Begin or End")
        tag, content = outer[0]
        component = tlvs(one(content, 0x6C))
        require(len(component) == 1, "Expected exactly one TCAP component")
        component_tag, component_value = component[0]
        invoke_id = one(component_value, 2) if component_tag == 0xA2 else tlvs(component_value)[0][1]
        require(len(invoke_id) == 1, "Invalid invoke identity")
        if request:
            require(tag == 0x62 and component_tag == 0xA1 and context == 100 and frame > activation[0][0],
                    "Invoke preceded activation or used the wrong context")
            fields = tlvs(component_value)
            require(len(fields) == 3 and fields[0][0] == fields[1][0] == 2 and len(fields[1][1]) == 1,
                    "Malformed Invoke component")
            code = fields[1][1][0]
            require(case < 10 and code == CODES[case], "Unexpected operation sequence")
            tid = one(content, 0x48)
            require(tid and tid not in pending, "Duplicate transaction identity")
            dialogue(one(content, 0x6B), 0x60, CONTEXTS[code])
            pending[tid] = (case + 1, code, invoke_id, called, calling)
            case += 1
        else:
            require(tag == 0x64, "Response must close the transaction")
            tid = one(content, 0x49)
            require(tid in pending, "Response has no correlated Invoke")
            index, code, expected_invoke, expected_called, expected_calling = pending[tid]
            require(invoke_id == expected_invoke and called == expected_calling and calling == expected_called,
                    "Response invoke or SCCP address pair differs")
            dialogue(one(content, 0x6B), 0x61, CONTEXTS[code])
            if context == 101:
                require(index == 10 and component_tag == 0xA3 and foreign == 0, "Unexpected foreign-context response")
                foreign += 1
                continue
            require(context == 100, "Unexpected application Routing Context")
            expected_tag = 0xA3 if index == 6 else 0xA4 if index == 7 else 0xA2
            require(component_tag == expected_tag, "Wrong component outcome")
            fields = tlvs(component_value)
            if index == 6:
                require(fields == [(2, invoke_id), (2, b"\x06")], "MAP user error differs")
            elif index == 7:
                require(fields == [(2, invoke_id), (0x81, b"\x01")], "TCAP Reject problem differs")
            elif index == 8:
                require(fields == [(2, invoke_id)], "Expected the invalid ID-only SRI-SM result")
            elif code == 45:
                require(len(fields) == 2 and fields[1][0] == 0x30, "SRI-SM result sequence is missing")
            else:
                require(fields == [(2, invoke_id)], "Unexpected result parameters")
            pending.pop(tid)
            responses.append(index)
    require(case == 10 and responses == [1, 2, 3, 4, 5, 6, 7, 8, 10]
            and len(pending) == 1 and next(iter(pending.values()))[0] == 9
            and foreign == (0 if profile == "maintained" else 1),
            "The wire trace did not complete the declared negative matrix")
    return {"invokes": case, "correlatedTerminalResponses": len(responses), "intentionallyUnanswered": 1,
            "foreignContextResponses": foreign, "dataMessages": len(data), "positiveOperations": 5, "negativeCases": 5}


def verify_transit(host, peer):
    """Prove the external STP relayed the original bytes and translated the two AS contexts."""
    active = [entry for entry in peer if not entry[1] and entry[3:5] == (4, 3)]
    require(len(active) == 1 and active[0][5].get(6) == (200).to_bytes(4, "big"),
            "External peer leg did not activate Routing Context 200")
    payloads = [(entry[1], int.from_bytes(entry[5][6], "big"), entry[5][0x210])
                for entry in peer if entry[3:5] == (1, 1) and entry[2] != 0]
    requests = [pdu for sending, context, pdu in payloads if not sending and context == 200]
    responses = [pdu for sending, context, pdu in payloads if sending and context == 200]
    invalid = [pdu for sending, context, pdu in payloads if sending and context == 201]
    require(len(payloads) == 20 and len(invalid) == 1, "External peer leg did not carry the declared foreign-context probe")
    host_data = [entry for entry in host if entry[3:5] == (1, 1)]
    require(requests == [entry[5][0x210] for entry in host_data if entry[1]]
            and responses == [entry[5][0x210] for entry in host_data if not entry[1]],
            "External STP did not preserve Protocol Data across both associations")
    probe_frame = next(entry[0] for entry in peer if entry[1] and entry[3:5] == (1, 1)
                       and entry[5].get(6) == (201).to_bytes(4, "big"))
    errors = [entry for entry in peer if entry[0] > probe_frame and not entry[1]
              and entry[3:5] == (0, 0) and entry[5].get(0x0C) == b"\0\0\0\x19"]
    require(len(errors) == 1, "External STP did not reject the foreign Routing Context")
    return {"stpForwardedInvokes": len(requests), "stpForwardedResponses": len(responses),
            "stpRejectedForeignContext": 1, "stpApplicationServerContexts": [100, 200]}


def verify_http(report, profile):
    require(report.get("result") == "pass" and report.get("profile") == profile, "HTTP profile did not pass")
    observations = report["observations"]
    if profile == "wrong-context":
        require(observations == [{"status": 503, "outcome": "Unavailable", "accepted": 0}], "Rejected activation dispatched work")
    else:
        require([(entry["case"], entry["status"], entry["outcome"]) for entry in observations]
                == list(zip(range(1, 11), STATUSES, OUTCOMES)), "HTTP terminal outcomes differ from the matrix")


def verify(root, profile):
    def read(name):
        return (root / name).read_text(encoding="utf-8-sig").strip()

    verify_http(json.loads(read("http-result.json")), "reference" if profile == "maintained" else profile)
    counters = re.search(r"Packets received/dropped on interface .*?: ([1-9]\d*)/0\b", read("capture.log"))
    require(counters, "Capture counters are absent or report drops")
    sha = read("source-commit.txt")
    require(re.fullmatch(r"[a-f0-9]{40}", sha), "Invalid tested source commit")
    command = ["tshark", "-n", "-r", str(root / "traffic.pcap"), "--disable-protocol", "m3ua",
               "-Y", "sctp.data_payload_proto_id == 3", "-T", "fields", "-E", "header=y"]
    for field in FIELDS:
        command.extend(("-e", field))
    table = subprocess.run(command, capture_output=True, text=True, check=True, timeout=20).stdout
    host = messages(table, 33060, 33061 if profile == "maintained" else None)
    result = verify_wire(host, profile)
    files = ["traffic.pcap", "wire-m3ua.tsv", "http-result.json", "peer.log", "host.log", "capture.log"]
    environment = {"kernel": read("kernel.txt"), "dotnetSdk": read("dotnet.txt"),
                   "topology": "Reference host and independent C/lksctp peer in a private Linux loopback namespace"}
    if profile == "maintained":
        result.update(verify_transit(host, messages(table, 33060, 33062)))
        files.extend(("stp.log", "stp.cfg", "stp-version.txt", "stp-binaries.sha256"))
        environment["maintainedStp"] = read("stp-version.txt").splitlines()[0]
        environment["topology"] = "Reference host and C/lksctp endpoint connected through an external STP over two native SCTP associations"
    (root / "wire-m3ua.tsv").write_text(table, encoding="utf-8")
    peer = read("peer.log")
    require("event=validation-failed" not in peer, "Independent peer rejected a request")
    require(("event=complete " in peer and "operations=10 expected=10 passed=true" in peer) if profile != "wrong-context"
            else "event=wrong-activation-context" in peer, "Missing peer completion")
    return {"result": "pass", "profile": profile, "sourceCommit": sha,
            "environment": environment,
            "observations": {"capturePackets": int(counters[1]), "captureDrops": 0, **result},
            "privateArtifactSha256": {name: hashlib.sha256((root / name).read_bytes()).hexdigest()
                                     for name in files},
            "limitations": ["The MAP endpoint is a repository-owned C peer, not operator or vendor acceptance.",
                            "Synthetic loopback workload, not load, long-run or exact-candidate qualification."]}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("profile", choices=("reference", "wrong-context", "maintained"))
    parser.add_argument("artifacts", type=Path)
    args = parser.parse_args()
    try:
        print(json.dumps(verify(args.artifacts, args.profile), indent=2))
    except (OSError, ValueError, KeyError, IndexError, TypeError, subprocess.SubprocessError) as error:
        parser.exit(1, f"Reference service evidence rejected: {error}\n")


if __name__ == "__main__":
    main()
