using System.Text.Json;
using BudgetFrance.Domain;

namespace BudgetFrance.Tests;

/// <summary>Valide le catalogue édité à la main contre la baseline publiée : une erreur de saisie casse les tests.</summary>
public class ProgrammeCatalogTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static T Load<T>(string file) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", file)), Web)!;

    [Fact]
    public void Every_published_programme_is_valid_against_the_baseline()
    {
        var baseline = Load<Baseline>("baseline.json");
        var catalog = Load<ProgrammeCatalog>("programmes.json");

        Assert.Equal(catalog.Programmes.Count, catalog.Programmes.Select(p => p.Code).Distinct().Count());
        foreach (var programme in catalog.Programmes)
        {
            Assert.NotEmpty(programme.Measures);
            Assert.All(programme.Measures, m => Assert.True(Uri.IsWellFormedUriString(m.SourceUrl, UriKind.Absolute), m.Label));
            ProgrammeEvaluator.Evaluate(baseline, programme);
        }
    }
}
