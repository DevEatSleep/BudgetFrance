using BudgetFrance.Domain;
using static BudgetFrance.Tests.TestData;

namespace BudgetFrance.Tests;

public class FeasibilityEvaluatorTests
{
    [Fact]
    public void Unchanged_scenario_reproduces_baseline_totals()
    {
        var result = FeasibilityEvaluator.Evaluate(SampleBaseline(), Scenario.Unchanged);

        Assert.Equal(550m, result.TotalSpending);
        Assert.Equal(520m, result.TotalRevenue);
        Assert.Equal(-30m, result.Balance);
        Assert.Equal(3m, result.DeficitPctGdp);
        Assert.Equal(40m, result.TaxBurdenPctGdp);
    }

    [Fact]
    public void Changes_are_applied_as_percentages_of_baseline_amounts()
    {
        var scenario = new Scenario(new Dictionary<string, int> { ["EDU"] = 10, ["VAT"] = -5 });

        var result = FeasibilityEvaluator.Evaluate(SampleBaseline(), scenario);

        Assert.Equal(580m, result.TotalSpending);
        Assert.Equal(500m, result.TotalRevenue);
        Assert.Equal(33m, result.Spending.Single(p => p.Code == "EDU").PctGdp);
        Assert.Equal(380m, result.Revenues.Single(r => r.Code == "VAT").Amount);
    }

    [Fact]
    public void Changes_are_reported_in_billions_per_line_and_per_side()
    {
        var scenario = new Scenario(new Dictionary<string, int> { ["EDU"] = 10, ["VAT"] = -5 });

        var result = FeasibilityEvaluator.Evaluate(SampleBaseline(), scenario);

        Assert.Equal(30m, result.Spending.Single(p => p.Code == "EDU").Change);
        Assert.Equal(0m, result.Spending.Single(p => p.Code == "OTHER").Change);
        Assert.Equal(-20m, result.Revenues.Single(r => r.Code == "VAT").Change);
        Assert.Equal(30m, result.SpendingChange);
        Assert.Equal(-20m, result.RevenueChange);
    }

    [Fact]
    public void Priority_sums_the_amounts_and_changes_of_its_lines()
    {
        var scenario = new Scenario(new Dictionary<string, int> { ["EDU"] = 10, ["OTHER"] = -10 });

        var priority = FeasibilityEvaluator.Evaluate(SampleBaseline(), scenario).Priorities.Single();

        Assert.Equal("CORE", priority.Code);
        Assert.Equal(537m, priority.Amount);
        Assert.Equal(7m, priority.Change);
    }

    [Fact]
    public void Priority_referencing_an_unknown_line_is_rejected()
    {
        var baseline = SampleBaseline(priorities: [new("BAD", "Inconnu", ["NOPE"], Note: null)]);

        Assert.Throws<ArgumentException>(() => FeasibilityEvaluator.Evaluate(baseline, Scenario.Unchanged));
    }

    [Fact]
    public void Gap_to_eu_limit_is_the_amount_still_to_find_above_three_percent()
    {
        // Déficit de 53 Md€ (5,3 %) : il manque 23 Md€ pour revenir à 3 %.
        var scenario = new Scenario(new Dictionary<string, int> { ["OTHER"] = 10 });

        var result = FeasibilityEvaluator.Evaluate(SampleBaseline(), scenario);

        Assert.Equal(23m, result.GapToEuLimit);
        Assert.Equal(23m + (3m - 100m * 0.03m / 1.03m) * 10m, result.GapToDebtStabilisation);
    }

    [Fact]
    public void Gap_is_negative_when_the_target_is_met()
    {
        // TVA +10 % : déficit ramené à -10 Md€ (excédent), 40 Md€ de marge sous les 3 %.
        var result = FeasibilityEvaluator.Evaluate(SampleBaseline(), new Scenario(new Dictionary<string, int> { ["VAT"] = 10 }));

        Assert.Equal(-40m, result.GapToEuLimit);
        Assert.True(result.GapToDebtStabilisation < 0m);
    }

    [Fact]
    public void Changing_a_locked_line_is_rejected()
    {
        var scenario = new Scenario(new Dictionary<string, int> { ["DEBT"] = -10 });

        Assert.Throws<ArgumentException>(() => FeasibilityEvaluator.Evaluate(SampleBaseline(), scenario));
    }

    [Fact]
    public void Debt_stabilising_deficit_follows_debt_times_growth_over_one_plus_growth()
    {
        Assert.Equal(100m * 0.03m / 1.03m, FeasibilityEvaluator.DebtStabilisingDeficit(100m, 3m));
    }

    [Fact]
    public void Deficit_above_three_percent_is_not_feasible()
    {
        var scenario = new Scenario(new Dictionary<string, int> { ["OTHER"] = 1 });

        var result = FeasibilityEvaluator.Evaluate(SampleBaseline(), scenario);

        Assert.False(result.DeficitWithinEuLimit);
        Assert.Equal(Verdict.NotFeasible, result.Verdict);
    }

    [Fact]
    public void Deficit_under_three_percent_but_growing_debt_is_not_feasible()
    {
        // Dette 50 % et croissance 3 % : la dette n'est stable qu'en dessous d'environ 1,46 % de déficit.
        var result = FeasibilityEvaluator.Evaluate(SampleBaseline(debtPctGdp: 50m), Scenario.Unchanged);

        Assert.True(result.DeficitWithinEuLimit);
        Assert.False(result.DebtStabilised);
        Assert.Equal(Verdict.NotFeasible, result.Verdict);
    }

    [Fact]
    public void Balanced_scenario_within_eu_ranges_is_feasible()
    {
        var scenario = new Scenario(new Dictionary<string, int> { ["VAT"] = 10 });

        var result = FeasibilityEvaluator.Evaluate(SampleBaseline(), scenario);

        Assert.Equal(Verdict.Feasible, result.Verdict);
    }

    [Fact]
    public void Spending_outside_eu_range_makes_feasible_scenario_unprecedented()
    {
        var scenario = new Scenario(new Dictionary<string, int> { ["EDU"] = -20, ["VAT"] = -10 });

        var result = FeasibilityEvaluator.Evaluate(SampleBaseline(), scenario);

        Assert.False(result.Spending.Single(p => p.Code == "EDU").WithinEuRange);
        Assert.Equal(Verdict.FeasibleButUnprecedented, result.Verdict);
    }

    [Fact]
    public void Tax_burden_above_eu_maximum_makes_feasible_scenario_unprecedented()
    {
        var scenario = new Scenario(new Dictionary<string, int> { ["VAT"] = 15 });

        var result = FeasibilityEvaluator.Evaluate(SampleBaseline(), scenario);

        Assert.False(result.TaxBurdenWithinEuRange);
        Assert.Equal(Verdict.FeasibleButUnprecedented, result.Verdict);
    }

    [Fact]
    public void Locked_line_outside_eu_range_does_not_affect_verdict()
    {
        // DEBT = 2 % du PIB, au-dessus du maximum de 1 % de sa fourchette, mais verrouillé.
        var result = FeasibilityEvaluator.Evaluate(SampleBaseline(), new Scenario(new Dictionary<string, int> { ["VAT"] = 10 }));

        Assert.True(result.Spending.Single(p => p.Code == "DEBT").WithinEuRange);
        Assert.Equal(Verdict.Feasible, result.Verdict);
    }
}
