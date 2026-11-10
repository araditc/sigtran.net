"""Verify bounded ITU SCCP and MTP3 failover from the original native SCTP capture."""

import argparse
import csv
import hashlib
import io
import json
from pathlib import Path
import re
import subprocess
from typing import NamedTuple


PRIMARY, ALTERNATE, RESTORED = 32908, 32909, 32910
DATA_FIELDS = ("frame.number", "sctp.srcport", "sctp.dstport", "sctp.data_tsn",
               "sctp.data_sid", "sctp.data_payload_proto_id", "data.data")
CONTROL_FIELDS = ("frame.number", "sctp.srcport", "sctp.dstport", "sctp.chunk_type")


class Record(NamedTuple):
    """One independently decoded MTP3 user or management message on an SCTP association."""

    frame: int
    port: int
    request: bool
    kind: str
    heading: int
    value: int


def require(condition, detail):
    """Reject missing or contradictory evidence."""
    if not condition:
        raise ValueError(detail)


def route(src, dst, ports):
    """Resolve a direction from the listening port, not the shared loopback IP."""
    require((src in ports) != (dst in ports), "Unexpected SCTP endpoint ports")
    return (dst, True) if dst in ports else (src, False)


def decode_record(payload, sid, sls, port, request, frame):
    """Validate the RFC 4165 header and decode the supported fixture independently of the SDK."""
    require(len(payload) >= 16 and payload[:3] == b"\x01\x00\x0b"
            and int.from_bytes(payload[4:8], "big") == len(payload)
            and payload[8] == payload[12] == 0, "Invalid M2PA header")
    if payload[3] == 2:
        require(len(payload) >= 20, "Truncated M2PA Link Status")
        return None
    require(payload[3] == 1 and sid == 1, "M2PA User Data must use stream 1")
    if len(payload) == 16:
        return None

    body = payload[16:]
    require(len(body) >= 7 and body[0] == 0, "Missing M2PA spare octet or ITU MSU")
    dpc, opc = (2, 1) if request else (1, 2)
    label = int.from_bytes(body[2:6], "little")
    require(label & 0x3fff == dpc and (label >> 14) & 0x3fff == opc, "Unexpected ITU routing label")
    if body[1] == 0x80:
        management = body[6:]
        expected_lengths = {0x31: 4, 0x41: 4, 0x51: 2, 0x61: 2}
        require(label >> 28 == 1 and expected_lengths.get(management[0]) == len(management),
                "Unexpected management heading, length or affected link code")
        return Record(frame, port, request, "management", management[0], int.from_bytes(management[1:], "little"))

    require(body[1] == 0x83 and label >> 28 == sls, "Unexpected SCCP SIO or SLS")
    udt = body[6:]
    called_ssn, calling_ssn = (6, 7) if request else (7, 6)
    expected = bytes((9, 0, 3, 7, 11, 4, 0x43, dpc, 0, called_ssn,
                      4, 0x43, opc, 0, calling_ssn, 2, 0xA5 if request else 0x5A))
    require(len(udt) == len(expected) + 1 and udt[:-1] == expected, "Unexpected SCCP UDT addresses or payload")
    return Record(frame, port, request, "sccp", 9, udt[-1])


def rows(text, fields):
    """Read TShark tables with an exact schema."""
    reader = csv.DictReader(io.StringIO(text), delimiter="\t")
    require(tuple(reader.fieldnames or ()) == fields, "Unexpected TShark field table")
    return reader


def read_records(text, ports, sls):
    """Read each SCTP DATA chunk and ignore retransmissions of the same TSN."""
    seen = {}
    records = []
    for row in rows(text, DATA_FIELDS):
        frame, src, dst = (int(row[field]) for field in DATA_FIELDS[:3])
        port, request = route(src, dst, ports)
        vectors = [row[field].split(",") for field in DATA_FIELDS[3:]]
        require(len({len(vector) for vector in vectors}) == 1, "Incomplete SCTP chunk metadata")
        for tsn, sid, ppid, raw in zip(*vectors):
            payload = bytes.fromhex(raw)
            require(int(ppid, 0) == 5, "M2PA DATA must use PPID 5")
            key = (src, dst, int(tsn, 0))
            if key in seen:
                require(seen[key] == payload, "A retransmitted SCTP TSN changed bytes")
                continue
            seen[key] = payload
            record = decode_record(payload, int(sid, 0), sls, port, request, frame)
            if record:
                records.append(record)
    return records


