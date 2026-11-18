#!/usr/bin/env python3
"""Read GitHub lab control metadata, not credentials or live Kubernetes evidence."""

import argparse
import json
import os
from pathlib import Path
import re
import shutil
import subprocess


ENVIRONMENT = "kubernetes-sctp"
GROUP = "sigtran-kubernetes-lab"
WORKFLOW = ".github/workflows/phase56-kubernetes-sctp.yml@refs/heads/main"
LABELS = {"self-hosted", "linux", GROUP}
SECRETS = {"KUBECONFIG_B64", "K8S_REMOTE_IP", "K8S_REMOTE_PORT"}
VARIABLES = {"K8S_RAW_EVIDENCE_ROOT", "K8S_CNI_DAEMONSET", "K8S_CLUSTER_UID", "K8S_QUALIFICATION_NAMESPACE_UID"}


def entries(result, key):
    """Reject partial API pages rather than inferring absence or exclusivity."""
    values = result[key]
    if not isinstance(values, list) or type(result["total_count"]) is not int or result["total_count"] != len(values):
        raise ValueError("Incomplete metadata page")
    return values


def gh_read(endpoint):
    """Request bounded metadata from GitHub; never print raw API responses or errors."""
    executable = shutil.which("gh")
    if executable is None and os.name == "nt":
        path = Path(os.environ.get("ProgramFiles", "C:/Program Files")) / "GitHub CLI/gh.exe"
        executable = str(path) if path.is_file() else None
    if executable is None:
        raise ValueError("GitHub CLI not found")
    result = subprocess.run([executable, "api", "--hostname", "github.com", "--method", "GET", endpoint],
                            capture_output=True, check=True, timeout=20)
    if len(result.stdout) > 2 * 1024 * 1024:
        raise ValueError("Metadata response exceeds the read limit")
    return json.loads(result.stdout)


def assess(repo, read=gh_read):
    """Check orthogonal controls; failure of one read does not hide other blockers."""
    if not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9-]{0,38}/[A-Za-z0-9_.-]{1,100}", repo) or repo.split("/")[1] in (".", ".."):
        raise ValueError("Expected a GitHub OWNER/REPO")
    prefix = f"repos/{repo}"
    environment = f"{prefix}/environments/{ENVIRONMENT}"
    groups = f"orgs/{repo.split('/')[0]}/actions/runner-groups"
    checks = {}

    def check(name, callback):
        try:
            checks[name] = "pass" if callback() else "missing-or-mismatched"
        except (KeyError, ValueError, TypeError, AttributeError, OSError, subprocess.SubprocessError):
            checks[name] = "unavailable-or-invalid"

    def approval():
        value = read(environment)
        policy = value["deployment_branch_policy"]
        rules = [rule for rule in value["protection_rules"] if rule["type"] == "required_reviewers"]
        reviewers = rules[0]["reviewers"] if len(rules) == 1 else []
        return (value["name"] == ENVIRONMENT and policy["custom_branch_policies"] is True
                and policy["protected_branches"] is False and len(rules) == 1
                and rules[0]["prevent_self_review"] is True and isinstance(reviewers, list) and bool(reviewers)
                and all(entry["type"] in ("User", "Team") and type(entry["reviewer"]["id"]) is int
                        and entry["reviewer"]["id"] > 0 for entry in reviewers))

    def branch_policies():
        records = entries(read(f"{environment}/deployment-branch-policies?per_page=100"), "branch_policies")
        return [(item["type"], item["name"]) for item in records] == [("branch", "main")]

    def env_secrets():
        records = entries(read(f"{environment}/secrets?per_page=100"), "secrets")
        return SECRETS.issubset({item["name"] for item in records})

    activation = None

    def env_variables():
        nonlocal activation
        records = entries(read(f"{environment}/variables?per_page=100"), "variables")
        values = {item["name"]: item["value"] for item in records}
        activation = values.get("K8S_LAB_ENABLED") == "true"
        return all(isinstance(values.get(name), str) and values[name].strip() for name in VARIABLES)

    group_id = None

    def runner_group():
        nonlocal group_id
        values = entries(read(f"{groups}?per_page=100"), "runner_groups")
        matches = [item for item in values if item["name"] == GROUP]
        if len(matches) != 1:
            return False
        group = matches[0]
        if type(group["id"]) is not int or group["id"] <= 0:
            raise ValueError("Invalid runner group ID")
        group_id = group["id"]
        return (group["visibility"] == "selected" and group["default"] is False
                and group["allows_public_repositories"] is True and group["restricted_to_workflows"] is True
                and group["selected_workflows"] == [f"{repo}/{WORKFLOW}"])

    def runner_repo_scope():
        values = entries(read(f"{groups}/{group_id}/repositories?per_page=100"), "repositories")
        return [item["full_name"] for item in values] == [repo]

    def online_runner():
        values = entries(read(f"{groups}/{group_id}/runners?per_page=100"), "runners")
        return any(item["status"] == "online" and LABELS.issubset({label["name"].lower() for label in item["labels"]})
                   for item in values)

    check("mainProtected", lambda: read(f"{prefix}/branches/main")["protected"] is True)
    check("environmentApproval", approval)
    check("environmentMainOnly", branch_policies)
    check("environmentSecretNames", env_secrets)
    check("environmentVariables", env_variables)
    check("restrictedRunnerGroup", runner_group)
    if group_id is not None:
        check("runnerGroupRepositoryScope", runner_repo_scope)
        check("onlineRunnerInGroup", online_runner)
    else:
        checks.update(runnerGroupRepositoryScope="not-checked", onlineRunnerInGroup="not-checked")
    configured = all(value == "pass" for value in checks.values())
    return {"githubMetadataConfigured": configured, "activationEnabled": activation, "checks": checks,
            "adminBypassVerified": False, "runnerIsolationVerifiedOnHost": False,
            "clusterVerified": False, "peerVerified": False, "qualificationPassed": False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", default="araditc/sigtran.net")
    try:
        result = assess(parser.parse_args().repo)
    except ValueError as exc:
        parser.error(str(exc))
    print(json.dumps(result, indent=2))
    return 0 if result["githubMetadataConfigured"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
