using Sigtran.NET.Core.Utilities;

namespace Sigtran.NET.Layers.TCAP;

/// <summary>
/// Provides deterministic TCAP evidence vectors for byte-level transaction validation.
/// </summary>
public static class TcapEvidenceVectors
{
    private const string Source = "SDK deterministic TCAP BER vector";

    private static readonly SigtranProtocolEvidenceVector[] Vectors =
    [
        new(
            "tcap/begin/dialogue-invoke",
            SigtranProtocolInteropSurface.Tcap,
            "TCAP Begin with originating transaction id, dialogue portion, and Invoke component.",
            new byte[]
            {
                0x62, 0x17,
                0x88, 0x01, 0x01,
                0xAB, 0x04, 0x06, 0x02, 0x2A, 0x03,
                0xAC, 0x0C,
                0xA1, 0x0A, 0x02, 0x01, 0x02, 0x02, 0x01, 0x01, 0x04, 0x02, 0xAA, 0xBB
            },
            Source),
        new(
            "tcap/end/return-result",
            SigtranProtocolInteropSurface.Tcap,
            "TCAP End with destination transaction id and ReturnResult component.",
            new byte[]
            {
                0x64, 0x10,
                0x89, 0x01, 0x01,
                0xAC, 0x0B,
                0xA2, 0x09, 0x02, 0x01, 0x02, 0x02, 0x01, 0x01, 0x04, 0x01, 0xCC
            },
            Source)
    ];

    /// <summary>Returns deterministic TCAP evidence vectors.</summary>
    /// <returns>The TCAP evidence vectors.</returns>
    public static IReadOnlyList<SigtranProtocolEvidenceVector> GetVectors()
    {
        return Vectors.ToArray();
    }

    /// <summary>Validates current TCAP encoders against deterministic evidence vectors.</summary>
    /// <returns>The byte-level validation reports.</returns>
    public static IReadOnlyList<SigtranProtocolEvidenceValidationReport> ValidateEncoders()
    {
        return Vectors.Select(vector => SigtranProtocolEvidenceValidator.Validate(vector, EncodeActualPayload(vector.Id))).ToArray();
    }

    private static byte[] EncodeActualPayload(string vectorId)
    {
        return vectorId switch
        {
            "tcap/begin/dialogue-invoke" => CreateBeginInvoke().Encode(),
            "tcap/end/return-result" => CreateEndReturnResult().Encode(),
            _ => throw new InvalidOperationException($"Unknown TCAP evidence vector '{vectorId}'.")
        };
    }

    private static TcapTransactionMessage CreateBeginInvoke()
    {
        TcapDialoguePortion dialogue = new(new TcapObjectIdentifier(1, 2, 3));
        TcapBerInvokeComponent invoke = new(2, TcapOperationCode.MoForwardShortMessage, new byte[] { 0xAA, 0xBB });
        return new(
            TcapPackageType.Begin,
            originatingTransactionId: new TcapTransactionId([0x01]),
            dialoguePortion: dialogue.Encode(),
            componentPortion: invoke.Encode());
    }

    private static TcapTransactionMessage CreateEndReturnResult()
    {
        TcapBerReturnResultComponent result = new(2, TcapOperationCode.MoForwardShortMessage, new byte[] { 0xCC });
        return new(
            TcapPackageType.End,
            destinationTransactionId: new TcapTransactionId([0x01]),
            componentPortion: result.Encode());
    }
}
