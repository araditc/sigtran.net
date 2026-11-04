using System.Text.Json;

namespace Sigtran.Qualification;

/// <summary>Pins result envelopes for every mandatory product gate; omitted gates never imply success.</summary>
internal sealed record QualificationPolicy(int SchemaVersion, string PackageId, string Version, string ScopeId, GateEvidence[] Gates);

/// <summary>References a reviewed envelope by immutable digest.</summary>
internal sealed record GateEvidence(string Id, string? BundlePath, string? BundleSha256);

/// <summary>Returns failures for a single mandatory gate without exposing private artifact contents.</summary>
internal sealed record GateDecision(string Id, bool Passed, string[] Errors);

/// <summary>Aggregates actual result validation into a fail-closed release decision.</summary>
internal sealed record ReleaseEvaluation(string Decision, GateDecision[] Gates, string[] Errors)
{
    internal static readonly string[] RequiredGates =
    ["native-linux-sctp", "external-m3ua", "full-stack-traffic", "independent-m2pa", "operator-profile",
     "capacity-target", "multi-host-soak", "operations-runtime", "kubernetes-sctp", "trusted-signing",
     "public-api-baseline", "protected-publication", "wire-conformance", "native-multihoming", "mtp3-m2pa"];

    internal static ReleaseEvaluation Evaluate(QualificationPolicy policy, string root,
        CandidateContext? context = null, IEvidenceAttestationVerifier? verifier = null)
    {
        List<string> errors = [];
        if (context is null) errors.Add("An actual candidate package and source commit are required.");
        else if (context.PackageVersion != policy.Version) errors.Add("Candidate package version does not match stable policy.");
        if (policy.SchemaVersion != 2 || policy.PackageId != "Sigtran.NET" || policy.Version != "1.0.0"
            || policy.ScopeId != "itu-map-sms-v1") errors.Add("Unknown qualification target/schema.");
        if (policy.Gates is null || policy.Gates.Any(g => g is null)
            || !policy.Gates.Select(g => g.Id).Order().SequenceEqual(RequiredGates.Order()))
            errors.Add("Exactly the mandatory product gates are required, without duplicates or omissions.");
        if (policy.Gates?.Any(g => g is null) == true) return new("NO-GO", [], errors.ToArray());
        List<GateDecision> gates = [];
        foreach (GateEvidence gate in policy.Gates ?? [])
        {
            List<string> failures = [];
            try
            {
                if (string.IsNullOrEmpty(gate.BundlePath) || !EvidenceFiles.IsDigest(gate.BundleSha256!))
                    failures.Add("No pinned candidate evidence envelope.");
                else
                {
                    string path = EvidenceFiles.Resolve(root, gate.BundlePath);
                    if (EvidenceFiles.Hash(path) != gate.BundleSha256) failures.Add("Envelope digest mismatch.");
                    else
                    {
                        EvidenceBundle bundle = Contracts.Read<EvidenceBundle>(path);
                        failures.AddRange(bundle.ValidateIntegrity(root));
                        failures.AddRange(CheckOutcome(gate.Id, bundle));
                        if (context is null) failures.Add("Candidate context missing.");
                        else failures.AddRange(context.Validate(bundle));
                        if (failures.Count == 0 && !(verifier ?? new GitHubEvidenceAttestationVerifier()).Verify(path, context!.SourceCommit))
                            failures.Add("Trusted source-bound GitHub attestation required.");
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or ArgumentException or JsonException or UnauthorizedAccessException)
            { failures.Add("Invalid or unavailable evidence envelope."); }
            gates.Add(new(gate.Id, failures.Count == 0, failures.ToArray()));
        }
        return new(errors.Count == 0 && gates.All(g => g.Passed) ? "GO" : "NO-GO", gates.ToArray(), errors.ToArray());
    }

    /// <summary>Interprets measured result fields using fixed acceptance criteria, never a manual passed flag.</summary>
    internal static string[] CheckOutcome(string gate, EvidenceBundle bundle)
    {
        List<string> errors = [];
        if (!RequiredGates.Contains(gate) || bundle.GateId != gate) errors.Add("Evidence gate mismatch.");
        RunOutcome? result = bundle.Result;
        if (result is null || result.Outcome != "passed" || result.Observations <= 0 || result.Failures != 0)
            errors.Add("A successful observed run with zero failures is required.");
        Dictionary<string, double> metrics = result?.Metrics ?? [];
        if (metrics.Values.Any(v => !double.IsFinite(v))) errors.Add("Non-finite run metric.");
        Require("report");
        if (gate is "native-linux-sctp" or "external-m3ua" or "full-stack-traffic" or "independent-m2pa"
            or "operator-profile" or "native-multihoming" or "mtp3-m2pa") { Require("pcap"); Require("comparison"); }
        switch (gate)
        {
            case "wire-conformance": Minimum("vectorCount", 1); Minimum("negativeCases", 1); Require("vectors"); break;
            case "capacity-target": Minimum("completedTps", 20000); Minimum("durationSeconds", 1800); Minimum("distinctHosts", 2); break;
            case "multi-host-soak": Minimum("durationSeconds", 259200); Minimum("faultScenarios", 3); Minimum("distinctHosts", 2); Maximum("unresolvedOperations", 0); break;
            case "native-multihoming": Minimum("pathFailures", 1); Minimum("survivingPaths", 1); break;
            case "mtp3-m2pa": Minimum("linkFailuresRecovered", 1); break;
            case "operator-profile": Minimum("operationTypes", 5); Minimum("negativeCases", 4); Require("acceptance"); break;
            case "kubernetes-sctp": Minimum("nodeFailureRecovery", 1); Minimum("policyRecovery", 1); Minimum("drain", 1); Minimum("rollback", 1); Require("cluster-validation"); break;
            case "trusted-signing": Minimum("signatureVerified", 1); Minimum("timestampVerified", 1); Require("certificate-verification"); break;
            case "public-api-baseline": Maximum("removedMembers", 0); Maximum("unreviewedAdditions", 0); Require("api-diff"); break;
            case "protected-publication": Minimum("reviewerApproval", 1); Require("protection"); break;
        }
        return errors.ToArray();

        void Minimum(string name, double threshold)
        { if (!metrics.TryGetValue(name, out double value) || !double.IsFinite(value) || value < threshold) errors.Add($"Metric {name} must be >= {threshold}."); }
        void Maximum(string name, double threshold)
        { if (!metrics.TryGetValue(name, out double value) || !double.IsFinite(value) || value < 0 || value > threshold) errors.Add($"Metric {name} must be between 0 and {threshold}."); }
        void Require(string kind)
        { if (bundle.Artifacts is null || !bundle.Artifacts.Any(a => a.Kind == kind)) errors.Add($"Required artifact kind: {kind}"); }
    }
}