def verify_independent(records):
    """Require two independent class-0 request/reply pairs."""
    require([(r.kind, r.request, r.value, r.port) for r in records] ==
            [("sccp", request, seq, 32907) for seq in (1, 2) for request in (True, False)],
            "Independent PCAP must contain the two SCCP request/reply pairs")
    return {"roundTrips": 2, "sls": 10, "independentPeer": "C/lksctp reference peer"}


def verify_changeover(records, inits, closes):
    """Require selective replay before new traffic and CBA before restored-link use."""
    requests = [record for record in records if record.kind == "sccp" and record.request]
    replies = [record for record in records if record.kind == "sccp" and not record.request]
    require([(record.value, record.port) for record in requests] ==
            list(enumerate((PRIMARY, PRIMARY, ALTERNATE, ALTERNATE, ALTERNATE, RESTORED), 1)),
            "Request order or association changed; accepted sequence 2 must not replay")
    require([(record.value, record.port) for record in replies] ==
            list(enumerate((PRIMARY, ALTERNATE, ALTERNATE, ALTERNATE, ALTERNATE, RESTORED), 1)),
            "Reply order or association changed")
    require(all(req.frame < reply.frame for req, reply in zip(requests, replies)), "Reply preceded its request")
    require(sorted(port for _, port in inits) == [PRIMARY, ALTERNATE, RESTORED], "Expected exactly three SCTP associations")

    management = [record for record in records if record.kind == "management"]
    orders = [record for record in management if record.heading in {0x31, 0x41}]
    require({record.heading for record in orders} == {0x31, 0x41}
            and all(record.port == ALTERNATE and requests[1].frame < record.frame < requests[2].frame for record in orders),
            "XCO/XCA did not precede replay on the surviving link")
    require(all(any(port == PRIMARY and requests[1].frame < frame < record.frame for frame, port in closes)
                for record in orders), "No native primary shutdown preceded changeover")
    require({record.value for record in orders if not record.request} == {1}
            and {record.value for record in orders if record.request} == {0},
            "Peer confirmations did not separate accepted and missing FSNs")
    require(requests[3].frame < next(frame for frame, port in inits if port == RESTORED) < requests[4].frame,
            "Replacement association was not established while traffic was diverted")

    barriers = [record for record in management if record.heading in {0x51, 0x61}]
    require(all(record.port == ALTERNATE and replies[4].frame < record.frame < requests[5].frame for record in barriers),
            "Changeback barrier was not on the alternate between sequences 5 and 6")
    for request in (True, False):
        declarations = [record for record in barriers if record.heading == 0x51 and record.request == request]
        require(declarations and len({record.value for record in declarations}) == 1, "Missing changeback declaration")
        require(any(record.heading == 0x61 and record.request != request and record.value == declarations[0].value
                    and record.frame > declarations[0].frame for record in barriers), "Missing matching changeback acknowledgement")
    return {"roundTrips": 6, "sctpAssociations": 3, "replayedSequences": [3], "notReplayedSequences": [2],
            "newTrafficAfterReplay": [4], "alternateBeforeChangeback": [5], "restoredAfterChangeback": [6],
            "extendedChangeoverOrders": sum(record.heading == 0x31 for record in orders),
            "extendedChangeoverAcknowledgements": sum(record.heading == 0x41 for record in orders),
            "changebackBarriersAcknowledged": 2}


def verify_trace(events, profile):
    """Corroborate the packet observations with the SDK's validated upper-layer messages."""
    def payloads(name):
        return [event["payload"] for event in events if event["eventName"] == name]

    require(payloads("complete") == [{"passed": True}] and not payloads("failed"), "SDK run did not complete")
    if profile == "itu-sccp":
        require([item["sequence"] for item in payloads("sccp-unitdata-roundtrip")] == [1, 2],
                "SDK did not accept both independent replies")
    else:
        require(payloads("probe-staged") == [{"acceptedWithoutAck": 2, "withheldBeforeWire": 3, "retained": [1, 2]}],
                "Missing controlled retained-message probe")
        require(payloads("association-closed") == [{"port": PRIMARY}]
                and payloads("replacement-aligned") == [{"port": RESTORED}]
                and payloads("changeover-complete") == [{"replayed": 3, "excluded": 2}]
                and payloads("changeback-complete") == [{"client": True, "server": True}], "Incomplete SDK fault timeline")
        for event in ("request-accepted", "reply-accepted"):
            require([item["sequence"] for item in payloads(event)] == list(range(1, 7)), f"SDK {event} was not complete and ordered")


