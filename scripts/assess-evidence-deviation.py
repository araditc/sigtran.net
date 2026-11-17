#!/usr/bin/env python3
"""Assess administrative evidence acceptance separately from strict release qualification."""
import argparse
import base64
import datetime
import hashlib
import importlib.util
import json
import os
import pathlib
import re
import shutil
import subprocess

spec = importlib.util.spec_from_file_location('acceptance_contract', pathlib.Path(__file__).with_name('evidence-deviation.py'))
contract = importlib.util.module_from_spec(spec); spec.loader.exec_module(contract)
require = contract.require
BUNDLE_FILES = (contract.CASE, contract.OWNER, contract.REVIEW_POLICY, contract.REVIEW_REQUEST, contract.RISK_EVENTS)
CURRENT_REPORT = 'docs/evidence/ENDURANCE_REVIEW_REFRESH_20261117.json'


def snapshot(root):
    """Pin exact decision bytes, including filenames and JSON formatting."""
    return {name: contract.evidence.capacity.digest(contract.evidence.relative_file(root, name)) for name in BUNDLE_FILES}


def bundle_digest(files):
    """Ignore inventory ordering without normalizing the referenced decision bytes."""
    return hashlib.sha256(json.dumps(files, sort_keys=True, separators=(',', ':')).encode('ascii')).hexdigest()


def github_read(endpoint):
    """Use only read-only GitHub API calls; never print credentials or trust cached approval flags."""
    executable = shutil.which('gh')
    if executable is None and os.name == 'nt':
        candidate = pathlib.Path(os.environ.get('ProgramFiles', 'C:/Program Files')) / 'GitHub CLI/gh.exe'
        if candidate.is_file(): executable = str(candidate)
    if executable is None: raise ValueError('GitHub CLI unavailable')
    result = subprocess.run([executable, 'api', '--hostname', 'github.com', '--method', 'GET',
        '-H', 'Accept: application/vnd.github+json', '-H', 'X-GitHub-Api-Version: 2026-03-10', endpoint],
        capture_output=True, check=True, timeout=30)
    require(len(result.stdout) <= 2 * 1048576, 'Review response exceeds budget')
    return contract.evidence.json_bytes(result.stdout)


def content_digest(api, repository, path, commit):
    """Verify bounded remote decision bytes without extracting files or trusting a JSON success flag."""
    value = api('repos/' + repository + '/contents/' + path + '?ref=' + commit)
    require(value['type'] == 'file' and value['encoding'] == 'base64' and 0 < value['size'] <= 32768
            and value['path'] == path and isinstance(value['content'], str) and len(value['content']) <= 65536,
            'Unexpected remote decision file')
    data = base64.b64decode(value['content'].replace('\n', ''), validate=True)
    require(len(data) == value['size'], 'Remote decision size differs')
    return hashlib.sha256(data).hexdigest()


def live_review(material, files, digest, case_id, now, expected_head, api=github_read):
    """Authenticate a human review on the exact head and pre-existing base-branch trust roster."""
    policy, request = material['policy'], material['request']; repository = policy['repository']
    prefix = 'repos/' + repository + '/pulls/' + str(request['pullRequest'])
    pr = api(prefix); head, base = pr['head']['sha'], pr['base']['sha']
    require(pr['number'] == request['pullRequest'] and pr['base']['repo']['full_name'] == repository
            and pr['head']['repo']['full_name'] == repository and pr['base']['ref'] == 'main'
            and pr['draft'] is False and (pr['state'] == 'open' or (pr['state'] == 'closed' and pr['merged'] is True))
            and re.fullmatch('[0-9a-f]{40}', head) and re.fullmatch('[0-9a-f]{40}', base)
            and head == expected_head, 'Review PR identity/state differs')
    for path in (contract.CASE, contract.OWNER, contract.REVIEW_POLICY):
        require(content_digest(api, repository, path, base) == files[path],
                'Case scope, owner authority and reviewer trust must already be admitted on base branch')
    for path, expected in files.items():
        require(content_digest(api, repository, path, head) == expected, 'Reviewed head contains different case bytes')
    allowed = {name.lower() for name in policy['allowedReviewers']}
    excluded = {name.lower() for name in policy['excludedReviewers']} | {pr['user']['login'].lower()}
    latest = {}
    for page in range(1, 4):
        reviews = api(prefix + '/reviews?per_page=100&page=' + str(page))
        require(isinstance(reviews, list) and len(reviews) <= 100, 'Unbounded review response')
        for review in reviews:
            require(isinstance(review['user']['login'], str), 'Invalid reviewer identity')
            login = review['user']['login'].lower()
            if login not in allowed or login in excluded or review['user']['type'] != 'User': continue
            if review['state'] not in ('APPROVED', 'CHANGES_REQUESTED', 'DISMISSED'): continue
            require(type(review['id']) is int and review['id'] > 0, 'Invalid review identity')
            if login not in latest or review['id'] > latest[login]['id']: latest[login] = review
        if len(reviews) < 100: break
    else: raise ValueError('Review pagination exceeds budget')
    require(latest, 'No independent human approval')
    approvals = []
    for login, review in latest.items():
        require(review['state'] == 'APPROVED' and review['commit_id'] == head, 'Rejected, dismissed or stale review')
        submitted = contract.evidence.utc_time(review['submitted_at'])
        require(datetime.timedelta(0) <= now - submitted <= datetime.timedelta(days=policy['maximumReviewAgeDays']),
                'Expired or future-dated review')
        body = review['body']; require(isinstance(body, str) and len(body) <= 65536, 'Unbounded review rationale')
        tags = ['Evidence-Deviation-Case: ' + case_id, 'Evidence-Deviation-SHA256: ' + digest,
                'Disposition: ACCEPT_SCOPED_DEVIATION']; lines = body.splitlines()
        require(all(lines.count(tag) == 1 for tag in tags)
                and len('\n'.join(line for line in lines if line not in tags).strip()) >= 40, 'Review scope/digest/rationale differs')
        approvals.append(dict(login=login, reviewId=review['id'], commit=head, submittedAtUtc=review['submitted_at']))
    final = api(prefix)
    require(final['head']['sha'] == head and final['base']['sha'] == base and final['draft'] is False
            and (final['state'] == 'open' or (final['state'] == 'closed' and final['merged'] is True)),
            'PR changed while review was being verified')
    return dict(pullRequest=request['pullRequest'], repository=repository, approvals=approvals, authenticated=True)


