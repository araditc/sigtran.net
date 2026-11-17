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
BUNDLE_FILES = (contract.CASE, contract.OWNER, contract.REVIEW_POLICY, contract.REVIEW_REQUEST)


def snapshot(root):
    """Pin exact decision bytes, including filenames and JSON formatting."""
    return {name: contract.evidence.capacity.digest(contract.evidence.relative_file(root, name)) for name in BUNDLE_FILES}


def bundle_digest(files):
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
    value = api('repos/' + repository + '/contents/' + path + '?ref=' + commit)
    require(value['type'] == 'file' and value['encoding'] == 'base64' and 0 < value['size'] <= 32768
            and value['path'] == path and isinstance(value['content'], str) and len(value['content']) <= 65536,
            'Unexpected remote decision file')
    data = base64.b64decode(value['content'].replace('\n', ''), validate=True)
    require(len(data) == value['size'], 'Remote decision size differs')
    return hashlib.sha256(data).hexdigest()


def live_review(material, files, digest, case_id, now, api=github_read):
    """Authenticate a human review on the exact head and pre-existing base-branch trust roster."""
    policy, request = material['policy'], material['request']; repository = policy['repository']
    prefix = 'repos/' + repository + '/pulls/' + str(request['pullRequest'])
    pr = api(prefix); head, base = pr['head']['sha'], pr['base']['sha']
    require(pr['number'] == request['pullRequest'] and pr['base']['repo']['full_name'] == repository
            and pr['head']['repo']['full_name'] == repository and pr['base']['ref'] == 'main'
            and pr['draft'] is False and (pr['state'] == 'open' or (pr['state'] == 'closed' and pr['merged'] is True))
            and re.fullmatch('[0-9a-f]{40}', head) and re.fullmatch('[0-9a-f]{40}', base), 'Review PR identity/state differs')
    require(content_digest(api, repository, contract.REVIEW_POLICY, base) == files[contract.REVIEW_POLICY],
            'Reviewer roster was not already admitted on base branch')
    for path, expected in files.items():
        require(content_digest(api, repository, path, head) == expected, 'Reviewed head contains different case bytes')
    allowed = {name.lower() for name in policy['allowedReviewers']}
    excluded = {name.lower() for name in policy['excludedReviewers']} | {pr['user']['login'].lower()}
    latest = {}
    for page in range(1, 4):
        reviews = api(prefix + '/reviews?per_page=100&page=' + str(page))
        require(isinstance(reviews, list) and len(reviews) <= 100, 'Unbounded review response')
        for review in reviews:
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
    return dict(pullRequest=request['pullRequest'], repository=repository, approvals=approvals, authenticated=True)


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
        if not material['reviewerDesignated']: result['blockers'].append('independent-reviewer-not-designated')
        if not material['requestAssigned']: result['blockers'].append('independent-review-pr-not-assigned')
        if result['blockers']: return result
        if not verify_review:
            result['blockers'].append('live-independent-review-verification-required'); return result
        for path in BUNDLE_FILES:
            committed = subprocess.check_output(['git', 'show', 'HEAD:' + path], cwd=root, timeout=15)
            require(hashlib.sha256(committed).hexdigest() == files[path], 'Uncommitted decision file')
        result['review'] = live_review(material, files, digest, audit['case']['id'], now, api or github_read)
        result.update(status='CLOSED_WITH_ACCEPTED_DEVIATION', independentReviewApproved=True,
                      administrativeClosureApproved=True, acceptanceExceptionGranted=True)
    except (ValueError, KeyError, TypeError, OSError, subprocess.SubprocessError):
        result['status'] = 'INELIGIBLE'; result['blockers'].append('case-or-authenticated-review-validation-failed')
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--verify-review', action='store_true', help='Read actual GitHub review state; never submit a review.')
    parser.add_argument('--output', type=pathlib.Path)
    args = parser.parse_args(); report = assess(verify_review=args.verify_review)
    if args.output:
        output = args.output.resolve(); allowed = (contract.ROOT / 'artifacts/qualification').resolve()
        require(allowed in output.parents and not args.output.is_symlink(), 'Output must be a new private qualification artifact')
        with output.open('x', encoding='utf-8', newline='\n') as stream: json.dump(report, stream, indent=2); stream.write('\n')
    print(json.dumps(report, indent=2))
    return 0 if report['administrativeClosureApproved'] else (3 if report['status'] == 'PENDING_INDEPENDENT_REVIEW' else 1)


if __name__ == '__main__': raise SystemExit(main())