def tshark(root, fields, expression, raw=False):
    """Derive evidence from the capture itself, not from a supplied field table."""
    command = ["tshark", "-n", "-r", str(root / "traffic.pcap"), "-Y", expression, "-T", "fields", "-E", "header=y"]
    if raw:
        command.extend(("--disable-protocol", "m2pa"))
    for field in fields:
        command.extend(("-e", field))
    return subprocess.run(command, check=True, capture_output=True, text=True, timeout=20).stdout


def verify(root, profile):
    """Verify the selected profile and return a sanitized, digest-bound report."""
    def read(name):
        return (root / name).read_text(encoding="utf-8-sig").strip()

    capture = re.search(r"Packets received/dropped on interface .*?: ([1-9]\d*)/0\b", read("capture.log"))
    require(capture is not None, "Capture dropped packets or has no valid counters")
    sha = read("source-commit.txt")
    require(re.fullmatch(r"[a-f0-9]{40}", sha), "Invalid SDK source commit")
    result = json.loads(read("sdk-result.json"))
    require(result.get("passed") is True and result.get("profile") == profile, "SDK result/profile mismatch")
    verify_trace([json.loads(line) for line in read("sdk.jsonl").splitlines()], profile)

    independent = profile == "itu-sccp"
    ports = {32907} if independent else {PRIMARY, ALTERNATE, RESTORED}
    data = tshark(root, DATA_FIELDS, "sctp.data_payload_proto_id == 5", raw=True)
    records = read_records(data, ports, 10 if independent else 0)
    files = ["traffic.pcap", "sdk.jsonl", "sdk-result.json", "capture.log", "wire-m2pa.tsv"]
    if independent:
        log = read("peer.log")
        require(log.count("event=itu-sccp-roundtrip ") == 2 and re.search(r"event=complete .*passed=true", log),
                "Independent peer did not validate the wire fixture")
        observations = verify_independent(records)
        files.append("peer.log")
    else:
        control = tshark(root, CONTROL_FIELDS, "sctp.chunk_type == 1 || sctp.chunk_type == 6 || sctp.chunk_type == 7")
        inits, closes = [], []
        for row in rows(control, CONTROL_FIELDS):
            frame, src, dst = (int(row[field]) for field in CONTROL_FIELDS[:3])
            port, _ = route(src, dst, ports)
            chunks = {int(chunk, 0) for chunk in row[CONTROL_FIELDS[3]].split(",")}
            if 1 in chunks:
                inits.append((frame, port))
            if chunks & {6, 7}:
                closes.append((frame, port))
        observations = verify_changeover(records, inits, closes)
        (root / "wire-associations.tsv").write_text(control, encoding="utf-8")
        files.append("wire-associations.tsv")

    (root / "wire-m2pa.tsv").write_text(data, encoding="utf-8")
    return {"result": "pass", "profile": profile, "sdkSourceCommit": sha, "runId": result["runId"],
            "environment": {"kernel": read("kernel.txt"), "dotnetSdk": read("dotnet.txt"),
                            "topology": "One private Linux loopback network namespace"},
            "observations": {"capturePackets": int(capture[1]), "captureDrops": 0, **observations},
            "privateArtifactSha256": {name: hashlib.sha256((root / name).read_bytes()).hexdigest() for name in files},
            "limitations": ["Synthetic traffic; no operator peer, cross-host or stable candidate qualification.",
                            "The changeover profile uses two SDK endpoints; the independent C peer verifies the separate ITU SCCP profile."]}


def main():
    """Emit a report only when both capture and trace pass verification."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("profile", choices=("itu-sccp", "changeover"))
    parser.add_argument("artifacts", type=Path)
    args = parser.parse_args()
    try:
        print(json.dumps(verify(args.artifacts, args.profile), indent=2))
    except (OSError, ValueError, KeyError, TypeError, subprocess.SubprocessError) as error:
        parser.exit(1, f"MTP3/M2PA evidence rejected: {error}\n")


if __name__ == "__main__":
    main()
