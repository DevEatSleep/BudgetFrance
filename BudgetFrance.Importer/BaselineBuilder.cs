using BudgetFrance.Domain;

namespace BudgetFrance.Importer;

/// <summary>
/// Construit la <see cref="Baseline"/> d'une année à partir d'Eurostat, pour les administrations publiques françaises (S13).
/// </summary>
internal sealed class BaselineBuilder(EurostatClient eurostat)
{
    /// <summary>
    /// Pays de comparaison : les 27 États membres, sauf l'Irlande dont le PIB, gonflé par les bénéfices
    /// des multinationales, rend les ratios en % du PIB incomparables.
    /// </summary>
    private static readonly string[] Peers =
    [
        "AT", "BE", "BG", "CY", "CZ", "DE", "DK", "EE", "EL", "ES", "FI", "FR", "HR", "HU",
        "IT", "LT", "LU", "LV", "MT", "NL", "PL", "PT", "RO", "SE", "SI", "SK"
    ];

    private const string EuAggregate = "EU27_2020";

    /// <summary>
    /// Postes de dépense : divisions COFOG, dont on isole des groupes réglables séparément ou utiles aux priorités :
    /// charge de la dette (GF0107, verrouillée), énergie (GF0403), transports (GF0405),
    /// retraites (GF1002, vieillesse) et famille (GF1004).
    /// </summary>
    private static readonly SpendingDefinition[] SpendingDefinitions =
    [
        new("SERVICES_GENERAUX", "Services publics généraux (hors dette)", "GF01", Excluded: ["GF0107"]),
        new("DETTE", "Charge de la dette", "GF0107", Locked: true),
        new("DEFENSE", "Défense", "GF02"),
        new("SECURITE", "Ordre et sécurité publics", "GF03"),
        new("ENERGIE", "Combustibles et énergie", "GF0403"),
        new("TRANSPORTS", "Transports", "GF0405"),
        new("ECONOMIE", "Autres affaires économiques (aides, agriculture, R&D…)", "GF04", Excluded: ["GF0403", "GF0405"]),
        new("ENVIRONNEMENT", "Protection de l'environnement", "GF05"),
        new("LOGEMENT", "Logement et équipements collectifs", "GF06"),
        new("SANTE", "Santé", "GF07"),
        new("CULTURE", "Loisirs, culture et culte", "GF08"),
        new("ENSEIGNEMENT", "Enseignement", "GF09"),
        new("RETRAITES", "Retraites et vieillesse", "GF1002"),
        new("FAMILLE", "Famille et enfance", "GF1004"),
        new("PROTECTION_SOCIALE", "Autre protection sociale (maladie, chômage, logement, exclusion…)", "GF10", Excluded: ["GF1002", "GF1004"]),
    ];

    /// <summary>
    /// Priorités proposées au citoyen, exprimées en postes COFOG. Le climat et l'égalité femmes-hommes
    /// sont transversaux : ce ne sont que des approximations, signalées par leur note.
    /// </summary>
    private static readonly Priority[] Priorities =
    [
        new("SANTE", "Santé", ["SANTE"], null),
        new("EDUCATION", "Éducation", ["ENSEIGNEMENT"], null),
        new("CLIMAT", "Climat et environnement", ["ENVIRONNEMENT", "ENERGIE", "TRANSPORTS"],
            "Approximation : inclut aussi les aides aux énergies fossiles et la route."),
        new("SECURITE", "Sécurité et défense", ["SECURITE", "DEFENSE"], null),
        new("RETRAITES", "Retraites", ["RETRAITES"], null),
        new("SOLIDARITE", "Solidarité", ["PROTECTION_SOCIALE", "LOGEMENT"], null),
        new("EGALITE", "Égalité femmes-hommes", ["FAMILLE"],
            "Approximation : garde d'enfants et congés parentaux. Aucune donnée publique ne ventile la dépense par sexe."),
        new("CULTURE", "Culture", ["CULTURE"], null),
    ];

