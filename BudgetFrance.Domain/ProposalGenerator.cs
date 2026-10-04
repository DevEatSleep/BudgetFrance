namespace BudgetFrance.Domain;

public enum ProposalKind
{
    /// <summary>Relèvement d'un panier fiscal.</summary>
    Taxes,

    /// <summary>Économies réparties sur les dépenses hors priorités.</summary>
    Savings,

    /// <summary>Moitié recettes (toutes), moitié économies hors priorités.</summary>
    Mixed
}

/// <summary>
/// Piste de financement : <see cref="Scenario"/> complet (scénario courant + <see cref="Adjustments"/>, en points de %)
/// et son évaluation. Une piste bornée à ±<see cref="Scenario.MaxChangePct"/> peut rester insuffisante.
/// </summary>
public sealed record Proposal(
    ProposalKind Kind,
    TaxPackage? Package,
    Scenario Scenario,
    IReadOnlyDictionary<string, int> Adjustments,
    FeasibilityResult Result);

/// <summary>
/// Propose des façons de combler l'effort restant (<see cref="FeasibilityResult.RemainingGap"/>) du scénario courant,
/// sans toucher aux dépenses des priorités choisies. Même calcul statique que <see cref="FeasibilityEvaluator"/>.
/// </summary>
public static class ProposalGenerator
{
    public static IReadOnlyList<Proposal> Generate(Baseline baseline, Scenario current, IReadOnlySet<string> priorities)
    {
        var gap = FeasibilityEvaluator.Evaluate(baseline, current).RemainingGap;
        if (gap <= 0)
            return [];

        var adjustableRevenues = baseline.Revenues.Where(l => !l.Locked).ToDictionary(l => l.Code, l => l.Amount);
        var protectedLines = baseline.Priorities
            .Where(p => priorities.Contains(p.Code))
            .SelectMany(p => p.LineCodes)
            .ToHashSet();
        var savingsLines = baseline.Spending
            .Where(l => !l.Locked && !protectedLines.Contains(l.Code))
            .ToDictionary(l => l.Code, l => l.Amount);

        var proposals = baseline.TaxPackages
            .Select(package => Build(baseline, current, ProposalKind.Taxes, package,
                Spread(PackageLines(package, adjustableRevenues), gap, sign: 1)))
            .ToList();

        if (savingsLines.Count > 0)
        {
            proposals.Add(Build(baseline, current, ProposalKind.Savings, null, Spread(savingsLines, gap, sign: -1)));
            proposals.Add(Build(baseline, current, ProposalKind.Mixed, null,
                Spread(adjustableRevenues, gap / 2, sign: 1)
                    .Concat(Spread(savingsLines, gap / 2, sign: -1))
                    .ToDictionary()));
        }

        return proposals;
    }

    /// <summary>
    /// Variation entière commune (en % des montants de référence) qui dégage au moins <paramref name="amount"/> Md€
    /// sur ces postes : δ = ⌈amount × 100 / Σ montants⌉.
    /// </summary>
    private static Dictionary<string, int> Spread(IReadOnlyDictionary<string, decimal> lines, decimal amount, int sign)
    {
        var delta = (int)Math.Ceiling(amount * 100m / lines.Values.Sum());
        return lines.Keys.ToDictionary(code => code, _ => sign * delta);
    }

    private static Proposal Build(
        Baseline baseline, Scenario current, ProposalKind kind, TaxPackage? package, IReadOnlyDictionary<string, int> deltas)
    {
        var changes = new Dictionary<string, int>(current.ChangesPct);
        foreach (var (code, delta) in deltas)
            changes[code] = Math.Clamp(current.ChangeFor(code) + delta, -Scenario.MaxChangePct, Scenario.MaxChangePct);

        // Ajustements réellement appliqués, après la borne.
        var adjustments = deltas.Keys
            .Select(code => (Code: code, Applied: changes[code] - current.ChangeFor(code)))
            .Where(a => a.Applied != 0)
            .ToDictionary(a => a.Code, a => a.Applied);

        var scenario = new Scenario(changes);
        return new Proposal(kind, package, scenario, adjustments, FeasibilityEvaluator.Evaluate(baseline, scenario));
    }

    private static Dictionary<string, decimal> PackageLines(TaxPackage package, IReadOnlyDictionary<string, decimal> adjustableRevenues) =>
        package.LineCodes.ToDictionary(
            code => code,
            code => adjustableRevenues.TryGetValue(code, out var amount)
                ? amount
                : throw new ArgumentException($"Le panier '{package.Code}' référence une recette inconnue ou verrouillée : '{code}'.", nameof(package)));
}
