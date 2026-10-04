namespace BudgetFrance.Domain;

/// <summary>
/// Comptes des administrations publiques (APU, secteur S13) pour une année de référence.
/// Montants en milliards d'euros, ratios en % du PIB. Produit par BudgetFrance.Importer depuis Eurostat.
/// </summary>
public sealed record Baseline(
    int Year,
    decimal Gdp,
    decimal DebtPctGdp,
    decimal NominalGrowthPct,
    IReadOnlyList<SpendingLine> Spending,
    IReadOnlyList<RevenueLine> Revenues,
    PeerRange TaxBurdenPeers,
    IReadOnlyList<Priority> Priorities,
    IReadOnlyList<TaxPackage> TaxPackages,
    DateOnly ExtractedOn);

/// <summary>Poste de dépense par fonction (nomenclature COFOG).</summary>
public sealed record SpendingLine(string Code, string Label, decimal Amount, bool Locked, PeerRange Peers);

/// <summary>Poste de recette. <see cref="IsCompulsoryLevy"/> : compte dans le taux de prélèvements obligatoires.</summary>
public sealed record RevenueLine(string Code, string Label, decimal Amount, bool Locked, bool IsCompulsoryLevy);

/// <summary>
/// Thème proposé au citoyen (santé, climat…), regroupant des postes de dépense.
/// <see cref="Note"/> signale une approximation : un thème transversal n'a pas de fonction COFOG propre.
/// </summary>
public sealed record Priority(string Code, string Label, IReadOnlyList<string> LineCodes, string? Note);

/// <summary>Groupe de recettes relevées ensemble par une piste de financement.</summary>
public sealed record TaxPackage(string Code, string Label, IReadOnlyList<string> LineCodes);

/// <summary>Fourchette observée dans les pays de l'UE, en % du PIB.</summary>
public sealed record PeerRange(
    decimal EuAveragePctGdp,
    decimal MinPctGdp,
    string MinCountry,
    decimal MaxPctGdp,
    string MaxCountry)
{
    public bool Contains(decimal pctGdp) => pctGdp >= MinPctGdp && pctGdp <= MaxPctGdp;
}