    /// <summary>
    /// Paniers fiscaux des pistes de financement. L'effort réparti couvre toutes les recettes réglables :
    /// il est complété dans <see cref="BuildTaxPackages"/>.
    /// </summary>
    private static readonly TaxPackage[] TargetedTaxPackages =
    [
        new("CONSOMMATION", "Consommation (TVA)", ["TVA"]),
        new("MENAGES", "Revenus et patrimoine des ménages", ["IMPOTS_REVENU", "AUTRES_IMPOTS_COURANTS", "IMPOTS_CAPITAL"]),
        new("ENTREPRISES", "Entreprises", ["IMPOT_SOCIETES", "IMPOTS_PRODUCTION"]),
    ];

    public async Task<Baseline> BuildAsync(int year)
    {
        var time = ("time", year.ToString());
        var cofogCodes = SpendingDefinitions
            .SelectMany(d => d.Excluded.Prepend(d.Cofog))
            .Distinct()
            .Select(c => ("cofog99", c));
        var spendingFilters = new[] { ("sector", "S13"), ("na_item", "TE"), time }.Concat(cofogCodes).ToArray();

        var spendingFrance = await eurostat.GetAsync("gov_10a_exp", [("geo", "FR"), ("unit", "MIO_EUR"), .. spendingFilters]);
        var spendingPeers = await eurostat.GetAsync("gov_10a_exp", [("unit", "PC_GDP"), .. spendingFilters]);
        var accounts = await eurostat.GetAsync("gov_10a_main",
            ("geo", "FR"), ("sector", "S13"), ("unit", "MIO_EUR"), time,
            ("na_item", "TR"), ("na_item", "D2REC"), ("na_item", "D211REC"), ("na_item", "D5REC"),
            ("na_item", "D61REC"), ("na_item", "D91REC"));
        var incomeTaxes = await eurostat.GetAsync("gov_10a_taxag",
            ("geo", "FR"), ("sector", "S13"), ("unit", "MIO_EUR"), time, ("na_item", "D51A"), ("na_item", "D51B"));
        // Périmètre S13 + institutions de l'UE (droits de douane), comme le taux de prélèvements obligatoires publié.
        var taxBurdenPeers = await eurostat.GetAsync("gov_10a_taxag",
            ("sector", "S13_S212"), ("unit", "PC_GDP"), time, ("na_item", "D2_D5_D91_D61_M_D995"));
        var gdp = await eurostat.GetAsync("nama_10_gdp",
            ("geo", "FR"), ("unit", "CP_MEUR"), ("na_item", "B1GQ"), time, ("time", (year - 1).ToString()));
        var debt = await eurostat.GetAsync("gov_10dd_edpt1",
            ("geo", "FR"), ("sector", "S13"), ("unit", "PC_GDP"), ("na_item", "GD"), time);

        var gdpCurrent = Require(gdp.Find(time), $"PIB {year}");
        var revenues = BuildRevenues(accounts, incomeTaxes);
        var gdpPrevious = Require(gdp.Find(("time", (year - 1).ToString())), $"PIB {year - 1}");

        return new Baseline(
            year,
            Gdp: Billions(gdpCurrent),
            DebtPctGdp: Require(debt.Find(), $"dette {year}"),
            NominalGrowthPct: Math.Round((gdpCurrent / gdpPrevious - 1m) * 100m, 2),
            Spending: SpendingDefinitions.Select(d => BuildSpendingLine(d, spendingFrance, spendingPeers)).ToList(),
            Revenues: revenues,
            TaxBurdenPeers: BuildPeerRange(taxBurdenPeers, geo => taxBurdenPeers.Find(("geo", geo))),
            Priorities: Priorities,
            TaxPackages: BuildTaxPackages(revenues),
            ExtractedOn: DateOnly.FromDateTime(DateTime.Today));
    }

