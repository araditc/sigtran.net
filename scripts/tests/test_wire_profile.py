"""Regression tests for the independent MAP SMS wire smoke report."""

import csv
import importlib.util
import io
from pathlib import Path
import unittest


spec = importlib.util.spec_from_file_location(
    "verify_wire_profile", Path(__file__).resolve().parents[1] / "verify-wire-profile.py")
wire = importlib.util.module_from_spec(spec)
spec.loader.exec_module(wire)


def make_rows():
    """Build field rows independently of the capture reader."""
    rows = []
    layers = "eth:ip:sctp:m3ua:sccp:tcap:gsm_map"
    operations = (
        (45, "0.4.0.0.1.0.20.3"), (46, "0.4.0.0.1.0.21.3"),
        (44, "0.4.0.0.1.0.25.3"), (47, "0.4.0.0.1.0.20.3"),
        (64, "0.4.0.0.1.0.23.2"),
    )
    for transaction, (code, context) in enumerate(operations, 1):
        shared = {"frame.protocols": layers, "tcap.application_context_name": context, "gsm_old.invokeID": "1"}
        begin = {
            **shared, "tcap.otid": str(transaction), "tcap.begin_element": "1",
            "tcap.dialogueRequest_element": "1", "gsm_old.invoke_element": "1", "gsm_old.localValue": str(code),
        }
        end = {
            **shared, "tcap.dtid": str(transaction), "tcap.end_element": "1", "tcap.result": "0",
            "tcap.dialogueResponse_element": "1", "gsm_old.returnResultLast_element": "1",
        }
        if code in (44, 46):
            begin["frame.protocols"] += ":gsm_sms"
        if code == 45:
            begin.update({"gsm_map.sm.sm_RP_PRI": "0", "gsm_map.sm.gprsSupportIndicator_element": "1"})
            end.update({"gsm_old.localValue": "45", "gsm_map.sm.imsi": "0010010000", "gsm_map.sm.networkNode_Number": "911032"})
        if code == 47:
            begin["gsm_map.sm.sm_DeliveryOutcome"] = "2"
        rows.extend((begin, end))
    return rows


def render(rows):
    """Serialize field rows in the same tabular format as TShark."""
    stream = io.StringIO()
    writer = csv.DictWriter(stream, fieldnames=wire.FIELDS, delimiter="\t")
    writer.writeheader()
    writer.writerows(rows)
    return stream.getvalue()


class WireProfileTests(unittest.TestCase):
    def test_all_five_pairs_are_required(self):
        rows = make_rows()
        report = wire.verify_fields(render(rows))
        self.assertEqual(report["matchedTransactionPairs"], 5)
        self.assertEqual(report["mapLocalOperationCodes"], [44, 45, 46, 47, 64])
        with self.assertRaises(ValueError):
            wire.verify_fields(render(rows[:-1]))

    def test_unrelated_or_rejected_result_does_not_pass(self):
        for field, value in (("tcap.dtid", "00"), ("gsm_old.invokeID", "2"),
                             ("tcap.result", "1"), ("tcap.application_context_name", "1.2.3"),
                             ("gsm_map.sm.networkNode_Number", ""), ("_ws.malformed", "1")):
            with self.subTest(field=field):
                rows = make_rows()
                rows[1][field] = value
                with self.assertRaises(ValueError):
                    wire.verify_fields(render(rows))


if __name__ == "__main__":
    unittest.main()
