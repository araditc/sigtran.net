#!/usr/bin/env python3
"""Verify the bounded MAP SMS wire smoke with an independent TShark dissection."""

import argparse
import csv
import io
import json
from pathlib import Path
import subprocess


FIELDS = (
    "frame.protocols", "tcap.otid", "tcap.dtid", "tcap.begin_element",
    "tcap.end_element", "tcap.dialogueRequest_element", "tcap.dialogueResponse_element",
    "tcap.application_context_name", "tcap.result", "gsm_old.invoke_element",
    "gsm_old.returnResultLast_element", "gsm_old.invokeID", "gsm_old.localValue",
    "gsm_map.sm.sm_RP_PRI", "gsm_map.sm.gprsSupportIndicator_element",
    "gsm_map.sm.imsi", "gsm_map.sm.networkNode_Number",
    "gsm_map.sm.sm_DeliveryOutcome", "_ws.malformed",
)
CONTEXTS = {
    44: "0.4.0.0.1.0.25.3",
    45: "0.4.0.0.1.0.20.3",
    46: "0.4.0.0.1.0.21.3",
    47: "0.4.0.0.1.0.20.3",
    64: "0.4.0.0.1.0.23.2",
}


def verify_fields(tsv):
    """Require five complete transaction pairs and only the declared workload."""
    table = csv.DictReader(io.StringIO(tsv), delimiter="\t")
    if tuple(table.fieldnames or ()) != FIELDS:
        raise ValueError("Unexpected TShark field schema")

    pending = {}
    seen_codes = set()
    responses = 0
    tpdu_count = 0
    for row in table:
        if None in row or any(value is None for value in row.values()):
            raise ValueError("Invalid TShark field row")
        layers = set(row["frame.protocols"].split(":"))
        if not {"sctp", "m3ua", "sccp", "tcap", "gsm_map"} <= layers or row["_ws.malformed"]:
            raise ValueError("A DATA frame is malformed or lacks a protocol layer")

        context = row["tcap.application_context_name"]
        invoke_id = row["gsm_old.invokeID"]
        if row["tcap.begin_element"] == "1":
            otid = row["tcap.otid"]
            code = int(row["gsm_old.localValue"])
            if not otid or row["tcap.dtid"] or not invoke_id or code in seen_codes or otid in pending:
                raise ValueError("Missing or duplicate MAP invoke identity")
            if (context != CONTEXTS.get(code) or row["tcap.dialogueRequest_element"] != "1"
                    or row["gsm_old.invoke_element"] != "1"):
                raise ValueError("Wrong application context or AARQ on MAP invoke")
            if code in (44, 46):
                if "gsm_sms" not in layers:
                    raise ValueError("Relay operation has no independently decoded TPDU")
                tpdu_count += 1
            if code == 45 and (row["gsm_map.sm.sm_RP_PRI"] != "0"
                               or row["gsm_map.sm.gprsSupportIndicator_element"] != "1"):
                raise ValueError("SRI-SM priority or GPRS indicator differs")
            if code == 47 and row["gsm_map.sm.sm_DeliveryOutcome"] != "2":
                raise ValueError("ReportSM outcome is not successfulTransfer")
            pending[otid] = (invoke_id, code, context)
            seen_codes.add(code)
        else:
            if row["tcap.end_element"] != "1" or row["tcap.otid"]:
                raise ValueError("Expected TCAP End for a completed smoke transaction")
            expected = pending.pop(row["tcap.dtid"], None)
            if expected is None:
                raise ValueError("Response has no matching originating transaction ID")
            expected_invoke, code, expected_context = expected
            if (invoke_id != expected_invoke or context != expected_context or row["tcap.result"] != "0"
                    or row["tcap.dialogueResponse_element"] != "1" or row["gsm_old.returnResultLast_element"] != "1"):
                raise ValueError("Response did not accept the matching dialogue and invoke")
            if code == 45:
                if (row["gsm_old.localValue"] != "45" or not row["gsm_map.sm.imsi"]
                        or not row["gsm_map.sm.networkNode_Number"]):
                    raise ValueError("SRI-SM result lacks IMSI or network-node data")
            elif row["gsm_old.localValue"]:
                raise ValueError("Expected an ID-only ReturnResultLast")
            responses += 1

    if pending or seen_codes != CONTEXTS.keys() or responses != len(CONTEXTS):
        raise ValueError("Incomplete MAP SMS wire workload")

    return {
        "passed": True,
        "matchedTransactionPairs": responses,
        "aarq": len(seen_codes),
        "acceptedAare": responses,
        "mapLocalOperationCodes": sorted(seen_codes),
        "relayTpdusDecoded": tpdu_count,
        "malformedDataFrames": 0,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("capture", type=Path)
    parser.add_argument("port", type=int)
    args = parser.parse_args()
    if not 1 <= args.port <= 65535 or not args.capture.is_file():
        parser.error("Expected a capture file and SCTP port 1..65535")

    command = [
        "tshark", "-n", "-r", str(args.capture), "-d", f"sctp.port=={args.port},m3ua",
        "-Y", "m3ua.message_class == 1 && m3ua.message_type == 1",
        "-T", "fields", "-E", "header=y",
    ]
    for field in FIELDS:
        command.extend(("-e", field))

    try:
        fields = subprocess.run(command, capture_output=True, text=True, check=True, timeout=30)
        report = verify_fields(fields.stdout)
    except (ValueError, OSError, subprocess.SubprocessError) as error:
        parser.exit(1, f"Wire profile verification failed: {error}\n")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
