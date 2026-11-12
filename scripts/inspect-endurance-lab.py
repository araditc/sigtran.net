#!/usr/bin/env python3
"""Read a durable endurance job's progress without modifying it or claiming qualification."""
import argparse
import importlib.util
import json
import pathlib
import re

CODE = '''import json,pathlib,struct,subprocess,sys
root=pathlib.Path('/opt/sigtran-endurance/runs')/sys.argv[1]
unit='sigtran-endurance-sdk-'+sys.argv[1]+'.service'
state=subprocess.run(['systemctl','show',unit,'--property=ActiveState,SubState,Result,ExecMainStatus'],capture_output=True,text=True,check=True)
result=dict(runId=sys.argv[1],unit=dict(line.split('=',1) for line in state.stdout.splitlines() if '=' in line),qualificationPassed=False)
path=root/'work/sdk/resource-observations.bin'
if path.exists():
 size=path.stat().st_size
 if size>=216:
  with path.open('rb') as f:f.seek(8+((size-8)//208-1)*208);row=struct.unpack('<d6qd18q',f.read(208))
  result['observation']=dict(elapsedSeconds=row[0],started=row[14],successful=row[15],pending=row[16],peakRssMiB=row[2]/1048576,managedMiB=row[3]/1048576,heapMiB=row[4]/1048576)
for name,key in [('work/inventory.json','collection'),('verified-summary.json','verifiedSummary')]:
 path=root/name
 if path.exists():
  value=json.loads(path.read_text())
  fields=('completed','cleanupVerified','collectionErrors','sdkExitCode','controllerExitCode') if key=='collection' else ('testedSourceCommit','diagnostic','executionValid','qualificationPassed','stablePromotion','durationSeconds','completedTps','recoveredIncidents','unexpectedFailures','unresolvedOperations','reasons')
  result[key]={field:value[field] for field in fields if field in value}
print(json.dumps(result,indent=2))
'''


def main():
    import paramiko
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--run-id', required=True)
    parser.add_argument('--host', default='192.168.100.28')
    parser.add_argument('--key', required=True)
    parser.add_argument('--known-hosts', default=str(pathlib.Path.home() / '.ssh/known_hosts'))
    args = parser.parse_args()
    if not re.fullmatch('[a-z0-9][a-z0-9-]{0,47}', args.run_id): raise ValueError('Invalid run identity')
    spec = importlib.util.spec_from_file_location('endurance_bootstrap', pathlib.Path(__file__).with_name('start-endurance-lab.py'))
    bootstrap = importlib.util.module_from_spec(spec); spec.loader.exec_module(bootstrap)
    client = paramiko.SSHClient(); client.load_host_keys(str(pathlib.Path(args.known_hosts).expanduser()))
    client.set_missing_host_key_policy(paramiko.RejectPolicy())
    try:
        client.connect(args.host, username='ammar', key_filename=args.key, look_for_keys=False, allow_agent=False, timeout=15)
        result = json.loads(bootstrap.command(client, ['python3', '-c', CODE, args.run_id], timeout=30))
        print(json.dumps(result, indent=2))
    finally: client.close()


if __name__ == '__main__': main()
