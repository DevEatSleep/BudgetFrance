using BudgetFrance.Domain;

namespace BudgetFrance.Web.Services;

/// <summary>
/// Encode le scénario et les priorités dans l'URL (<c>?s=SANTE:10,TVA:-5&amp;p=CLIMAT,SANTE</c>)
/// pour qu'ils soient partageables sans compte.
/// </summary>
public static class ScenarioQuery
{
    public const string Parameter = "s";
    public const string PrioritiesParameter = "p";

    /// <summary>Nombre maximal de priorités : on choisit, on ne coche pas tout.</summary>
    public const int MaxPriorities = 3;

    public static string Format(IReadOnlyDictionary<string, int> changes) =>
        string.Join(",", changes.Where(c => c.Value != 0).OrderBy(c => c.Key).Select(c => $"{c.Key}:{c.Value}"));

    /// <summary>
    /// Lit un scénario venant de l'URL : les codes inconnus ou verrouillés sont ignorés, les valeurs bornées.
    /// </summary>
    public static Dictionary<string, int> Parse(string? value, Baseline baseline)
    {
        var adjustable = baseline.Spending.Where(l => !l.Locked).Select(l => l.Code)
            .Concat(baseline.Revenues.Where(l => !l.Locked).Select(l => l.Code))
            .ToHashSet();
        var changes = new Dictionary<string, int>();

        foreach (var pair in (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split(':');
            if (parts.Length == 2 && adjustable.Contains(parts[0]) && int.TryParse(parts[1], out var change))
                changes[parts[0]] = Math.Clamp(change, -Scenario.MaxChangePct, Scenario.MaxChangePct);
        }

        return changes;
    }

    public static string FormatPriorities(IEnumerable<string> priorities) => string.Join(",", priorities.Order());

    /// <summary>Lit les priorités venant de l'URL : codes inconnus ignorés, au plus <see cref="MaxPriorities"/>.</summary>
    public static HashSet<string> ParsePriorities(string? value, Baseline baseline)
    {
        var known = baseline.Priorities.Select(p => p.Code).ToHashSet();
        return (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Where(known.Contains)
            .Distinct()
            .Take(MaxPriorities)
            .ToHashSet();
    }
}
