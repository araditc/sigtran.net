"""Exercise the authenticated local MAP SMS host using only fixed synthetic identities."""

import argparse
import json
import os
from pathlib import Path
import time
import urllib.error
import urllib.parse
import urllib.request


SRI = {"msisdn": "15550100001", "serviceCentre": "15550100002", "gprsSupport": True}
MO = {"msisdn": "15550100001", "serviceCentre": "15550100002", "tpduHex": "01000B915155100000F100040141"}
MT = {"imsi": "001010000000001", "serviceCentre": "15550100002", "tpduHex": "040B915155100000F10004621101210000000141"}
REPORT = {"msisdn": "15550100001", "serviceCentre": "15550100002", "deliveryStatus": "delivered"}
ALERT = {"msisdn": "15550100001", "serviceCentre": "15550100002"}
CASES = (
    ("sri-sm", SRI, 200, "Result"), ("mo-forward-sm", MO, 200, "Result"),
    ("mt-forward-sm", MT, 200, "Result"), ("report-sm-delivery-status", REPORT, 200, "Result"),
    ("alert-service-centre", ALERT, 200, "Result"), ("mt-forward-sm", MT, 422, "Error"),
    ("alert-service-centre", ALERT, 502, "Reject"), ("sri-sm", SRI, 502, "MalformedResult"),
    ("report-sm-delivery-status", REPORT, 504, "TimedOut"), ("alert-service-centre", ALERT, 200, "Result"),
)


def require(condition, message):
    if not condition:
        raise ValueError(message)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("profile", choices=("reference", "wrong-context"))
    parser.add_argument("url")
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    url = urllib.parse.urlsplit(args.url)
    require(url.scheme == "http" and url.hostname in {"127.0.0.1", "::1"} and url.path in {"", "/"}
            and not url.username and not url.query and not url.fragment, "The driver accepts only explicit loopback HTTP")
    token = os.environ["SIGTRAN_MAP_SMS_TOKEN"]
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))

    def request(path, body=None, credential=token):
        headers = {"Authorization": "Bearer " + credential, "Content-Type": "application/json"}
        payload = json.dumps(body).encode() if body is not None else None
        req = urllib.request.Request(args.url + path, data=payload, headers=headers)
        try:
            response = opener.open(req, timeout=8)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            data = response.read(65537)
            require(len(data) <= 65536, "Response exceeded the lab bound")
            return response.status, data

    until = time.monotonic() + 15
    while True:
        try:
            status, _ = request("/health/live")
            if status == 200:
                break
        except OSError:
            pass
        require(time.monotonic() < until, "HTTP host did not start")
        time.sleep(0.05)

    observations = []
    if args.profile == "reference":
        while True:
            status, _ = request("/health/ready")
            if status == 200:
                break
            require(time.monotonic() < until, "The full reference stack did not become ready")
            time.sleep(0.05)

        status, before = request("/metrics")
        require(status == 200 and b"sigtran_map_sms_accepted_total 0\n" in before, "Unexpected pre-workload admissions")
        for body, credential, expected in ((SRI, "incorrect", 401), ({"unexpected": 1}, token, 400)):
            status, _ = request("/v1/map-sms/sri-sm", body, credential)
            require(status == expected, "Local input policy did not reject the request")
        for index, (operation, body, expected_status, expected_outcome) in enumerate(CASES, 1):
            status, encoded = request("/v1/map-sms/" + operation, body)
            result = json.loads(encoded)
            require(status == expected_status and result["outcome"] == expected_outcome,
                    f"Unexpected terminal outcome for case {index}: HTTP {status}, {result.get('outcome')}")
            if index == 6:
                require(result["errorCode"] == 6, "MAP error code did not survive correlation")
            if index == 7:
                require(result["rejectProblem"] == 11, "TCAP Reject problem did not survive correlation")
            observations.append({"case": index, "operation": operation, "status": status, "outcome": result["outcome"]})
        status, after = request("/metrics")
        require(status == 200 and b"sigtran_map_sms_in_flight 0\n" in after
                and b"sigtran_map_sms_accepted_total 10\n" in after
                and b"sigtran_map_sms_terminal_total 10\n" in after, "Incomplete terminal accounting")
        (args.output.parent / "metrics-before.txt").write_bytes(before)
        (args.output.parent / "metrics-after.txt").write_bytes(after)
    else:
        for _ in range(3):
            status, _ = request("/health/ready")
            require(status == 503, "Wrong activation context unexpectedly became ready")
            time.sleep(0.1)
        status, result = request("/v1/map-sms/sri-sm", SRI)
        require(status == 503 and json.loads(result)["outcome"] == "Unavailable", "Wrong-context ASP admitted work")
        status, metrics = request("/metrics")
        require(status == 200 and b"sigtran_map_sms_accepted_total 0\n" in metrics
                and b"sigtran_m3ua_transfers_sent_total 0\n" in metrics, "Wrong-context ASP dispatched DATA")
        observations.append({"status": 503, "outcome": "Unavailable", "accepted": 0})

    args.output.write_text(json.dumps({"result": "pass", "profile": args.profile, "observations": observations}, indent=2),
                           encoding="utf-8")
    print(f"Reference service {args.profile}: {len(observations)} terminal observations passed.")


if __name__ == "__main__":
    main()
