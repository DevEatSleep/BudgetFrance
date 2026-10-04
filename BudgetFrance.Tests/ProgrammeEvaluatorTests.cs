using BudgetFrance.Domain;
using static BudgetFrance.Tests.TestData;

namespace BudgetFrance.Tests;

public class ProgrammeEvaluatorTests
{
    private static Programme SampleProgramme(params Measure[] measures) =>
        new("TEST", "Candidat", "Parti", new DateOnly(2027, 1, 15), measures);

    private static Measure On(string? lineCode, decimal amountBn) =>
        new("Mesure", lineCode, amountBn, "Source", "https://example.org");

    [Fact]
    public void Measures_are_converted_to_percentages_of_the_line_amount()
    {
        var evaluation = ProgrammeEvaluator.Evaluate(SampleBaseline(), SampleProgramme(On("EDU", 15m), On("VAT", -20m)));

        Assert.Equal(5, evaluation.Scenario.ChangeFor("EDU"));
        Assert.Equal(-5, evaluation.Scenario.ChangeFor("VAT"));
        Assert.Equal(565m, evaluation.Result.TotalSpending);
    }

    [Fact]
    public void Measures_on_the_same_line_are_summed()
    {
        var evaluation = ProgrammeEvaluator.Evaluate(SampleBaseline(), SampleProgramme(On("EDU", 9m), On("EDU", 6m)));

        Assert.Equal(5, evaluation.Scenario.ChangeFor("EDU"));
    }

    [Theory]
    [InlineData(4.4, 1)]  // 1,47 %
    [InlineData(4.5, 2)]  // 1,5 % : arrondi au plus loin de zéro
    [InlineData(-4.5, -2)]
    public void Changes_are_rounded_to_the_nearest_percent(decimal amountBn, int expectedPct)
    {
        var evaluation = ProgrammeEvaluator.Evaluate(SampleBaseline(), SampleProgramme(On("EDU", amountBn)));

        Assert.Equal(expectedPct, evaluation.Scenario.ChangeFor("EDU"));
    }

    [Fact]
    public void Measures_without_line_are_not_part_of_the_scenario()
    {
        var evaluation = ProgrammeEvaluator.Evaluate(SampleBaseline(), SampleProgramme(On(null, 30m)));

        Assert.Empty(evaluation.Scenario.ChangesPct);
        Assert.Equal(3m, evaluation.Result.DeficitPctGdp);
    }

    [Theory]
    [InlineData("UNKNOWN", 1)]
    [InlineData("DEBT", 1)]   // verrouillé
    [InlineData("EDU", 153)]  // 51 %
    public void Invalid_measures_are_rejected(string lineCode, decimal amountBn)
    {
        Assert.Throws<ArgumentException>(() => ProgrammeEvaluator.Evaluate(SampleBaseline(), SampleProgramme(On(lineCode, amountBn))));
    }

    [Theory]
    [InlineData(1, PriorityTrend.Strengthened)]
    [InlineData(0, PriorityTrend.Maintained)]
    [InlineData(-1, PriorityTrend.Reduced)]
    public void Priority_trend_follows_the_sign_of_the_change(decimal change, PriorityTrend expected)
    {
        Assert.Equal(expected, new PriorityOutcome("CORE", 100m, change).Trend);
    }
}
