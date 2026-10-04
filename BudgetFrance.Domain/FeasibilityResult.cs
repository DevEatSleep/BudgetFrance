namespace BudgetFrance.Domain;

public enum Verdict
{
    /// <summary>Respecte les 3 % et stabilise la dette, sans sortir des fourchettes observées dans l'UE.</summary>
    Feasible,

    /// <summary>Respecte les règles budgétaires, mais au moins un niveau n'a d'équivalent dans aucun pays de l'UE.</summary>
    FeasibleButUnprecedented,

    /// <summary>Dépasse les 3 % de déficit ou fait croître la dette.</summary>
    NotFeasible
}

public sealed record FeasibilityResult(
    decimal Gdp,
    decimal TotalSpending,
    decimal TotalRevenue,
    decimal DeficitPctGdp,
    decimal DebtStabilisingDeficitPctGdp,
    decimal TaxBurdenPctGdp,
    bool TaxBurdenWithinEuRange,
    IReadOnlyList<SpendingPosition> Spending,
    IReadOnlyList<RevenuePosition> Revenues,
    IReadOnlyList<PriorityOutcome> Priorities)
{
    public decimal Balance => TotalRevenue - TotalSpending;

    /// <summary>Variation des dépenses par rapport à l'année de référence, en Md€.</summary>
    public decimal SpendingChange => Spending.Sum(p => p.Change);

    /// <summary>Variation des recettes par rapport à l'année de référence, en Md€.</summary>
    public decimal RevenueChange => Revenues.Sum(r => r.Change);

    public bool DeficitWithinEuLimit => DeficitPctGdp <= FeasibilityEvaluator.EuDeficitLimitPctGdp;

    public bool DebtStabilised => DeficitPctGdp <= DebtStabilisingDeficitPctGdp;

    /// <summary>Économies ou recettes restant à trouver pour respecter les 3 %, en Md€ (négatif : marge disponible).</summary>
    public decimal GapToEuLimit => GapTo(FeasibilityEvaluator.EuDeficitLimitPctGdp);

    /// <summary>Économies ou recettes restant à trouver pour stabiliser la dette, en Md€ (négatif : marge disponible).</summary>
    public decimal GapToDebtStabilisation => GapTo(DebtStabilisingDeficitPctGdp);

    /// <summary>Effort restant pour respecter à la fois les 3 % et la stabilisation de la dette, en Md€.</summary>
    public decimal RemainingGap => Math.Max(GapToEuLimit, GapToDebtStabilisation);

    public Verdict Verdict =>
        !DeficitWithinEuLimit || !DebtStabilised ? Verdict.NotFeasible
        : !TaxBurdenWithinEuRange || Spending.Any(p => !p.WithinEuRange) ? Verdict.FeasibleButUnprecedented
        : Verdict.Feasible;

    private decimal GapTo(decimal deficitTargetPctGdp) => (DeficitPctGdp - deficitTargetPctGdp) * Gdp / 100m;
}

/// <summary>Niveau d'un poste de dépense après application du scénario ; <see cref="Change"/> en Md€.</summary>
public sealed record SpendingPosition(string Code, decimal Amount, decimal Change, decimal PctGdp, bool WithinEuRange);

/// <summary>Dépense consacrée à une priorité après application du scénario ; <see cref="Change"/> en Md€.</summary>
public sealed record PriorityOutcome(string Code, decimal Amount, decimal Change)
{
    public PriorityTrend Trend => Change > 0 ? PriorityTrend.Strengthened : Change < 0 ? PriorityTrend.Reduced : PriorityTrend.Maintained;
}

public enum PriorityTrend
{
    Reduced,
    Maintained,
    Strengthened
}

/// <summary>Montant d'un poste de recette après application du scénario ; <see cref="Change"/> en Md€.</summary>
public sealed record RevenuePosition(string Code, decimal Amount, decimal Change);