    private static SpendingLine BuildSpendingLine(SpendingDefinition definition, EurostatDataset france, EurostatDataset peers)
    {
        decimal? Value(EurostatDataset dataset, string? geo)
        {
            (string, string)[] Filter(string cofog) => geo is null ? [("cofog99", cofog)] : [("geo", geo), ("cofog99", cofog)];
            var included = dataset.Find(Filter(definition.Cofog));
            // Somme nullable : un groupe exclu manquant rend le poste inconnu plutôt que faux.
            return definition.Excluded.Aggregate(included, (total, excluded) => total - dataset.Find(Filter(excluded)));
        }

        return new SpendingLine(
            definition.Code,
            definition.Label,
            Billions(Require(Value(france, null), definition.Code)),
            definition.Locked,
            BuildPeerRange(peers, geo => Value(peers, geo)));
    }

    private static List<RevenueLine> BuildRevenues(EurostatDataset accounts, EurostatDataset incomeTaxes)
    {
        decimal Account(string code) => Require(accounts.Find(("na_item", code)), code);
        decimal IncomeTax(string code) => Require(incomeTaxes.Find(("na_item", code)), code);

        var vat = Account("D211REC");
        var productionTaxes = Account("D2REC") - vat;
        var householdIncomeTaxes = IncomeTax("D51A");
        var corporateTax = IncomeTax("D51B");
        var otherCurrentTaxes = Account("D5REC") - householdIncomeTaxes - corporateTax;
        var capitalTaxes = Account("D91REC");
        var socialContributions = Account("D61REC");
        var otherRevenue = Account("TR") - vat - productionTaxes - householdIncomeTaxes - corporateTax
            - otherCurrentTaxes - capitalTaxes - socialContributions;

        return
        [
            new("TVA", "TVA", Billions(vat), Locked: false, IsCompulsoryLevy: true),
            new("IMPOTS_PRODUCTION", "Autres impôts sur la production (accises, taxes foncières, sur les salaires…)", Billions(productionTaxes), false, true),
            new("IMPOTS_REVENU", "Impôts sur le revenu des ménages (IR, CSG, CRDS)", Billions(householdIncomeTaxes), false, true),
            new("IMPOT_SOCIETES", "Impôt sur les sociétés", Billions(corporateTax), false, true),
            new("AUTRES_IMPOTS_COURANTS", "Autres impôts courants (taxe d'habitation résiduelle, IFI…)", Billions(otherCurrentTaxes), false, true),
            new("IMPOTS_CAPITAL", "Droits de succession et de donation", Billions(capitalTaxes), false, true),
            new("COTISATIONS", "Cotisations sociales", Billions(socialContributions), false, true),
            new("AUTRES_RECETTES", "Autres recettes (ventes, revenus de la propriété, transferts)", Billions(otherRevenue), Locked: true, IsCompulsoryLevy: false),
        ];
    }

    private static List<TaxPackage> BuildTaxPackages(IEnumerable<RevenueLine> revenues) =>
    [
        new("REPARTI", "Effort fiscal réparti", revenues.Where(r => !r.Locked).Select(r => r.Code).ToList()),
        .. TargetedTaxPackages,
    ];

    private static PeerRange BuildPeerRange(EurostatDataset dataset, Func<string, decimal?> valueFor)
    {
        var values = Peers
            .Select(geo => (Country: dataset.GeoLabels[geo], Value: valueFor(geo)))
            .Where(p => p.Value is not null)
            .Select(p => (p.Country, Value: p.Value!.Value))
            .OrderBy(p => p.Value)
            .ToList();
        var (minCountry, min) = values.First();
        var (maxCountry, max) = values.Last();

        return new PeerRange(Require(valueFor(EuAggregate), EuAggregate), min, minCountry, max, maxCountry);
    }

    private static decimal Billions(decimal millions) => millions / 1000m;

    private static decimal Require(decimal? value, string what) =>
        value ?? throw new InvalidOperationException($"Donnée Eurostat manquante : {what}.");

    private sealed record SpendingDefinition(string Code, string Label, string Cofog, string[]? Excluded = null, bool Locked = false)
    {
        public string[] Excluded { get; } = Excluded ?? [];
    }
}
