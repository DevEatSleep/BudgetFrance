using BudgetFrance.Domain;

namespace BudgetFrance.Tests;

/// <summary>
/// Référence de test. PIB de 1 000 : 1 Md€ = 0,1 % du PIB, ce qui rend les attendus lisibles.
/// Déficit de référence : 30 Md€ (3 %) ; dette stable sous 100 × 3 / 103 ≈ 2,91 %.
/// </summary>
internal static class TestData
{
    private static readonly PeerRange WideRange = new(10m, 0m, "A", 100m, "B");

    private static readonly Priority[] SamplePriorities = [new("CORE", "Essentiel", ["EDU", "OTHER"], Note: null)];

    private static readonly TaxPackage[] SampleTaxPackages = [new("CONSUMPTION", "Consommation", ["VAT"])];

    public static Baseline SampleBaseline(
        decimal debtPctGdp = 100m,
        decimal nominalGrowthPct = 3m,
        IReadOnlyList<Priority>? priorities = null,
        IReadOnlyList<TaxPackage>? taxPackages = null) => new(
        Year: 2024,
        Gdp: 1000m,
        DebtPctGdp: debtPctGdp,
        NominalGrowthPct: nominalGrowthPct,
        Spending:
        [
            new SpendingLine("EDU", "Enseignement", 300m, Locked: false, new PeerRange(30m, 25m, "A", 35m, "B")),
            new SpendingLine("DEBT", "Charge de la dette", 20m, Locked: true, new PeerRange(1m, 0m, "A", 1m, "B")),
            new SpendingLine("OTHER", "Autres", 230m, Locked: false, WideRange),
        ],
        Revenues:
        [
            new RevenueLine("VAT", "TVA", 400m, Locked: false, IsCompulsoryLevy: true),
            new RevenueLine("MISC", "Autres recettes", 120m, Locked: true, IsCompulsoryLevy: false),
        ],
        TaxBurdenPeers: new PeerRange(40m, 30m, "A", 45m, "B"),
        Priorities: priorities ?? SamplePriorities,
        TaxPackages: taxPackages ?? SampleTaxPackages,
        ExtractedOn: new DateOnly(2026, 10, 3));
}
