namespace BudgetFrance.Domain;

/// <summary>Programme traduit en <see cref="Scenario"/> et son évaluation.</summary>
public sealed record ProgrammeEvaluation(Programme Programme, Scenario Scenario, FeasibilityResult Result);

/// <summary>
/// Traduit un programme en scénario : les mesures d'un même poste sont additionnées, puis rapportées au montant
/// de référence du poste, arrondies au % entier le plus proche. Même calcul statique que <see cref="FeasibilityEvaluator"/>.
/// </summary>
public static class ProgrammeEvaluator
{
    public static ProgrammeEvaluation Evaluate(Baseline baseline, Programme programme)
    {
        var adjustable = baseline.Spending.Where(l => !l.Locked).Select(l => (l.Code, l.Amount))
            .Concat(baseline.Revenues.Where(l => !l.Locked).Select(l => (l.Code, l.Amount)))
            .ToDictionary(l => l.Code, l => l.Amount);

        var changes = programme.Measures
            .Where(m => m.LineCode is not null)
            .GroupBy(m => m.LineCode!)
            .ToDictionary(g => g.Key, g => ChangePct(programme, g.Key, g.Sum(m => m.AmountBn), adjustable));

        var scenario = new Scenario(changes);
        return new ProgrammeEvaluation(programme, scenario, FeasibilityEvaluator.Evaluate(baseline, scenario));
    }

    // Une erreur de saisie doit casser les tests, pas être bornée en silence.
    private static int ChangePct(Programme programme, string code, decimal amount, IReadOnlyDictionary<string, decimal> adjustable)
    {
        if (!adjustable.TryGetValue(code, out var reference))
            throw new ArgumentException($"Le programme '{programme.Code}' référence un poste inconnu ou verrouillé : '{code}'.", nameof(programme));

        var change = (int)Math.Round(amount * 100m / reference, MidpointRounding.AwayFromZero);
        if (Math.Abs(change) > Scenario.MaxChangePct)
            throw new ArgumentException($"Le programme '{programme.Code}' fait varier '{code}' de {change} %, au-delà de ±{Scenario.MaxChangePct} %.", nameof(programme));

        return change;
    }
}
