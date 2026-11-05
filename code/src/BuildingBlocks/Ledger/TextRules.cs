namespace StableRail.BuildingBlocks.Ledger;

public static class TextRules
{
    public static string Required(string value, string code, int max)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim() != value || value.Length > max)
            throw new LedgerRuleException(code, "A required value is missing or too long.");
        return value;
    }

    public static string Token(string value, string code)
    {
        var token = Required(value, code, 32);
        if (token.Any(character => !char.IsAsciiLetterOrDigit(character)))
            throw new LedgerRuleException(code, "Asset code must be letters and digits.");
        return token.ToUpperInvariant();
    }

    public static string Contract(string value)
    {
        var text = Required(value, "validation", 128);
        if (text.Any(character => !char.IsAsciiLetterOrDigit(character)))
            throw new LedgerRuleException("validation", "Contract and destination values must be letters and digits.");
        return text;
    }

    public static string Json(string value)
    {
        var text = Required(value, "validation", 8000);
        if (!text.StartsWith('{') || !text.EndsWith('}'))
            throw new LedgerRuleException("validation", "Evidence documents must be JSON objects.");
        return text;
    }

    public static void RejectSensitive(string details)
    {
        var lowered = details.ToLowerInvariant();
        foreach (var denied in new[] { "\"secret\"", "\"password\"", "\"private_key\"", "\"verifier\"", "\"api_key\"" })
        {
            if (lowered.Contains(denied, StringComparison.Ordinal))
                throw new LedgerRuleException("sensitive_data", "Audit details cannot carry secrets.");
        }
    }
}
