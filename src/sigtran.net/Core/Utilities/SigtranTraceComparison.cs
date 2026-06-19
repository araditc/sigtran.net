namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes one trace comparison mismatch.
/// </summary>
public sealed class SigtranTraceComparisonMismatch
{
    /// <summary>Creates a trace comparison mismatch.</summary>
    /// <param name="index">The zero-based message index.</param>
    /// <param name="expected">The expected message.</param>
    /// <param name="actual">The actual message, if present.</param>
    public SigtranTraceComparisonMismatch(int index, string expected, string? actual)
    {
        Index = index < 0 ? throw new ArgumentOutOfRangeException(nameof(index), "Index cannot be negative.") : index;
        Expected = string.IsNullOrWhiteSpace(expected) ? throw new ArgumentException("Expected message is required.", nameof(expected)) : expected;
        Actual = string.IsNullOrWhiteSpace(actual) ? null : actual;
    }

    /// <summary>The zero-based message index.</summary>
    public int Index { get; }

    /// <summary>The expected message.</summary>
    public string Expected { get; }

    /// <summary>The actual message, if present.</summary>
    public string? Actual { get; }

    /// <summary>Formats a compact mismatch summary.</summary>
    /// <returns>The mismatch summary.</returns>
    public string Describe()
    {
        return $"index={Index} expected={Expected} actual={Actual ?? "<missing>"}";
    }
}

/// <summary>
/// Describes the result of comparing expected and actual trace messages.
/// </summary>
public sealed class SigtranTraceComparisonReport
{
    /// <summary>Creates a trace comparison report.</summary>
    /// <param name="expectedCount">The expected message count.</param>
    /// <param name="actualCount">The actual message count.</param>
    /// <param name="mismatches">The mismatches.</param>
    public SigtranTraceComparisonReport(int expectedCount, int actualCount, IReadOnlyList<SigtranTraceComparisonMismatch> mismatches)
    {
        ArgumentNullException.ThrowIfNull(mismatches);
        ExpectedCount = expectedCount < 0 ? throw new ArgumentOutOfRangeException(nameof(expectedCount), "Expected count cannot be negative.") : expectedCount;
        ActualCount = actualCount < 0 ? throw new ArgumentOutOfRangeException(nameof(actualCount), "Actual count cannot be negative.") : actualCount;
        Mismatches = mismatches.ToArray();
    }

    /// <summary>The expected message count.</summary>
    public int ExpectedCount { get; }

    /// <summary>The actual message count.</summary>
    public int ActualCount { get; }

    /// <summary>The mismatches.</summary>
    public IReadOnlyList<SigtranTraceComparisonMismatch> Mismatches { get; }

    /// <summary>Whether the trace matched exactly.</summary>
    public bool Passed => Mismatches.Count == 0 && ExpectedCount == ActualCount;

    /// <summary>Formats a compact comparison summary.</summary>
    /// <returns>The comparison summary.</returns>
    public string Describe()
    {
        return $"passed={Passed} expected={ExpectedCount} actual={ActualCount} mismatches={Mismatches.Count}";
    }
}

/// <summary>
/// Compares expected and actual SIGTRAN trace message names.
/// </summary>
public static class SigtranTraceComparison
{
    /// <summary>Compares expected and actual trace messages in order.</summary>
    /// <param name="expectedMessages">The expected message names.</param>
    /// <param name="actualMessages">The actual message names.</param>
    /// <returns>The trace comparison report.</returns>
    public static SigtranTraceComparisonReport Compare(IReadOnlyList<string> expectedMessages, IReadOnlyList<string> actualMessages)
    {
        ArgumentNullException.ThrowIfNull(expectedMessages);
        ArgumentNullException.ThrowIfNull(actualMessages);

        List<SigtranTraceComparisonMismatch> mismatches = [];
        int max = Math.Max(expectedMessages.Count, actualMessages.Count);
        for (int i = 0; i < max; i++)
        {
            string? expected = i < expectedMessages.Count ? expectedMessages[i] : null;
            string? actual = i < actualMessages.Count ? actualMessages[i] : null;

            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            {
                mismatches.Add(new SigtranTraceComparisonMismatch(i, expected ?? "<unexpected-extra>", actual));
            }
        }

        return new SigtranTraceComparisonReport(expectedMessages.Count, actualMessages.Count, mismatches);
    }
}
