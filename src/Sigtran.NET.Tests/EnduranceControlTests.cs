using System.Text;
using System.Text.Json;

internal static partial class EnduranceTests
{
    internal static void ControlProtocol()
    {
        byte[] good = JsonSerializer.SerializeToUtf8Bytes(new { Sequence = 1, Id = 1, Action = "begin", Nonce = new string('a', 32) });
        Check(EnduranceCommand.Parse(good).Action == "begin", "Bounded sequenced control frame must decode");
        foreach (string invalid in new[] { "{}", "{\"Id\":1,\"Id\":2}",
            "{\"Sequence\":1,\"Id\":1,\"Action\":\"restart-kafka\",\"Nonce\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"}",
            "{\"Sequence\":28,\"Id\":1,\"Action\":\"begin\",\"Nonce\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"}",
            "{\"Sequence\":1,\"Id\":1,\"Action\":\"begin\",\"Nonce\":\"not-a-nonce\"}" })
        {
            try { EnduranceCommand.Parse(Encoding.UTF8.GetBytes(invalid)); }
            catch (Exception error) when (error is InvalidDataException or JsonException or InvalidOperationException) { continue; }
            throw new InvalidOperationException("Unsafe or malformed control frame accepted");
        }
    }
}