def risk_events(root, owner, case_id):
    """Keep reopening events append-only across committed history; clearing the file cannot revive old approval."""
    require(subprocess.check_output(['git', 'rev-parse', '--is-shallow-repository'], cwd=root,
            text=True, timeout=15).strip() == 'false', 'Complete history is required for reopening-event verification')
    value = contract.evidence.read_json(contract.evidence.relative_file(root, contract.RISK_EVENTS))
    require(set(value) == {'schemaVersion', 'caseId', 'events'} and value['schemaVersion'] == 1
            and value['caseId'] == case_id and isinstance(value['events'], list) and len(value['events']) <= 32,
            'Invalid reopening event register')
    identities = set()
    for item in value['events']:
        require(set(item) == {'id', 'kind', 'observedAtUtc', 'reference'} and type(item['id']) is int
                and item['id'] > 0 and item['id'] not in identities and item['kind'] in owner['revocationTriggers']
                and isinstance(item['reference'], str) and 1 <= len(item['reference']) <= 512, 'Invalid reopening event')
        identities.add(item['id']); contract.evidence.utc_time(item['observedAtUtc'])
    commits = subprocess.check_output(['git', 'log', '--format=%H', '--max-count=65', '--', contract.RISK_EVENTS],
                                      cwd=root, text=True, timeout=15).splitlines()
    require(len(commits) <= 64, 'Reopening history requires a new reviewed case')
    for commit in commits:
        old = contract.evidence.json_bytes(subprocess.check_output(['git', 'show', commit + ':' + contract.RISK_EVENTS], cwd=root, timeout=15))
        require(value['events'][:len(old['events'])] == old['events'], 'Recorded reopening events cannot be erased or rewritten')
    return value['events']


