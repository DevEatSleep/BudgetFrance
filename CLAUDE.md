# BudgetFrance

Simulateur citoyen de la dépense publique française : préférences par domaine (COFOG), ajustement des recettes, verdict de faisabilité (3 % de déficit, stabilisation de la dette, comparaison aux pays de l'UE). Voir `README.md` pour les commandes et le déploiement ; la méthode publique (sources Eurostat, règles, limites) est `BudgetFrance.Web/Pages/Methode.razor`.

## Architecture

Solution `BudgetFrance.slnx`, `net10.0`, 4 projets, **sans backend** (site statique) :

- **`BudgetFrance.Domain`** — zéro dépendance. `Baseline` (données de référence), `Scenario` (variations en % par code de poste), `FeasibilityEvaluator` + `FeasibilityResult` (tout le calcul et le verdict), `ProposalGenerator` (pistes de financement, évaluées par `FeasibilityEvaluator`), `Programme` + `ProgrammeEvaluator` (programmes 2027 : mesures en Md€ → `Scenario`), `QuickScenario` (mode rapide : plus / autant / moins par priorité et pour les impôts → `Scenario`).
- **`BudgetFrance.Importer`** — console : `EurostatClient` (JSON-stat) + `BaselineBuilder` (catalogue des postes, fourchettes UE) → `BudgetFrance.Web/wwwroot/data/baseline.json`, versionné.
- **`BudgetFrance.Web`** — Blazor WASM autonome : `Pages/Home.razor` (état du scénario, URL), `Components/LineSlider.razor`, `Components/VerdictPanel.razor`, `Components/PriorityPicker.razor`, `Components/ProposalList.razor`, `Components/ProgrammeList.razor`, `Components/QuickQuiz.razor`, `Services/ScenarioQuery.cs` (encodage `?s=CODE:pct,…&p=PRIO,…`), `Pages/Methode.razor` (méthode et limites publiques, chiffres lus dans `baseline.json`), `Pages/MentionsLegales.razor`, `Services/SiteLinks.cs` (dépôt, signalement d'erreur).
- **`BudgetFrance.Tests`** — xUnit, ne référence que le Domain.

## Conventions

- **Tout calcul vit dans `FeasibilityEvaluator`** : le client affiche les montants de `FeasibilityResult`, il ne recalcule rien.
- **Périmètre APU (S13) en COFOG**, pas le budget de l'État (LOLF) : la santé et les retraites relèvent de la Sécurité sociale.
- Les libellés, le découpage des postes et les catalogues des priorités et des paniers fiscaux sont définis une seule fois, dans `BaselineBuilder`, et transportés par le JSON. Une priorité transversale (climat, égalité) est une approximation en postes COFOG, signalée par sa `Note`.
- Exception : les programmes des candidats vivent dans `wwwroot/data/programmes.json`, édité à la main (contenu éditorial, pas Eurostat) et validé par `ProgrammeCatalogTests`. Neutralité : même règle d'encodage pour tous, chaque mesure sourcée, ordre alphabétique, aucun score. Ne jamais inventer de chiffrage.
- Les postes verrouillés (charge de la dette, autres recettes) ne peuvent pas être modifiés et ne pèsent pas sur le verdict « sans équivalent dans l'UE ».
- Application en français uniquement pour l'instant (culture `fr-FR` fixée dans `Program.cs`) ; les valeurs CSS calculées utilisent `CultureInfo.InvariantCulture`.
- Version centralisée dans `Directory.Build.props` ; `CHANGELOG.md` au format Keep a Changelog.

## Principes

SRP, DRY, KISS, YAGNI : relire les fichiers modifiés en fin de session et vérifier explicitement ces 4 principes.
