"""Exercise evidence checks that distinguish replay, duplication and premature changeback."""

import importlib.util
from pathlib import Path
import unittest


spec = importlib.util.spec_from_file_location(
    "mtp3_evidence", Path(__file__).resolve().parents[1] / "verify-mtp3-m2pa-evidence.py")
evidence = importlib.util.module_from_spec(spec)
spec.loader.exec_module(evidence)
Record = evidence.Record
PRIMARY, ALTERNATE, RESTORED = 32908, 32909, 32910
INITS = [(1, PRIMARY), (3, ALTERNATE), (27, RESTORED)]
CLOSES = [(16, PRIMARY)]


def fixture():
    """A captured conversation expressed as independent decoded records."""
    reqs = [(10, PRIMARY), (15, PRIMARY), (22, ALTERNATE), (23, ALTERNATE), (30, ALTERNATE), (40, RESTORED)]
    rsps = [(11, PRIMARY), (24, ALTERNATE), (25, ALTERNATE), (26, ALTERNATE), (31, ALTERNATE), (41, RESTORED)]
    records = [Record(frame, port, True, "sccp", 9, seq) for seq, (frame, port) in enumerate(reqs, 1)]
    records.extend(Record(frame, port, False, "sccp", 9, seq) for seq, (frame, port) in enumerate(rsps, 1))
    records.extend(Record(frame, ALTERNATE, request, "management", heading, value)
                   for frame, request, heading, value in ((20, True, 0x31, 0), (21, False, 0x41, 1),
                                                          (32, True, 0x51, 3), (33, False, 0x51, 5),
                                                          (34, False, 0x61, 3), (35, True, 0x61, 5)))
    return sorted(records)


class Mtp3EvidenceTests(unittest.TestCase):
    def test_decodes_m2pa_and_itu_sccp_fixture(self):
        pdu = bytes.fromhex("01000b010000002800ffffff000000000083024000a0090003070b0443020006044301000702a501")
        self.assertEqual(evidence.decode_record(pdu, 1, 10, 32907, True, 14),
                         Record(14, 32907, True, "sccp", 9, 1))
        with self.assertRaisesRegex(ValueError, "stream 1"):
            evidence.decode_record(pdu, 0, 10, 32907, True, 14)
        with self.assertRaisesRegex(ValueError, "routing label"):
            evidence.decode_record(pdu, 1, 10, 32907, False, 14)

    def test_complete_changeover_and_changeback_pass(self):
        result = evidence.verify_changeover(fixture(), INITS, CLOSES)
        self.assertEqual(result["replayedSequences"], [3])
        self.assertEqual(result["changebackBarriersAcknowledged"], 2)

    def test_accepted_sequence_must_not_replay(self):
        records = sorted(fixture() + [Record(22, ALTERNATE, True, "sccp", 9, 2)])
        with self.assertRaisesRegex(ValueError, "sequence 2 must not replay"):
            evidence.verify_changeover(records, INITS, CLOSES)

    def test_confirmation_and_real_shutdown_are_required(self):
        records = [record._replace(value=2) if record.heading == 0x41 else record for record in fixture()]
        with self.assertRaisesRegex(ValueError, "confirmations did not separate"):
            evidence.verify_changeover(records, INITS, CLOSES)
        with self.assertRaisesRegex(ValueError, "No native primary shutdown"):
            evidence.verify_changeover(fixture(), INITS, [])

    def test_changeback_requires_matching_code_and_wire_order(self):
        records = [record._replace(value=8) if record.frame == 34 else record for record in fixture()]
        with self.assertRaisesRegex(ValueError, "matching changeback acknowledgement"):
            evidence.verify_changeover(records, INITS, CLOSES)
        records = [record._replace(frame=42) if record.frame == 34 else record for record in fixture()]
        with self.assertRaisesRegex(ValueError, "between sequences 5 and 6"):
            evidence.verify_changeover(records, INITS, CLOSES)

    def test_extra_association_cannot_masquerade_as_changeover(self):
        with self.assertRaisesRegex(ValueError, "exactly three SCTP associations"):
            evidence.verify_changeover(fixture(), INITS + [(19, ALTERNATE)], CLOSES)


if __name__ == "__main__":
    unittest.main()
