using Sigtran.NET.Layers.MAP;
using Sigtran.NET.Layers.SCCP;
using Sigtran.NET.Layers.TCAP;

/// <summary>Checks that no supported decoder accepts a prefix of a complete wire message.</summary>
internal static class WireBoundaryTests
{
    private delegate bool Decoder(ReadOnlySpan<byte> bytes);

    internal static void RejectTruncatedVectors()
    {
        Decoder[] sccp =
        [
            static b => SccpUnitdataMessage.TryDecode(b, out _, out _),
            static b => SccpExtendedUnitdataMessage.TryDecode(b, out _, out _),
            static b => SccpLongUnitdataMessage.TryDecode(b, out _, out _),
            static b => SccpUnitdataServiceMessage.TryDecode(b, out _, out _)
        ];
        Decoder[] map =
        [
            static b => MapMoForwardShortMessage.TryDecode(b, out _, out _),
            static b => MapMtForwardShortMessage.TryDecode(b, out _, out _),
            static b => MapSendRoutingInfoForShortMessage.TryDecode(b, out _, out _),
            static b => MapReportShortMessageDeliveryStatus.TryDecode(b, out _, out _),
            static b => MapAlertServiceCentre.TryDecode(b, out _, out _)
        ];

        foreach (var (vector, decode) in SccpEvidenceVectors.GetVectors().Zip(sccp)
            .Concat(MapSmsEvidenceVectors.GetVectors().Zip(map)))
        {
            CheckPrefixes(vector.Id, vector.ExpectedPayload.Span, decode);
        }

        foreach (var vector in TcapEvidenceVectors.GetVectors())
        {
            CheckPrefixes(vector.Id, vector.ExpectedPayload.Span,
                static b => TcapTransactionMessage.TryDecode(b, out _, out _));
        }
    }

    private static void CheckPrefixes(string id, ReadOnlySpan<byte> payload, Decoder decode)
    {
        if (!decode(payload))
        {
            throw new InvalidOperationException($"Valid vector {id} was rejected.");
        }

        for (int length = 0; length < payload.Length; length++)
        {
            if (decode(payload[..length]))
            {
                throw new InvalidOperationException($"Decoder accepted truncated {id} at length {length}.");
            }
        }
    }
}
