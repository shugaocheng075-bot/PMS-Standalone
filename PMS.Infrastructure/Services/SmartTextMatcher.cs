using System.Text.RegularExpressions;

namespace PMS.Infrastructure.Services;

internal static class SmartTextMatcher
{
    private static readonly char[] QuerySeparators =
    [
        ' ', '\t', '\r', '\n', ',', '，', ';', '；', '|', '/', '\\'
    ];

    private static readonly Dictionary<string, string[]> AliasMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["手麻"] = ["手术麻醉"],
        ["麻醉"] = ["手术麻醉"],
        ["重症"] = ["重症监护"],
        ["icu"] = ["重症监护"],
        ["emr"] = ["电子病历"],
        ["病案"] = ["病案归档"],
        ["随访系统"] = ["随访"],
        ["ai质控"] = ["ai内涵质控"],
        ["内涵质控"] = ["ai内涵质控"],
        ["cdas"] = ["病案归档"]
    };

    public static bool Match(string? source, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        var normalizedSource = Normalize(source);
        if (string.IsNullOrWhiteSpace(normalizedSource))
        {
            return false;
        }

        var tokens = SplitTokens(query);
        if (tokens.Count == 0)
        {
            return normalizedSource.Contains(Normalize(query), StringComparison.OrdinalIgnoreCase);
        }

        foreach (var token in tokens)
        {
            var candidates = ExpandToken(token);
            var matched = candidates.Any(candidate =>
                !string.IsNullOrWhiteSpace(candidate)
                && normalizedSource.Contains(candidate, StringComparison.OrdinalIgnoreCase));

            if (!matched)
            {
                return false;
            }
        }

        return true;
    }

    private static List<string> SplitTokens(string query)
    {
        return query
            .Split(QuerySeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Normalize)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> ExpandToken(string token)
    {
        yield return token;

        if (!AliasMap.TryGetValue(token, out var aliases))
        {
            yield break;
        }

        foreach (var alias in aliases.Select(Normalize).Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            yield return alias;
        }
    }
    /// <summary>
    /// Exact match (case-insensitive) for enum/status fields.
    /// </summary>
    public static bool MatchExact(string? fieldValue, string queryValue)
    {
        if (string.IsNullOrWhiteSpace(fieldValue))
            return false;

        return fieldValue.Equals(queryValue, StringComparison.OrdinalIgnoreCase);
    }
    private static string Normalize(string value)
    {
        var text = value.Trim().ToLowerInvariant();
        text = Regex.Replace(text, @"[\s\-_/\\|,，;；:：()（）\[\]【】'\""""`·]", string.Empty);
        return text;
    }
}
