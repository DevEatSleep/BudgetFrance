namespace BudgetFrance.Domain;

/// <summary>Programmes des candidats, édités à la main dans <c>programmes.json</c>.</summary>
public sealed record ProgrammeCatalog(DateOnly UpdatedOn, IReadOnlyList<Programme> Programmes);

/// <summary>Programme budgétaire d'un candidat, décomposé en mesures sourcées.</summary>
public sealed record Programme(
    string Code,
    string Candidate,
    string Party,
    DateOnly PublishedOn,
    IReadOnlyList<Measure> Measures);

/// <summary>
/// Mesure chiffrée d'un programme, en Md€ par an en régime de croisière : positif = plus de dépense ou plus de recette.
/// <see cref="LineCode"/> nul : effet revendiqué sans poste (croissance, lutte contre la fraude…), non retenu dans le calcul statique.
/// </summary>
public sealed record Measure(string Label, string? LineCode, decimal AmountBn, string SourceLabel, string SourceUrl);
