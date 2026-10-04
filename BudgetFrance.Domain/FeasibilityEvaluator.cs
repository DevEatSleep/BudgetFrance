namespace BudgetFrance.Domain;

/// <summary>
/// Applique un <see cref="Scenario"/> à une <see cref="Baseline"/> et juge sa faisabilité.
/// Calcul statique : ni effet de comportement, ni effet sur le PIB ou sur la charge de la dette.
/// </summary>
public static class FeasibilityEvaluator
{
    /// <summary>Plafond de déficit public du Pacte de stabilité (art. 126 TFUE, protocole n° 12).</summary>
    public const decimal EuDeficitLimitPctGdp = 3m;

    public static FeasibilityResult Evaluate(Baseline baseline, Scenario scenario)
    {
        var spending = baseline.Spending
            .Select(line => ToSpendingPosition(line, Apply(line.Code, line.Amount, line.Locked, scenario), baseline.Gdp))
            .ToList();
        var revenues = baseline.Revenues
            .Select(line => ToRevenuePosition(line, Apply(line.Code, line.Amount, line.Locked, scenario)))
            .ToList();
        var compulsoryLevies = baseline.Revenues.Where(line => line.IsCompulsoryLevy).Select(line => line.Code).ToHashSet();

        var totalSpending = spending.Sum(p => p.Amount);
        var totalRevenue = revenues.Sum(r => r.Amount);
        var deficitPctGdp = PctGdp(totalSpending - totalRevenue, baseline.Gdp);
        var taxBurdenPctGdp = PctGdp(revenues.Where(r => compulsoryLevies.Contains(r.Code)).Sum(r => r.Amount), baseline.Gdp);
        var debtStabilisingDeficitPctGdp = DebtStabilisingDeficit(baseline.DebtPctGdp, baseline.NominalGrowthPct);

        return new FeasibilityResult(
            baseline.Gdp,
            totalSpending,
            totalRevenue,
            deficitPctGdp,
            debtStabilisingDeficitPctGdp,
            taxBurdenPctGdp,
            TaxBurdenWithinEuRange: baseline.TaxBurdenPeers.Contains(taxBurdenPctGdp),
            spending,
            revenues,
            baseline.Priorities.Select(priority => ToPriorityOutcome(priority, spending)).ToList());
    }

    /// <summary>
    /// Déficit (en % du PIB) qui maintient le ratio dette/PIB constant : d* = D × g / (1 + g),
    /// D = dette en % du PIB, g = croissance nominale du PIB.
    /// </summary>
    public static decimal DebtStabilisingDeficit(decimal debtPctGdp, decimal nominalGrowthPct)
    {
        var growth = nominalGrowthPct / 100m;
        return debtPctGdp * growth / (1m + growth);
    }

    private static decimal Apply(string code, decimal amount, bool locked, Scenario scenario)
    {
        var change = scenario.ChangeFor(code);
        if (locked && change != 0)
            throw new ArgumentException($"Le poste '{code}' est verrouillé et ne peut pas être modifié.", nameof(scenario));
        return amount * (100m + change) / 100m;
    }

    private static SpendingPosition ToSpendingPosition(SpendingLine line, decimal amount, decimal gdp)
    {
        var pctGdp = PctGdp(amount, gdp);
        // Un poste verrouillé échappe au choix de l'utilisateur : il ne doit pas peser sur le verdict.
        return new SpendingPosition(line.Code, amount, amount - line.Amount, pctGdp, line.Locked || line.Peers.Contains(pctGdp));
    }

    private static RevenuePosition ToRevenuePosition(RevenueLine line, decimal amount) =>
        new(line.Code, amount, amount - line.Amount);

    private static PriorityOutcome ToPriorityOutcome(Priority priority, IReadOnlyList<SpendingPosition> spending)
    {
        var positions = priority.LineCodes
            .Select(code => spending.SingleOrDefault(p => p.Code == code)
                ?? throw new ArgumentException($"La priorité '{priority.Code}' référence un poste inconnu : '{code}'.", nameof(priority)))
            .ToList();
        return new PriorityOutcome(priority.Code, positions.Sum(p => p.Amount), positions.Sum(p => p.Change));
    }

    private static decimal PctGdp(decimal amount, decimal gdp) => amount / gdp * 100m;
}
