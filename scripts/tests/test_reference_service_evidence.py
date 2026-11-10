"""Reject reference-service reports that lack matching signalling evidence."""

import importlib.util
from pathlib import Path
import unittest


spec = importlib.util.spec_from_file_location("reference_wire",
    Path(__file__).resolve().parents[1] / "verify-reference-service-evidence.py")
wire = importlib.util.module_from_spec(spec)
spec.loader.exec_module(wire)


def ber(tag, value):
    return bytes((tag, len(value))) + value


def dialogue(code, response):
    context = wire.CONTEXTS[code]
    oid = bytes((4, 0, 0, 1, 0, context, 2 if context == 23 else 3))
    association = ber(0xA1, ber(6, oid)) + (ber(0xA2, ber(2, b"\0")) if response else b"")
    return ber(0x28, ber(6, bytes.fromhex("00118605010101")) + ber(0xA0, ber(0x61 if response else 0x60, association)))


def parameters(index, request, context=100, foreign=False):
    code = wire.CODES[index - 1]
    identifier = ber(2, b"\1")
    if request:
        component = ber(0xA1, identifier + ber(2, bytes((code,))) + ber(0x30, b""))
    elif index == 6 or foreign:
        component = ber(0xA3, identifier + ber(2, b"\6"))
    elif index == 7:
        component = ber(0xA4, identifier + ber(0x81, b"\1"))
    else:
        component = ber(0xA2, identifier + (ber(0x30, ber(2, bytes((code,)))) if index == 1 else b""))
    tx = ber(0x62 if request else 0x64, ber(0x48 if request else 0x49, bytes((index,)))
             + ber(0x6B, dialogue(code, not request)) + ber(0x6C, component))
    called, calling = (b"\x43\x02\0\x08", b"\x43\x01\0\x08") if request else (b"\x43\x01\0\x08", b"\x43\x02\0\x08")
    udt = bytes((9, 0, 3, 7, 11, 4)) + called + b"\4" + calling + bytes((len(tx),)) + tx
    origin, destination = (1, 2) if request else (2, 1)
    protocol = origin.to_bytes(4, "big") + destination.to_bytes(4, "big") + b"\3\2\0\1" + udt
    return {6: context.to_bytes(4, "big"), 0x210: protocol}


def conversation():
    result = [(1, False, 0, 4, 3, {6: (100).to_bytes(4, "big")})]
    for index in range(1, 11):
        result.append((index * 3, True, 1, 1, 1, parameters(index, True)))
        if index == 10:
            result.append((index * 3 + 1, False, 1, 1, 1, parameters(index, False, 101, True)))
        if index != 9:
            result.append((index * 3 + 2, False, 1, 1, 1, parameters(index, False)))
    return result


class ReferenceEvidenceTests(unittest.TestCase):
    def test_correlated_matrix(self):
        result = wire.verify_wire(conversation(), "reference")
        self.assertEqual(result["invokes"], 10)
        self.assertEqual(result["negativeCases"], 5)

    def test_wrong_activation_context_has_no_data(self):
        ack = [(1, False, 0, 4, 3, {6: (101).to_bytes(4, "big")})]
        self.assertTrue(wire.verify_wire(ack, "wrong-context")["activationRejected"])
        with self.assertRaisesRegex(ValueError, "admitted DATA"):
            wire.verify_wire(ack + conversation()[1:2], "wrong-context")

    def test_data_must_not_use_stream_zero(self):
        rows = conversation()
        rows[1] = (*rows[1][:2], 0, *rows[1][3:])
        with self.assertRaisesRegex(ValueError, "DATA lacks stream"):
            wire.verify_wire(rows, "reference")

    def test_missing_response_and_missing_foreign_probe_fail(self):
        with self.assertRaisesRegex(ValueError, "complete the declared"):
            wire.verify_wire(conversation()[:-1], "reference")
        rows = [row for row in conversation() if row[5].get(6) != (101).to_bytes(4, "big")]
        with self.assertRaisesRegex(ValueError, "complete the declared"):
            wire.verify_wire(rows, "reference")

    def test_forged_http_success_does_not_pass(self):
        report = {"result": "pass", "profile": "reference", "observations":
                  [dict(case=i, operation=name, status=s, outcome=o)
                   for i, (name, s, o) in enumerate(zip(wire.OPERATIONS, wire.STATUSES, wire.OUTCOMES), 1)]}
        wire.verify_http(report, "reference")
        report["observations"][5]["outcome"] = "Result"
        with self.assertRaisesRegex(ValueError, "terminal outcomes"):
            wire.verify_http(report, "reference")

    def test_terminal_metrics_reject_missing_or_duplicate_observations(self):
        wire.check_metrics("sigtran_map_sms_terminal_total 10\n", {"sigtran_map_sms_terminal_total": 10})
        with self.assertRaisesRegex(ValueError, "terminal_total"):
            wire.check_metrics("sigtran_map_sms_terminal_total 9\n", {"sigtran_map_sms_terminal_total": 10})
        with self.assertRaisesRegex(ValueError, "terminal_total"):
            wire.check_metrics("sigtran_map_sms_terminal_total 10\nsigtran_map_sms_terminal_total 10\n",
                               {"sigtran_map_sms_terminal_total": 10})

    def test_external_stp_must_relay_the_same_payload_and_reject_foreign_context(self):
        host = [row for row in conversation() if row[5].get(6) != (101).to_bytes(4, "big")]
        self.assertEqual(wire.verify_wire(host, "maintained")["foreignContextResponses"], 0)
        peer = [(2, False, 0, 4, 3, {6: (200).to_bytes(4, "big")})]
        for frame, request, stream, kind, subtype, params in host[1:]:
            peer.append((frame + 1, not request, stream, kind, subtype, {**params, 6: (200).to_bytes(4, "big")}))
        peer.append((32, True, 1, 1, 1, parameters(10, False, 201, True)))
        peer.append((33, False, 0, 0, 0, {0x0C: b"\0\0\0\x19"}))
        self.assertEqual(wire.verify_transit(host, peer)["stpForwardedInvokes"], 10)
        with self.assertRaisesRegex(ValueError, "did not reject"):
            wire.verify_transit(host, peer[:-1])
        peer[1][5][0x210] = b"changed"
        with self.assertRaisesRegex(ValueError, "did not preserve"):
            wire.verify_transit(host, peer)


if __name__ == "__main__":
    unittest.main()
