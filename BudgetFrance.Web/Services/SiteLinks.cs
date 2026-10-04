namespace BudgetFrance.Web.Services;

/// <summary>Liens externes du site : dépôt, signalement d'erreur, contact (issues GitHub, pas d'adresse e-mail publique).</summary>
public static class SiteLinks
{
    public const string Repository = "https://github.com/DevEatSleep/BudgetFrance";
    public const string NewIssue = Repository + "/issues/new/choose";
    public const string Contact = NewIssue;

    /// <summary>Issue pré-remplie du modèle « Erreur d'encodage » (<c>.github/ISSUE_TEMPLATE/erreur-encodage.yml</c>).</summary>
    public static string EncodingIssue(string candidate) =>
        $"{Repository}/issues/new?template=erreur-encodage.yml" +
        $"&title={Uri.EscapeDataString($"Erreur d'encodage : {candidate}")}" +
        $"&candidat={Uri.EscapeDataString(candidate)}";
}
