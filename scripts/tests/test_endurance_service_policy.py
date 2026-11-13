"""The library-update guard is exact-service, additive, self-expiring and safely removable."""
import datetime
import hashlib
import importlib.util
import json
import pathlib
import shutil
import subprocess
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('endurance_service_policy', pathlib.Path(__file__).resolve().parents[1] / 'endurance-service-policy.py')
policy = importlib.util.module_from_spec(spec); spec.loader.exec_module(policy)


class PolicyTests(unittest.TestCase):
    def test_scope_is_exact_and_never_replaces_global_restart_mode(self):
        value = policy.content('example-run', 'peer', '2030-01-01T00:00:00+00:00').decode()
        self.assertIn('if (time() < ', value)
        self.assertIn('$nrconf{override_rc}{qr(^', value)
        self.assertNotIn('$nrconf{restart}', value)
        self.assertNotIn('$nrconf{override_rc} =', value)
        self.assertNotIn('kafka', value)
        for name, role in [('../unsafe', 'peer'), ('valid', 'ssh')]:
            with self.assertRaises(ValueError): policy.service(name, role)

    @unittest.skipUnless(shutil.which('perl'), 'Installed policy language checked on Linux before launch')
    def test_perl_policy_is_additive_and_expired_snippet_is_inert(self):
        future = (datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(days=1)).isoformat()
        for expiry, expected in [(future, '0'), ('1970-01-01T00:00:00+00:00', 'unset')]:
            code = ('our %nrconf=(override_rc=>{qr(^ssh\\.service$)=>1});\n' + policy.content('example-run', 'peer', expiry).decode()
                    + '\nmy $unit="sigtran-endurance-peer-example-run.service";my $v="unset";'
                    'for my $p (sort keys %{$nrconf{override_rc}}) {if ($unit =~ $p) {$v=$nrconf{override_rc}{$p};last;}}'
                    'my $ssh=0;for my $p (keys %{$nrconf{override_rc}}) {if ("ssh.service" =~ $p) {$ssh=$nrconf{override_rc}{$p};}}'
                    'print "$v,$ssh";')
            result = subprocess.run(['perl', '-e', code], check=True, text=True, capture_output=True)
            self.assertEqual(result.stdout, expected + ',1')

    def test_removal_preserves_changed_configuration_and_is_idempotent(self):
        with tempfile.TemporaryDirectory() as temporary:
            base = pathlib.Path(temporary); root = base / 'example-run'; root.mkdir()
            directory = base / 'policies'; directory.mkdir()
            path = directory / 'sigtran-endurance-example-run.conf'
            body = policy.content(root.name, 'sdk', '2030-01-01T00:00:00+00:00')
            state = dict(runId=root.name, role='sdk', unit=policy.service(root.name, 'sdk'), snippetSha256=hashlib.sha256(body).hexdigest())
            (root / policy.STATE).write_text(json.dumps(state)); path.write_bytes(b'Changed externally')
            with self.assertRaises(ValueError): policy.release(root, directory)
            self.assertEqual(path.read_bytes(), b'Changed externally')
            path.write_bytes(body)
            self.assertTrue(policy.release(root, directory)['removed'])
            self.assertTrue(policy.release(root, directory)['policyAbsent'])
            self.assertFalse(path.exists())


if __name__ == '__main__': unittest.main()
