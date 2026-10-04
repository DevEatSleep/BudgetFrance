using BudgetFrance.Domain;
using static BudgetFrance.Tests.TestData;

namespace BudgetFrance.Tests;

public class ProposalGeneratorTests
{
    private static readonly HashSet<string> NoPriorities = [];

    // OTHER +10 % : déficit de 53 Md€ (5,3 %), effort restant ≈ 23,87 Md€ (stabilisation de la dette, plus exigeante que 3 %).
    private static readonly Scenario Overspending = new(new Dictionary<string, int> { ["OTHER"] = 10 });

    [Fact]
    public void Sustainable_budget_gets_no_proposal()
    {
        var current = new Scenario(new Dictionary<string, int> { ["VAT"] = 10 });

        Assert.Empty(ProposalGenerator.Generate(SampleBaseline(), current, NoPriorities));
    }

    [Fact]
    public void Each_proposal_closes_the_remaining_gap()
    {
        var proposals = ProposalGenerator.Generate(SampleBaseline(), Overspending, NoPriorities);

        Assert.Equal([ProposalKind.Taxes, ProposalKind.Savings, ProposalKind.Mixed], proposals.Select(p => p.Kind));
        Assert.All(proposals, p => Assert.True(p.Result.RemainingGap <= 0));
    }

    [Fact]
    public void Tax_proposal_raises_its_package_by_a_rounded_up_common_percentage()
    {
        var taxes = ProposalGenerator.Generate(SampleBaseline(), Overspending, NoPriorities).First(p => p.Kind == ProposalKind.Taxes);

        // ⌈23,87 × 100 / 400⌉ = 6 %.
        Assert.Equal(new Dictionary<string, int> { ["VAT"] = 6 }, taxes.Adjustments);
        Assert.Equal(10, taxes.Scenario.ChangeFor("OTHER"));
    }

    [Fact]
    public void Savings_spare_priority_and_locked_lines()
    {
        var baseline = SampleBaseline(priorities: [new("EDUC", "Éducation", ["EDU"], Note: null)]);

        var savings = ProposalGenerator.Generate(baseline, Overspending, new HashSet<string> { "EDUC" })
            .Single(p => p.Kind == ProposalKind.Savings);

        // ⌈23,87 × 100 / 230⌉ = 11 % sur OTHER seulement.
        Assert.Equal(new Dictionary<string, int> { ["OTHER"] = -11 }, savings.Adjustments);
    }

    [Fact]
    public void No_savings_when_every_adjustable_line_is_a_priority()
    {
        var proposals = ProposalGenerator.Generate(SampleBaseline(), Overspending, new HashSet<string> { "CORE" });

        Assert.All(proposals, p => Assert.Equal(ProposalKind.Taxes, p.Kind));
    }

    [Fact]
    public void Mixed_proposal_combines_taxes_and_savings()
    {
        var mixed = ProposalGenerator.Generate(SampleBaseline(), Overspending, NoPriorities).Single(p => p.Kind == ProposalKind.Mixed);

        Assert.True(mixed.Adjustments["VAT"] > 0);
        Assert.True(mixed.Adjustments["EDU"] < 0);
        Assert.True(mixed.Result.RemainingGap <= 0);
    }

    [Fact]
    public void Proposal_capped_at_the_maximum_change_is_reported_insufficient()
    {
        // Déficit de 135 Md€ avec la TVA déjà à +40 % : il faudrait 27 points, le panier n'en ajoute que 10.
        var current = new Scenario(new Dictionary<string, int> { ["VAT"] = 40, ["EDU"] = 50, ["OTHER"] = 50 });

        var taxes = ProposalGenerator.Generate(SampleBaseline(), current, NoPriorities).First(p => p.Kind == ProposalKind.Taxes);

        Assert.Equal(10, taxes.Adjustments["VAT"]);
        Assert.Equal(Verdict.NotFeasible, taxes.Result.Verdict);
    }
}
