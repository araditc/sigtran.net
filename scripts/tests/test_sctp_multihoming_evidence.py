"""Check that incomplete or reconnected runs cannot pass SCTP evidence verification."""

import csv
from datetime import timedelta
from decimal import Decimal
import importlib.util
import io
from pathlib import Path
import unittest


spec = importlib.util.spec_from_file_location(
    "multihoming", Path(__file__).resolve().parents[1] / "verify-sctp-multihoming-evidence.py")
evidence = importlib.util.module_from_spec(spec)
spec.loader.exec_module(evidence)
CUT = Decimal(100)


def trace(role):
    """Place the in-flight request before the cut and its response after it."""
    offset = 84 if role == "client" else 83
    association = 19 if role == "client" else 20
    probe = "idle-receive-cancelled" if role == "client" else "oversize-rejected"
    records = [
        dict(kind="validated-record", role=role, sequence=seq, length=(17, 147, 8192, 63)[seq % 4],
             timestampUtc=(evidence.EPOCH + timedelta(seconds=seq + offset + 0.5)).isoformat(),
             stream=seq % 4, ppid=0x53475400 + seq % 3, unordered=bool(seq % 2))
        for seq in range(48)
    ]
    return [
        dict(kind=probe, role=role), *records,
        dict(kind="multihoming-summary", role=role, attempts=1, state="Established", notices=[
            dict(type="AssociationChange", associationId=association, stateCode=0, inbound=4, outbound=4),
            dict(type="PeerAddressChange", associationId=association, stateCode=1, peer=evidence.PRIMARY_PEERS[role]),
        ]),
        dict(kind="status", status="complete"),
    ]


def rows():
    """Build an independent packet table with one handshake and both directions."""
    client = (("192.0.2.1", "192.0.2.2"), ("198.51.100.1", "198.51.100.2"))
    server = (("192.0.2.2", "192.0.2.1"), ("198.51.100.2", "198.51.100.1"))
    result = [[str(70 + i), *path, chunk, "0x01", "", "", ""]
              for i, (chunk, path) in enumerate(zip((1, 2, 10, 11), (client[0], server[0]) * 2))]
    for paths, tag in ((client, "0x12"), (server, "0x34")):
        result.append(["90", *paths[0], 0, tag, 0, "0x0000", 0x53475400])
        for seq in range(12):
            result.append(["110", *paths[1], 0, tag, seq + 1, hex(seq % 4), 0x53475400 + seq % 3])
    return result


def table(packets):
    output = io.StringIO()
    writer = csv.writer(output, delimiter="\t")
    writer.writerow(evidence.FIELDS)
    writer.writerows(packets)
    return output.getvalue()


class MultihomingEvidenceTests(unittest.TestCase):
    def test_dotnet_fractional_timestamp(self):
        self.assertEqual(evidence.epoch("1970-01-01T00:01:40.12345+00:00"), Decimal("100.123450"))

    def test_continuous_association_passes(self):
        for role in ("client", "server"):
            self.assertEqual(len(evidence.verify_trace(trace(role), role, CUT)), 48)
        report = evidence.verify_wire(table(rows()), CUT)
        self.assertEqual(report["alternateDataChunks"], {"client": 12, "server": 12})
        self.assertTrue(report["sameVerificationTagPerDirection"])

    def test_missing_record_or_wrong_metadata_fails(self):
        events = trace("client")
        del events[25]
        with self.assertRaisesRegex(ValueError, "incomplete workload"):
            evidence.verify_trace(events, "client", CUT)
        events = trace("client")
        events[4]["stream"] = 0
        with self.assertRaisesRegex(ValueError, "record or metadata mismatch"):
            evidence.verify_trace(events, "client", CUT)

    def test_reconnect_or_new_kernel_association_fails(self):
        events = trace("client")
        events[-2]["attempts"] = 2
        with self.assertRaisesRegex(ValueError, "without reconnect"):
            evidence.verify_trace(events, "client", CUT)
        events = trace("server")
        events[-2]["notices"][1]["associationId"] += 1
        with self.assertRaisesRegex(ValueError, "one active association"):
            evidence.verify_trace(events, "server", CUT)

    def test_fault_must_be_observed_at_both_layers(self):
        with self.assertRaisesRegex(ValueError, "cut did not straddle"):
            evidence.verify_trace(trace("client"), "client", Decimal(105))
        events = trace("server")
        events[-2]["notices"].pop()
        with self.assertRaisesRegex(ValueError, "not reported unreachable"):
            evidence.verify_trace(events, "server", CUT)
        packets = rows()
        packets[-1][1:3] = ["192.0.2.2", "192.0.2.1"]
        with self.assertRaisesRegex(ValueError, "wrong path"):
            evidence.verify_wire(table(packets), CUT)

    def test_changed_tag_or_second_handshake_fails(self):
        packets = rows()
        packets[-1][4] = "0x42"
        with self.assertRaisesRegex(ValueError, "verification tag changed"):
            evidence.verify_wire(table(packets), CUT)
        packets = rows()
        packets.append(["110", *packets[0][1:7], ""])
        with self.assertRaisesRegex(ValueError, "handshake appeared after"):
            evidence.verify_wire(table(packets), CUT)

    def test_incomplete_wire_metadata_fails(self):
        with self.assertRaisesRegex(ValueError, "every workload stream and PPID"):
            evidence.verify_wire(table(rows()[:-1]), CUT)
        packets = rows()
        packets[-1][3] = "0,0"
        with self.assertRaisesRegex(ValueError, "Malformed DATA metadata"):
            evidence.verify_wire(table(packets), CUT)


if __name__ == "__main__":
    unittest.main()
