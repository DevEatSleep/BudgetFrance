namespace BudgetFrance.Domain;

/// <summary>
/// Choix de l'utilisateur : variation en % par code de poste (dépense ou recette), par rapport à l'année de référence.
/// Un poste absent est inchangé.
/// </summary>
public sealed record Scenario(IReadOnlyDictionary<string, int> ChangesPct)
{
    /// <summary>Amplitude maximale d'une variation, en %.</summary>
    public const int MaxChangePct = 50;

    public static Scenario Unchanged { get; } = new(new Dictionary<string, int>());

    public int ChangeFor(string code) => ChangesPct.GetValueOrDefault(code);
}
