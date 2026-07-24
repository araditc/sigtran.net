namespace Sigtran.NET.Layers.SCCP;

/// <summary>
/// Defines one longest-prefix global-title translation rule.
/// </summary>
public sealed class SccpGlobalTitleTranslationRule
{
    /// <summary>Creates a global-title translation rule.</summary>
    /// <param name="name">The stable rule name.</param>
    /// <param name="prefix">The numeric global-title prefix.</param>
    /// <param name="destinationPointCode">The translated destination point code.</param>
    /// <param name="subsystemNumber">The translated subsystem number.</param>
    /// <param name="preserveGlobalTitle">Whether the translated address retains the original global title.</param>
    public SccpGlobalTitleTranslationRule(
        string name,
        string prefix,
        ushort destinationPointCode,
        SubsystemNumber subsystemNumber,
        bool preserveGlobalTitle = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Translation rule name is required.", nameof(name));
        }

        string normalized = string.IsNullOrWhiteSpace(prefix)
            ? throw new ArgumentException("Global-title prefix is required.", nameof(prefix))
            : prefix.Trim().TrimStart('+');
        if (!normalized.All(char.IsDigit))
        {
            throw new ArgumentException("Global-title prefix must be numeric.", nameof(prefix));
        }

        if (destinationPointCode > 0x3FFF)
        {
            throw new ArgumentOutOfRangeException(nameof(destinationPointCode));
        }

        if (subsystemNumber == SubsystemNumber.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(subsystemNumber));
        }

        Name = name.Trim();
        Prefix = normalized;
        DestinationPointCode = destinationPointCode;
        SubsystemNumber = subsystemNumber;
        PreserveGlobalTitle = preserveGlobalTitle;
    }

    /// <summary>The stable rule name.</summary>
    public string Name { get; }

    /// <summary>The numeric global-title prefix.</summary>
    public string Prefix { get; }

    /// <summary>The translated destination point code.</summary>
    public ushort DestinationPointCode { get; }

    /// <summary>The translated subsystem number.</summary>
    public SubsystemNumber SubsystemNumber { get; }

    /// <summary>Whether the translated address retains the original global title.</summary>
    public bool PreserveGlobalTitle { get; }
}

/// <summary>
/// Resolves SCCP global titles through deterministic longest-prefix matching.
/// </summary>
public sealed class SccpGlobalTitleTranslationTable
{
    private readonly object _sync = new();
    private readonly List<SccpGlobalTitleTranslationRule> _rules = [];

    /// <summary>Adds a translation rule.</summary>
    /// <param name="rule">The translation rule.</param>
    public void Add(SccpGlobalTitleTranslationRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        lock (_sync)
        {
            if (_rules.Any(existing =>
                    string.Equals(existing.Name, rule.Name, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    $"SCCP translation rule '{rule.Name}' already exists.");
            }

            _rules.Add(rule);
        }
    }

    /// <summary>Removes a translation rule by name.</summary>
    /// <param name="name">The rule name.</param>
    /// <returns>True when a rule was removed.</returns>
    public bool Remove(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        lock (_sync)
        {
            int index = _rules.FindIndex(rule =>
                string.Equals(rule.Name, name, StringComparison.Ordinal));
            if (index < 0)
            {
                return false;
            }

            _rules.RemoveAt(index);
            return true;
        }
    }

    /// <summary>Returns a snapshot of configured translation rules.</summary>
    /// <returns>The translation rules.</returns>
    public IReadOnlyList<SccpGlobalTitleTranslationRule> Snapshot()
    {
        lock (_sync)
        {
            return _rules.ToArray();
        }
    }

    /// <summary>Translates a route-on-global-title address.</summary>
    /// <param name="address">The original called party address.</param>
    /// <param name="translated">The translated route-on-subsystem address.</param>
    /// <param name="rule">The selected longest-prefix rule.</param>
    /// <returns>True when a translation rule matched.</returns>
    public bool TryTranslate(
        SccpPartyAddress address,
        out SccpPartyAddress? translated,
        out SccpGlobalTitleTranslationRule? rule)
    {
        ArgumentNullException.ThrowIfNull(address);
        translated = null;
        rule = null;
        string? digits = address.GlobalTitle?.Digits;
        if (address.RoutingIndicator != SccpRoutingIndicator.RouteOnGlobalTitle
            || digits is null)
        {
            return false;
        }

        lock (_sync)
        {
            rule = _rules
                .Where(candidate =>
                    digits.StartsWith(candidate.Prefix, StringComparison.Ordinal))
                .OrderByDescending(candidate => candidate.Prefix.Length)
                .ThenBy(candidate => candidate.Name, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        if (rule is null)
        {
            return false;
        }

        translated = new(
            SccpRoutingIndicator.RouteOnSubsystemNumber,
            rule.SubsystemNumber,
            rule.DestinationPointCode,
            rule.PreserveGlobalTitle ? address.GlobalTitle : null);
        return true;
    }
}
