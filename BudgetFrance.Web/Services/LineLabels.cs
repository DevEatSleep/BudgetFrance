using BudgetFrance.Domain;

namespace BudgetFrance.Web.Services;

/// <summary>Libellés des postes de dépense et de recette, partagés par les pistes et les programmes.</summary>
public static class LineLabels
{
    /// <summary>Libellé sans sa précision entre parenthèses : « TVA », « Défense »…</summary>
    public static string Short(Baseline baseline, string code)
    {
        var label = baseline.Spending.Select(l => (l.Code, l.Label))
            .Concat(baseline.Revenues.Select(l => (l.Code, l.Label)))
            .Single(l => l.Code == code).Label;
        return label.Split(" (")[0];
    }
}