def assess(root=contract.ROOT, verify_review=False, api=None, now=None):
    """Pending/invalid evidence never grants closure, technical qualification or stable GO."""
    root = pathlib.Path(root).resolve(); now = now or datetime.datetime.now(datetime.timezone.utc)
    result = dict(schemaVersion=1, evidenceKind='scoped-evidence-acceptance-assessment',
        caseId='endurance-managed-memory-observations', status='INELIGIBLE', ownerRiskAccepted=False,
        independentReviewApproved=False, administrativeClosureApproved=False, acceptanceExceptionGranted=False,
        originalVerdict='rejected', qualificationPassed=False, stablePromotion=False, rootCauseProven=False,
        freshEnduranceRunStarted=False, privateArtifactsReplayed=False, blockers=[])
    try:
        audit = contract.dossier(root); owner = contract.owner_acceptance(audit, root)
        material = contract.review_material(audit, owner, root); files = snapshot(root); digest = bundle_digest(files)
        result.update(ownerRiskAccepted=True, dossierSha256=audit['dossierSha256'], bundleSha256=digest,
                      decisionFileSha256=files, status='PENDING_INDEPENDENT_REVIEW')
        events = risk_events(root, owner, audit['case']['id'])
        if events:
            result.update(status='RISK_REOPENED', blockers=['recorded-risk-reopening-event']); return result
        if not material['reviewerDesignated']: result['blockers'].append('independent-reviewer-not-designated')
        if not material['requestAssigned']: result['blockers'].append('independent-review-pr-not-assigned')
        if result['blockers']: return result
        if not verify_review:
            result['blockers'].append('live-independent-review-verification-required'); return result
        for path in BUNDLE_FILES:
            committed = subprocess.check_output(['git', 'show', 'HEAD:' + path], cwd=root, timeout=15)
            require(hashlib.sha256(committed).hexdigest() == files[path], 'Uncommitted decision file')
        head = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True, timeout=15).strip()
        result['review'] = live_review(material, files, digest, audit['case']['id'], now, head, api or github_read)
        require(snapshot(root) == files and subprocess.check_output(['git', 'rev-parse', 'HEAD'],
                cwd=root, text=True, timeout=15).strip() == head, 'Local case changed during review verification')
        result.update(status='CLOSED_WITH_ACCEPTED_DEVIATION', independentReviewApproved=True,
                      administrativeClosureApproved=True, acceptanceExceptionGranted=True)
    except (ValueError, KeyError, TypeError, OSError, subprocess.SubprocessError):
        result['status'] = 'INELIGIBLE'; result['blockers'].append('case-or-authenticated-review-validation-failed')
    return result


def write_assessment(path, report, root=contract.ROOT):
    """Create only a new owned report; never write into original evidence, source files or an existing receipt."""
    root = pathlib.Path(root).resolve(); allowed = root / 'artifacts/qualification/evidence-deviations'
    output = path.resolve()
    require(allowed in output.parents and all(not p.is_symlink() for p in (path, *path.parents)
            if p == root or root in p.parents), 'Output must stay in the dedicated assessment artifact directory')
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open('x', encoding='utf-8', newline='\n') as stream: json.dump(report, stream, indent=2); stream.write('\n')


def remaining_review_steps(current):
    """Describe actual open prerequisites instead of freezing the initial empty-roster state."""
    require(current['status'] == 'PENDING_INDEPENDENT_REVIEW'
            and current['independentReviewApproved'] is False and current['administrativeClosureApproved'] is False,
            'A pending handoff cannot represent accepted or ineligible evidence')
    mapping = [('independent-reviewer-not-designated', 'independent-reviewer-designation'),
               ('independent-review-pr-not-assigned', 'independent-review-pr-assignment')]
    require(set(current['blockers']) <= {code for code, _ in mapping} | {'live-independent-review-verification-required'},
            'Unknown pending-review prerequisite')
    return [step for code, step in mapping if code in current['blockers']] + ['authenticated-independent-review']


def check_report(path, current):
    """Detect stale or promoted public handoff metadata by comparing it with a fresh assessment."""
    value = contract.evidence.read_json(path)
    require(set(value) == {'schemaVersion', 'recordedOn', 'assessment', 'delivery', 'remaining', 'limitations'}
            and value['schemaVersion'] == 1 and value['assessment'] == current, 'Saved handoff differs from current assessment')
    delivery = value['delivery']
    require(delivery['implementationUnits'] == 6 and delivery['independentHumanApproval'] == 'open'
            and all(delivery[key] is False for key in ('originalRawFilesModified', 'releasePolicyModified', 'measurementRepeated')),
            'Handoff cannot claim review, evidence repair or release-policy changes')
    require(value['remaining'] == remaining_review_steps(current), 'Saved remaining review tasks differ from actual prerequisites')
    return value


def main():
    """Keep pending review a nonzero exit, including when a retained report matches current facts."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--verify-review', action='store_true', help='Read actual GitHub review state; never submit a review.')
    parser.add_argument('--output', type=pathlib.Path)
    checks = parser.add_mutually_exclusive_group()
    checks.add_argument('--check-report', type=pathlib.Path, help='Check a retained public handoff against a fresh assessment.')
    checks.add_argument('--check-current-report', action='store_true', help='Check the source-controlled current handoff without changing archived receipts.')
    args = parser.parse_args(); report = assess(verify_review=args.verify_review)
    if args.check_report: check_report(args.check_report, report)
    if args.check_current_report: check_report(contract.ROOT / CURRENT_REPORT, report)
    if args.output: write_assessment(args.output, report)
    print(json.dumps(report, indent=2))
    return 0 if report['administrativeClosureApproved'] else (3 if report['status'] == 'PENDING_INDEPENDENT_REVIEW' else 1)


if __name__ == '__main__': raise SystemExit(main())
