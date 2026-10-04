namespace BudgetFrance.Domain;

/// <summary>Réponse du mode rapide ; la valeur est le signe de la variation.</summary>
public enum Direction
{
    Less = -1,
    Same = 0,
    More = 1
}

/// <summary>Réponses du mode rapide : une direction par priorité choisie (code → direction), une pour les impôts.</summary>
public sealed record QuickAnswers(IReadOnlyDictionary<string, Direction> Priorities, Direction Taxes);

/// <summary>
/// Traduit les réponses du mode rapide en <see cref="Scenario"/> : un même pourcentage pour tous les postes d'une priorité,
/// un autre pour toutes les recettes réglables. Les réponses de deux priorités qui partagent un poste s'additionnent.
/// </summary>
public static class QuickScenario
{
    /// <summary>Variation de chaque poste d'une priorité, en %.</summary>
    public const int PriorityChangePct = 10;

    /// <summary>Variation de chaque recette réglable (impôts et cotisations), en %.</summary>
    public const int TaxChangePct = 5;

    public static Scenario Build(Baseline baseline, QuickAnswers answers)
    {
        var changes = new Dictionary<string, int>();
        foreach (var (code, direction) in answers.Priorities)
        {
            var priority = baseline.Priorities.SingleOrDefault(p => p.Code == code)
                ?? throw new ArgumentException($"Les réponses référencent une priorité inconnue : '{code}'.", nameof(answers));
            foreach (var line in priority.LineCodes)
                changes[line] = changes.GetValueOrDefault(line) + (int)direction * PriorityChangePct;
        }

        foreach (var line in AdjustableRevenues(baseline))
            changes[line.Code] = (int)answers.Taxes * TaxChangePct;

        return new Scenario(changes.Where(c => c.Value != 0).ToDictionary());
    }

    /// <summary>Montant en Md€ de ±<see cref="PriorityChangePct"/> % sur les postes de la priorité.</summary>
    public static decimal PriorityStep(Baseline baseline, Priority priority) =>
        priority.LineCodes.Sum(code => (baseline.Spending.SingleOrDefault(l => l.Code == code)
            ?? throw new ArgumentException($"La priorité '{priority.Code}' référence un poste inconnu : '{code}'.", nameof(priority))).Amount)
        * PriorityChangePct / 100m;

    /// <summary>Montant en Md€ de ±<see cref="TaxChangePct"/> % sur les recettes réglables.</summary>
    public static decimal TaxStep(Baseline baseline) => AdjustableRevenues(baseline).Sum(l => l.Amount) * TaxChangePct / 100m;

    // Les mêmes recettes que l'effort fiscal réparti, par construction.
    private static IEnumerable<RevenueLine> AdjustableRevenues(Baseline baseline) => baseline.Revenues.Where(l => !l.Locked);
}
