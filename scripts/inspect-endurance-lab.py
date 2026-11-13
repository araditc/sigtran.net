#!/usr/bin/env python3
"""Read a durable endurance job's progress without modifying it or claiming qualification."""
import argparse
import importlib.util
import json
import os
import pathlib
import re

CODE = pathlib.Path(__file__).with_name('read-endurance-status.py').read_text(encoding='utf-8')


def qualification_status(value):
    """Require the independent verdict and successful stopped supervision; liveness never qualifies."""
    unit, summary = value.get('unit', {}), value.get('verifiedSummary', {})
    return (unit.get('ActiveState') == 'inactive' and unit.get('Result') == 'success'
            and summary.get('executionValid') is True and summary.get('qualificationPassed') is True
            and summary.get('stablePromotion') is False)


def unreachable(error):
    """A desktop management failure provides no evidence that the server-side load stopped."""
    return dict(management='unreachable', executionState='unknown', qualificationPassed=False,
                readOnly=True, error=type(error).__name__, workloadRestarted=False)


def main():
    import paramiko
    parser = argparse.ArgumentParser(description=__doc__)
    group = parser.add_mutually_exclusive_group()
    group.add_argument('--run-id')
    group.add_argument('--latest', action='store_true', help='Discover the active full run, otherwise the latest retained full run')
    parser.add_argument('--host', default=os.environ.get('SIGTRAN_LAB_SDK_HOST', '192.168.100.28'))
    parser.add_argument('--key', default=os.environ.get('SIGTRAN_LAB_SDK_KEY', str(pathlib.Path.home() / '.ssh/sigtran_vm_release_ed25519')))
    parser.add_argument('--known-hosts', default=str(pathlib.Path.home() / '.ssh/known_hosts'))
    parser.add_argument('--summary-output', type=pathlib.Path)
    args = parser.parse_args()
    if args.run_id is not None and not re.fullmatch('[a-z0-9][a-z0-9-]{0,47}', args.run_id): raise ValueError('Invalid run identity')
    spec = importlib.util.spec_from_file_location('endurance_bootstrap', pathlib.Path(__file__).with_name('start-endurance-lab.py'))
    bootstrap = importlib.util.module_from_spec(spec); spec.loader.exec_module(bootstrap)
    client = paramiko.SSHClient(); client.load_host_keys(str(pathlib.Path(args.known_hosts).expanduser()))
    client.set_missing_host_key_policy(paramiko.RejectPolicy())
    try:
        client.connect(args.host, username='ammar', key_filename=args.key, look_for_keys=False, allow_agent=False,
                       timeout=15, banner_timeout=15, auth_timeout=15)
        result = json.loads(bootstrap.command(client, ['python3', '-c', CODE, args.run_id or '@latest'], timeout=30))
        result['qualificationPassed'] = qualification_status(result)
        if args.summary_output:
            if result['unit'].get('ActiveState') != 'inactive' or result['unit'].get('Result') != 'success':
                raise ValueError('Successful stopped supervision is required before summary retrieval')
            with client.open_sftp() as sftp:
                with sftp.open('/opt/sigtran-endurance/runs/' + result['runId'] + '/verified-summary.json', 'rb') as stream:
                    data = stream.read(2097153)
            if len(data) > 2097152: raise ValueError('Oversized verified summary')
            value = json.loads(data)
            if value['executionValid'] is not True or value['stablePromotion'] is not False:
                raise ValueError('Unexpected summary boundary')
            with args.summary_output.open('xb') as stream: stream.write(data)
        print(json.dumps(result, indent=2))
    except (OSError, paramiko.SSHException, EOFError) as error:
        print(json.dumps(unreachable(error), indent=2)); return 2
    finally: client.close()


if __name__ == '__main__': raise SystemExit(main())
