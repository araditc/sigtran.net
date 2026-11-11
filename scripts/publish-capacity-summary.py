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
        f"Tested source: `{report['testedSourceCommit']}`.",
        f"Topology: `{report['topology']}`. One SDK process, one C peer process.", '',
        '| Observation | Measured value |', '| --- | ---: |',
        f"| Measurement duration | {report['durationSeconds']} s |",
        f"| In-window completed transactions | {report['completedTransactions']:,} |",
        f"| Average completed TPS | {report['completedTps']:,.2f} |",
        f"| Minimum / maximum one-second TPS | {report['minimumOneSecondTps']:,} / {report['maximumOneSecondTps']:,} |",
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
        'The fixed capacity target remains 20,000 successful complete transactions',
        'in every measured second for 1,800 seconds on two dedicated hosts.',
        'A shared Kafka host is not dedicated evidence. No Kafka service or',
        'configuration changes are authorized by this report. Unchanged process',
        'identities do not prove absence of performance interference.', '',
        'This is source-build workload evidence, not proof that a packed NuGet',
        'candidate was deployed. It is not a signed release envelope, an operator',
        'acceptance result, a production SLA or authorization to promote stable.', '',
    ])
    if 'hardware' in report:
        hardware = report['hardware']
        text += '\n## Declared Hardware\n\n'
        for name in ('sdk', 'peer'):
            node = hardware[name]
            text += f"- {name.upper()} host: {node['logicalProcessors']} logical CPUs, {node['memoryMiB']} MiB RAM, Linux `{node['kernel']}`.\n"
        text += '\nThe private peer was constrained to one CPU at nice 10. Declared logical\n'
        text += 'CPUs do not establish dedicated physical-core performance.\n'
    if 'cpuSample' in report:
        sample = report['cpuSample']
        text += '\n## Exploratory CPU Sample\n\n'
        text += (f"A {sample['sampleSeconds']:.3f}-second read-only sample observed "
                 f"{sample['sdkUserCpuSeconds']:.2f} user CPU seconds and "
                 f"{sample['sdkKernelCpuSeconds']:.2f} kernel CPU seconds across SDK threads. "
                 f"CPU model: `{sample['cpuModel']}`.\n\n")
        text += 'This sample cannot identify a syscall/allocation bottleneck or assign the\n'
        text += 'full-run limit solely to SDK code, kernel, network, GC or hardware.\n'
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
    with args.json_output.open('x', encoding='utf-8') as stream:
        stream.write(json.dumps(report, indent=2) + '\n')
    with args.markdown_output.open('x', encoding='utf-8') as stream:
        stream.write(render(report))
