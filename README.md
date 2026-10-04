# BudgetFrance

Site : https://budgetfrance.onrender.com

Simulateur citoyen de la dépense publique française : l'utilisateur augmente ou réduit la dépense par domaine (éducation, santé, retraites…), ajuste les recettes, et l'application lui dit si son budget est **soutenable** et s'il a un **équivalent dans l'UE**.

Périmètre : toutes les administrations publiques (État, Sécurité sociale, collectivités — secteur S13), nomenclature COFOG. Pas de compte, pas de backend : le scénario et les priorités sont encodés dans l'URL (`?s=SANTE:10,TVA:-5&p=CLIMAT,SANTE`).

## Priorités

Le citoyen choisit jusqu'à 3 thèmes qui comptent pour lui (santé, climat, égalité femmes-hommes…). Les postes concernés passent en tête et le verdict indique si chaque priorité est renforcée, maintenue ou réduite. Ce bilan n'influe pas sur la faisabilité. Chaque thème regroupe des postes COFOG ; le catalogue est défini dans `BaselineBuilder`.

## Pistes de financement

Tant que le budget n'est pas soutenable, `ProposalGenerator` (Domain) propose des pistes qui comblent l'effort restant (le plus exigeant entre 3 % de déficit et stabilisation de la dette) :

- un relèvement par panier fiscal : effort réparti sur toutes les recettes réglables, consommation, ménages, entreprises (paniers définis dans `BaselineBuilder`) ;
- des économies réparties sur les dépenses **hors priorités** choisies ;
- un mixte moitié recettes, moitié économies.

Chaque piste applique un même pourcentage entier, arrondi au-dessus, aux postes concernés, borné à ±50 %. Elle est évaluée par `FeasibilityEvaluator` et s'applique en un clic. Une piste bornée peut rester insuffisante : son verdict l'indique. Calcul déterministe, pas de LLM : mêmes hypothèses statiques que le reste du simulateur.

## Programmes 2027

Les programmes des candidats sont encodés à la main dans `BudgetFrance.Web/wwwroot/data/programmes.json`, évalués par `ProgrammeEvaluator` (Domain) avec le même calcul que le scénario du citoyen. Chaque carte affiche le verdict du programme et, pour chaque priorité choisie, son sens (renforcée, maintenue, réduite) comparé à celui du citoyen. Un clic charge le programme dans les curseurs.

```json
{
  "updatedOn": "2027-01-20",
  "programmes": [
    {
      "code": "XXX", "candidate": "Nom", "party": "Parti", "publishedOn": "2027-01-15",
      "measures": [
        { "label": "Recrutement de soignants", "lineCode": "SANTE", "amountBn": 5, "sourceLabel": "Chiffrage …", "sourceUrl": "https://…" },
        { "label": "Lutte contre la fraude", "lineCode": null, "amountBn": 10, "sourceLabel": "Programme, p. 12", "sourceUrl": "https://…" }
      ]
    }
  ]
}
```

Règles d'encodage et charte de neutralité : voir la [méthode](https://budgetfrance.onrender.com/methode) (source : `BudgetFrance.Web/Pages/Methode.razor`).

`dotnet test` valide le fichier contre la baseline : poste inconnu ou verrouillé, variation au-delà de ±50 %, code dupliqué ou source invalide font échouer les tests.

## Structure

| Projet | Rôle |
| --- | --- |
| `BudgetFrance.Domain` | Modèle (`Baseline`, `Scenario`, `Programme`), règles de faisabilité (`FeasibilityEvaluator`), pistes (`ProposalGenerator`) et programmes (`ProgrammeEvaluator`). Aucune dépendance. |
| `BudgetFrance.Importer` | Console qui interroge l'API Eurostat et écrit `BudgetFrance.Web/wwwroot/data/baseline.json`. |
| `BudgetFrance.Web` | Client Blazor WebAssembly autonome (site statique). |
| `BudgetFrance.Tests` | Tests xUnit du Domain. |

## Commandes

```bash
dotnet run --project BudgetFrance.Web                    # lance le client en local
dotnet test                                              # tests du Domain
dotnet run --project BudgetFrance.Importer -- 2024       # régénère les données de référence (année COFOG la plus récente)
```

L'importeur se lance depuis la racine du repo. Le JSON produit est versionné : il ne change qu'une fois par an, quand Eurostat publie une nouvelle année COFOG.

## Données, règles de faisabilité et limites

Jeux Eurostat utilisés, règles du verdict et limites connues : voir la [méthode](https://budgetfrance.onrender.com/methode), page publique qui fait foi.

## Déploiement

Branches : `develop` (intégration), `main` (production). `ci.yml` lance les tests sur chaque PR et sur `develop`.

Render n'a pas de SDK .NET pour les sites statiques, la construction se fait donc dans GitHub Actions : à chaque push sur `main`, `deploy.yml` teste, publie en Release et pousse `wwwroot` sur la branche `deploy`. Render Static Site (gratuit) publie cette branche :

- Branche : `deploy`, sans build command, publish directory `.`
- Rewrite rule : `/*` → `/index.html`

## Licences

- Code : [MIT](LICENSE).
- Données : `baseline.json` est dérivé d'Eurostat (CC BY 4.0, © Union européenne) ; `programmes.json` est publié sous [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/deed.fr), chaque mesure citant sa source.
- Éditeur, hébergeur et données personnelles : [mentions légales](https://budgetfrance.onrender.com/mentions-legales).
