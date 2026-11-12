#!/usr/bin/env python3
"""Create sanitized public capacity JSON/Markdown only after re-verifying private raw evidence."""
import argparse
import importlib.util
import json
import pathlib

SPEC = importlib.util.spec_from_file_location('capacity_verifier', pathlib.Path(__file__).with_name('verify-capacity-evidence.py'))
verifier = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(verifier)


def render(report):
    """Never convert a completed diagnostic into a passing product qualification."""
    qualified = report['qualificationPassed'] is True
    status = 'PASS' if qualified else 'NOT QUALIFIED'
    latency = report['latency']
    resources = report['sdkResources']
    allocated = resources['AllocatedBytes'] / max(1, report['completedTransactions'] + report['lateSuccesses'])
    text = '\n'.join([
        '# Completed MAP Capacity Measurement', '', f'Status: **{status}**.', '',
        f"Protocol/accounting execution: **{'PASS' if report['executionValid'] else 'FAIL'}**.",
        f"Numeric capacity target: **{'MET' if report['numericTargetMet'] else 'NOT MET'}**.",
        f"Capacity policy: `{report['capacityPolicy']}`.", '',
        f"Tested source: `{report['testedSourceCommit']}`.",
        f"Topology: `{report['topology']}`. One SDK process, one C peer process.", '',
        '| Observation | Measured value |', '| --- | ---: |',
        f"| Measurement duration | {report['durationSeconds']} s |",
        f"| In-window completed transactions | {report['completedTransactions']:,} |",
        f"| Average completed TPS | {report['completedTps']:,.2f} |",
        f"| Minimum / maximum one-second TPS | {report['minimumOneSecondTps']:,} / {report['maximumOneSecondTps']:,} |",
        f"| Seconds below target / measured seconds | {report['secondsBelowTarget']} / {report['durationSeconds']} |",
        f"| Worst one-second window, zero-based offset | {report['worstSecondOffset']} s |",
        f"| Failed / unresolved | {report['failedTransactions']} / {report['unresolvedTransactions']} |",
        f"| Successes during drain, excluded from TPS | {report['lateSuccesses']} |",
        f"| P50 / P95 / P99 latency | {latency['P50Milliseconds']:.1f} / {latency['P95Milliseconds']:.1f} / {latency['P99Milliseconds']:.1f} ms |",
        f"| Maximum successful latency | {latency['MaximumMilliseconds']:.3f} ms |",
        f"| Average / peak normalized process CPU | {resources['AverageCpuPercent']:.2f}% / {resources['PeakCpuPercent']:.2f}% |",
        f"| Peak SDK working set | {resources['PeakWorkingSetMegabytes']} MiB |",
        f"| Process-wide allocated bytes per successful transaction, including drain | {allocated:,.0f} |",
        f"| Peer total process CPU / peak RSS | {report['peerCpuSeconds']:.3f} s / {report['peerPeakRssKiB']} KiB |", '',
        '## Scope', '',
        'The workload is the equal five-operation ITU MAP SMS mix over native',
        'SCTP/M3UA/SCCP/TCAP. Warmup, drain, queued sends and errors do not raise',
        'measured TPS. Latency quantiles are 0.1 ms bucket upper bounds, not an',
        'open-loop arrival SLA. The bounded PCAP corroborates protocol correctness;',
        'full SDK/peer counters, not extrapolated capture samples, establish totals.', '',
        '## Remaining Gates', '',
        *[f'- `{reason}`' for reason in report['reasons']], '',
        f"The declared capacity target is {report['targetTps']:,} successful complete transactions",
        'in every measured second for 1,800 seconds on two dedicated hosts.',
        'A shared Kafka host is not dedicated evidence. This report grants no',
        'service-change authorization. Unchanged process identities alone do not',
        'prove absence of performance interference.', '',
        'This is source-build workload evidence, not proof that a packed NuGet',
        'candidate was deployed. It is not a signed release envelope, an operator',
        'acceptance result, a production SLA or authorization to promote stable.', '',
    ])
    if 'hostMaintenance' in report:
        text += '\n## Approved Maintenance Window\n\n'
        text += 'The owner explicitly approved temporary peer-host service quiescence.\n'
        text += 'The hashed audit verifies inactive selected units before traffic and\n'
        text += 'restoration of original active/inactive states, enablement and unit-file\n'
        text += 'hashes after collection. A remote fallback timer was armed before stop\n'
        text += 'and cancelled after verified restoration. No manual Kafka configuration\n'
        text += 'or topic-data edits were made. This is a dedicated VM workload window,\n'
        text += 'not proof of exclusive physical hypervisor resources.\n'
    if 'hardware' in report:
        hardware = report['hardware']
        text += '\n## Declared Hardware\n\n'
        for name in ('sdk', 'peer'):
            node = hardware[name]
            text += f"- {name.upper()} host: {node['logicalProcessors']} logical CPUs, {node['memoryMiB']} MiB RAM, Linux `{node['kernel']}`.\n"
        text += f"\nThe private peer was constrained to one CPU at nice {report.get('peerNice', 10)}. Declared logical\n"
        text += 'CPUs do not establish dedicated physical-core performance.\n'
    if 'nativeSocketProfiles' in report:
        text += '\n## Observed Socket Profile\n\n'
        for name, value in report['nativeSocketProfiles'].items():
            text += (f"- {name.upper()}: `{value['Name']}`, effective send/receive buffers "
                     f"{value['SendBufferBytes']} / {value['ReceiveBufferBytes']} bytes; "
                     f"RTO initial/minimum/maximum {value['RtoInitialMilliseconds']} / "
                     f"{value['RtoMinimumMilliseconds']} / {value['RtoMaximumMilliseconds']} ms.\n")
        text += '\nThese are per-socket workload settings, not SDK defaults or a public-network\n'
        text += 'recommendation. Buffer requests may be clamped. The opt-in LAN profile\n'
        text += 'uses shorter recovery bounds and needs requalification on a different path.\n'
    if 'cpuSample' in report:
        sample = report['cpuSample']
        text += '\n## Exploratory CPU Sample\n\n'
        text += (f"A {sample['sampleSeconds']:.3f}-second read-only sample observed "
                 f"{sample['sdkUserCpuSeconds']:.2f} user CPU seconds and "
                 f"{sample['sdkKernelCpuSeconds']:.2f} kernel CPU seconds across SDK threads. "
                 f"CPU model: `{sample['cpuModel']}`.\n\n")
        text += 'This sample cannot identify a syscall/allocation bottleneck or assign the\n'
        text += 'full-run limit solely to SDK code, kernel, network, GC or hardware.\n'
    if 'runtimeDiagnostics' in report:
        observation = report['runtimeDiagnostics']
        text += '\n## Runtime Observations\n\n'
        text += (f"Retained {observation['observationCount']} timestamped observations; "
                 f"maximum sampling gap {observation['maximumSampleGapSeconds']:.3f} s; "
                 f"cumulative GC pause {observation['totalGcPauseMilliseconds']:.3f} ms.\n\n")
        text += observation['caveat'] + '\n'
    if 'threadPoolLimits' in report:
        limits = report['threadPoolLimits']
        text += '\n## Application Runtime Profile\n\n'
        text += f"Observed worker minimum/maximum: {limits['MinimumWorkers']} / {limits['MaximumWorkers']}.\n\n"
        text += 'Worker limits apply only to the workload application. The SDK library never\n'
        text += 'changes a consumer thread pool. Results are conditional on this declared\n'
        text += 'profile and do not establish an HTTP or mixed-service workload SLA.\n'
    if report.get('runtimeTuning', {}).get('WorkerSpinLimitEnvironment') == '0':
        text += '\nThe owned process requested `DOTNET_ThreadPool_UnfairSemaphoreSpinLimit=0`.\n'
        text += 'This is a version-specific experimental runtime tuning setting, not an SDK\n'
        text += 'default or a stable public runtime contract. Requalify it after runtime changes.\n'
    return text


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root', type=pathlib.Path)
    parser.add_argument('--source-commit', required=True)
    parser.add_argument('--json-output', type=pathlib.Path, required=True)
    parser.add_argument('--markdown-output', type=pathlib.Path, required=True)
    args = parser.parse_args()
    report = verifier.verify(args.root, args.source_commit)
    # Exclusive output preserves previously reviewed evidence rather than silently replacing it.
    with args.json_output.open('x', encoding='utf-8', newline='\n') as stream:
        stream.write(json.dumps(report, indent=2) + '\n')
    with args.markdown_output.open('x', encoding='utf-8', newline='\n') as stream:
        stream.write(render(report))
