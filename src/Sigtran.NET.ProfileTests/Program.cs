using Sigtran.NET.Core.Profiles;
using Sigtran.NET.Layers.MAP;
using Sigtran.NET.Layers.SCCP;

Run("valid profile materializes existing SCCP and MAP primitives", ValidProfileMaterializesExistingPrimitives);
Run("invalid profile settings fail deterministically", InvalidProfileSettingsFailDeterministically);
Run("duplicate and unsupported MAP operations are rejected", DuplicateAndUnsupportedMapOperationsAreRejected);
Run("conflicting SCCP translation prefixes are rejected", ConflictingTranslationPrefixesAreRejected);

Console.WriteLine("All SIGTRAN network profile tests passed.");

static void ValidProfileMaterializesExistingPrimitives()
{
    SigtranNetworkProfileDefinition definition = new(
        name: "synthetic-lab",
        pointCodeFormat: SigtranPointCodeFormat.Itu14Bit,
        networkIndicator: 2,
        globalTitleTranslations:
        [
            new SccpGlobalTitleTranslationRule(
                "map-gateway",
                "44123",
                destinationPointCode: 1234,
                subsystemNumber: SubsystemNumber.MAP)
        ],
        allowedMapSmsOperations:
        [
            MapSmsOperationCode.SendRoutingInfoForShortMessage,
            MapSmsOperationCode.MtForwardShortMessage
        ]);

    SigtranNetworkProfileValidationResult validation =
        SigtranNetworkProfileValidator.Validate(definition);
    Assert(validation.IsValid, "Synthetic ITU profile should validate.");

    SigtranNetworkProfile profile = SigtranNetworkProfile.Create(definition);
    Assert(profile.Name == "synthetic-lab", "Profile name mismatch.");
    Assert(profile.NetworkIndicator == 2, "Network indicator mismatch.");
    Assert(
        profile.PointCodeFormat == SigtranPointCodeFormat.Itu14Bit,
        "Point-code format mismatch.");
    Assert(
        profile.CompatibilityMode == SigtranCompatibilityMode.Strict,
        "Strict compatibility should be the default.");
    Assert(
        profile.CreateGlobalTitleTranslationTable().Snapshot().Count == 1,
        "SCCP translation rules should materialize through the existing translation table.");
    Assert(
        profile.IsMapSmsOperationAllowed(MapSmsOperationCode.MtForwardShortMessage),
        "MT-ForwardSM should be admitted.");
    Assert(
        !profile.IsMapSmsOperationAllowed(MapSmsOperationCode.MoForwardShortMessage),
        "MO-ForwardSM should remain outside this profile allowlist.");

    MapSmsOperationProfile operationProfile = profile.GetMapSmsOperationProfile(
        MapSmsOperationCode.SendRoutingInfoForShortMessage);
    Assert(operationProfile.Timeout > TimeSpan.Zero, "Existing MAP timeout policy must be reused.");
    Assert(
        ReferenceEquals(
            operationProfile,
            MapSmsOperationProfiles.Get(MapSmsOperationCode.SendRoutingInfoForShortMessage)),
        "The network profile must expose the existing MAP operation profile, not duplicate it.");
}

static void InvalidProfileSettingsFailDeterministically()
{
    SigtranNetworkProfileDefinition definition = new(
        name: " ",
        pointCodeFormat: SigtranPointCodeFormat.Ansi24Bit,
        networkIndicator: 4,
        globalTitleTranslations: null,
        allowedMapSmsOperations: [],
        compatibilityMode: SigtranCompatibilityMode.Compatibility);

    SigtranNetworkProfileValidationResult result =
        SigtranNetworkProfileValidator.Validate(definition);

    Assert(!result.IsValid, "Unsupported profile settings must fail validation.");
    AssertIssue(result, "PROFILE_NAME_REQUIRED");
    AssertIssue(result, "POINT_CODE_FORMAT_UNSUPPORTED");
    AssertIssue(result, "NETWORK_INDICATOR_OUT_OF_RANGE");
    AssertIssue(result, "COMPATIBILITY_MODE_UNSUPPORTED");
    AssertIssue(result, "MAP_SMS_OPERATION_SET_REQUIRED");

    string[] orderedCodes = result.Issues.Select(issue => issue.Code).ToArray();
    string[] sortedCodes = orderedCodes.OrderBy(code => code, StringComparer.Ordinal).ToArray();
    Assert(
        orderedCodes.SequenceEqual(sortedCodes, StringComparer.Ordinal),
        "Validation issues must be deterministically ordered.");

    try
    {
        _ = SigtranNetworkProfile.Create(definition);
        throw new InvalidOperationException("Invalid definition unexpectedly materialized.");
    }
    catch (ArgumentException exception)
    {
        Assert(
            exception.Message.Contains("POINT_CODE_FORMAT_UNSUPPORTED", StringComparison.Ordinal),
            "Materialization error should retain structured validation codes.");
    }
}

static void DuplicateAndUnsupportedMapOperationsAreRejected()
{
    SigtranNetworkProfileDefinition definition = new(
        name: "map-policy-negative",
        pointCodeFormat: SigtranPointCodeFormat.Itu14Bit,
        networkIndicator: 0,
        globalTitleTranslations: null,
        allowedMapSmsOperations:
        [
            MapSmsOperationCode.MoForwardShortMessage,
            MapSmsOperationCode.MoForwardShortMessage,
            (MapSmsOperationCode)255
        ]);

    SigtranNetworkProfileValidationResult result =
        SigtranNetworkProfileValidator.Validate(definition);

    AssertIssue(result, "MAP_SMS_OPERATION_DUPLICATE");
    AssertIssue(result, "MAP_SMS_OPERATION_UNSUPPORTED");
}

static void ConflictingTranslationPrefixesAreRejected()
{
    SigtranNetworkProfileDefinition definition = new(
        name: "translation-negative",
        pointCodeFormat: SigtranPointCodeFormat.Itu14Bit,
        networkIndicator: 0,
        globalTitleTranslations:
        [
            new SccpGlobalTitleTranslationRule(
                "primary",
                "4477",
                destinationPointCode: 100,
                subsystemNumber: SubsystemNumber.MAP),
            new SccpGlobalTitleTranslationRule(
                "secondary",
                "4477",
                destinationPointCode: 200,
                subsystemNumber: SubsystemNumber.MAP)
        ],
        allowedMapSmsOperations: [MapSmsOperationCode.AlertServiceCentre]);

    SigtranNetworkProfileValidationResult result =
        SigtranNetworkProfileValidator.Validate(definition);
    AssertIssue(result, "SCCP_TRANSLATION_PREFIX_CONFLICT");
}

static void AssertIssue(SigtranNetworkProfileValidationResult result, string code)
{
    Assert(
        result.Issues.Any(issue => string.Equals(issue.Code, code, StringComparison.Ordinal)),
        $"Expected validation issue '{code}'.");
}

static void Run(string name, Action test)
{
    try
    {
        test();
        Console.WriteLine($"PASS: {name}");
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"FAIL: {name}: {exception.Message}");
        Environment.ExitCode = 1;
        throw;
    }
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
