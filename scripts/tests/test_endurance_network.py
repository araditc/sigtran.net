"""Offline scoped-network safety tests; no sudo, systemd or firewall command is executed."""
import datetime
import importlib.util
import pathlib
import types
import unittest

spec = importlib.util.spec_from_file_location('endurance_network', pathlib.Path(__file__).resolve().parents[1] / 'endurance-network.py')
network = importlib.util.module_from_spec(spec)
spec.loader.exec_module(network)


def fixture():
    return dict(runId='example-run', sdkIp='192.168.100.28', peerIp='192.168.100.43', port=33072,
                holdSeconds=10, networkFaultIds=[3, 6, 9],
                expiresAtUtc=(datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(days=3)).isoformat())


class NetworkTests(unittest.TestCase):
    def test_rules_are_single_sctp_flow_and_never_tcp_or_port_ranges(self):
        plan = network.validate_plan(fixture())
        for chain, rule in network.rules(plan, 3):
            self.assertIn(chain, ('INPUT', 'OUTPUT'))
            self.assertEqual(rule[:2], ['-p', 'sctp'])
            self.assertIn('33072', rule)
            self.assertNotIn('tcp', rule)
            self.assertIn('endurance-example-run-3', rule)
        for key, value in [('port', 22), ('peerIp', '0.0.0.0'), ('sdkIp', '192.168.100.43'), ('networkFaultIds', [1]), ('runId', '../unsafe')]:
            plan = fixture(); plan[key] = value
            with self.assertRaises(ValueError): network.validate_plan(plan)

    def test_guard_is_verified_before_any_rule_is_inserted(self):
        calls = []
        def run(arguments):
            calls.append(arguments)
            return types.SimpleNamespace(returncode=1 if '-C' in arguments else 0)
        network.partition(fixture(), 3, '/protected/network.json', run)
        guard = next(i for i, call in enumerate(calls) if call[:2] == ['systemctl', 'is-active'])
        self.assertTrue(all(i > guard for i, call in enumerate(calls) if '-I' in call and call[0] == 'iptables'))
        self.assertIn('/usr/bin/python3', calls[2])

    def test_failed_guard_never_inserts_rules(self):
        calls = []
        def run(arguments):
            calls.append(arguments)
            return types.SimpleNamespace(returncode=1 if '-C' in arguments or arguments[0] == 'systemd-run' else 0)
        with self.assertRaises(RuntimeError): network.partition(fixture(), 3, '/protected/network.json', run)
        self.assertFalse(any(call[0] == 'iptables' and '-I' in call for call in calls))

    def test_unknown_rule_status_never_counts_as_cleanup(self):
        calls = []
        def run(arguments):
            calls.append(arguments)
            return types.SimpleNamespace(returncode=4)
        with self.assertRaises(RuntimeError): network.rollback(fixture(), 3, run)
        self.assertFalse(any(call[0] == 'systemctl' and 'stop' in call for call in calls))

    def test_uncreated_future_timer_is_safe_only_when_explicitly_absent(self):
        def run(arguments):
            if '-C' in arguments: return types.SimpleNamespace(returncode=1)
            if arguments[:2] == ['systemctl', 'stop']: return types.SimpleNamespace(returncode=5)
            return types.SimpleNamespace(returncode=0, stdout='LoadState=not-found\nActiveState=inactive\n')
        network.rollback(fixture(), 3, run)

    def test_packet_counters_only_export_the_owned_tag(self):
        def run(arguments):
            self.assertEqual(arguments, ['iptables-save', '-c'])
            return types.SimpleNamespace(returncode=0, stdout='[5:100] -A OUTPUT -p sctp --comment endurance-example-run-3 -j DROP\n[999:2000] -A INPUT --comment other -j DROP\n')
        self.assertEqual(network.dropped_packets(fixture(), 3, run), 5)


if __name__ == '__main__':
    unittest.main()
