using BudgetFrance.Domain;
using static BudgetFrance.Tests.TestData;

namespace BudgetFrance.Tests;

public class QuickScenarioTests
{
    private static QuickAnswers Answers(Direction taxes, params (string Code, Direction Direction)[] priorities) =>
        new(priorities.ToDictionary(p => p.Code, p => p.Direction), taxes);

    private static Dictionary<string, int> Build(QuickAnswers answers, Baseline? baseline = null) =>
        new(QuickScenario.Build(baseline ?? SampleBaseline(), answers).ChangesPct);

    [Theory]
    [InlineData(Direction.More, 10)]
    [InlineData(Direction.Less, -10)]
    public void Priority_answer_changes_every_line_of_the_priority(Direction direction, int expected)
    {
        Assert.Equal(new Dictionary<string, int> { ["EDU"] = expected, ["OTHER"] = expected },
            Build(Answers(Direction.Same, ("CORE", direction))));
    }

    [Fact]
    public void Same_everywhere_leaves_the_budget_unchanged()
    {
        Assert.Empty(Build(Answers(Direction.Same, ("CORE", Direction.Same))));
    }

    [Theory]
    [InlineData(Direction.More, 5)]
    [InlineData(Direction.Less, -5)]
    public void Tax_answer_changes_every_unlocked_revenue(Direction direction, int expected)
    {
        Assert.Equal(new Dictionary<string, int> { ["VAT"] = expected }, Build(Answers(direction)));
    }

    [Fact]
    public void Priority_and_tax_answers_combine()
    {
        Assert.Equal(new Dictionary<string, int> { ["EDU"] = 10, ["OTHER"] = 10, ["VAT"] = -5 },
            Build(Answers(Direction.Less, ("CORE", Direction.More))));
    }

    [Fact]
    public void Answers_on_a_shared_line_add_up()
    {
        var baseline = SampleBaseline(priorities:
        [
            new("A", "A", ["EDU"], Note: null),
            new("B", "B", ["EDU", "OTHER"], Note: null),
        ]);

        Assert.Equal(new Dictionary<string, int> { ["EDU"] = 20, ["OTHER"] = 10 },
            Build(Answers(Direction.Same, ("A", Direction.More), ("B", Direction.More)), baseline));
        Assert.Equal(new Dictionary<string, int> { ["OTHER"] = -10 },
            Build(Answers(Direction.Same, ("A", Direction.More), ("B", Direction.Less)), baseline));
    }

    [Fact]
    public void Unknown_priority_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => Build(Answers(Direction.Same, ("NOPE", Direction.More))));
    }

    [Fact]
    public void Quick_scenario_is_evaluated_like_any_scenario()
    {
        var baseline = SampleBaseline();

        var result = FeasibilityEvaluator.Evaluate(baseline, QuickScenario.Build(baseline, Answers(Direction.More, ("CORE", Direction.More))));

        Assert.Equal(53m, result.SpendingChange);
        Assert.Equal(20m, result.RevenueChange);
    }

    [Fact]
    public void Steps_are_the_amounts_moved_by_one_answer()
    {
        var baseline = SampleBaseline();

        Assert.Equal(53m, QuickScenario.PriorityStep(baseline, baseline.Priorities.Single()));
        Assert.Equal(20m, QuickScenario.TaxStep(baseline));
    }
}
