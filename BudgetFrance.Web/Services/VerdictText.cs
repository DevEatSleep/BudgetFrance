using BudgetFrance.Domain;

namespace BudgetFrance.Web.Services;

/// <summary>Libellés et classes CSS d'un <see cref="Verdict"/> et d'une <see cref="PriorityTrend"/>, partagés par le panneau, les pistes et les programmes.</summary>
public static class VerdictText
{
    public static string Title(Verdict verdict) => verdict switch
    {
        Verdict.Feasible => "Budget faisable",
        Verdict.FeasibleButUnprecedented => "Faisable, mais sans équivalent dans l'UE",
        _ => "Budget non soutenable"
    };

    public static string CssClass(Verdict verdict) => "verdict-" + verdict.ToString().ToLowerInvariant();

    public static string Trend(PriorityTrend trend) => trend switch
    {
        PriorityTrend.Strengthened => "renforcée",
        PriorityTrend.Reduced => "réduite",
        _ => "maintenue"
    };
}
