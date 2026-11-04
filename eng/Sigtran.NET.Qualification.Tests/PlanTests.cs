using Sigtran.Qualification;

internal static partial class Tests
{
    private static void RegisterPlan()
    {
        Cases.Add(("Runner plan binds peer address and capture to observed topology", () =>
        {
            var plan = LabPlan.Create(ValidTopology(), [ValidHost("sdk", "sdk", 'a'), ValidHost("peer", "peer", 'b')],
                "run-1", "endurance", "peer-restart", DateTimeOffset.UtcNow);
            Check(plan["REMOTE_IP"] == "192.0.2.11" && plan["CAPTURE_INTERFACE"] == "eth0"
                && plan["QUALIFICATION_PROFILE"] == "endurance", "runner binding");
        }));
        Cases.Add(("Missing capture privilege prevents launch-plan generation", () =>
        {
            try
            {
                LabPlan.Create(ValidTopology(), [ValidHost("sdk", "sdk", 'a') with { SudoAvailable = false },
                    ValidHost("peer", "peer", 'b')], "run-1", "smoke", "peer-restart", DateTimeOffset.UtcNow);
                throw new InvalidOperationException("unqualified host accepted");
            }
            catch (ArgumentException) { }
        }));
    }
}
