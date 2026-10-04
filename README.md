# BudgetFrance

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

Règles d'encodage, identiques pour tous les candidats :

- une mesure = un montant annuel en Md€ en régime de croisière (positif : plus de dépense ou de recette), rattaché à un poste de `baseline.json` et appliqué à l'année de référence ;
- les mesures d'un même poste sont additionnées, puis converties en % entier du poste (arrondi au plus proche) ;
- un effet revendiqué sans poste (croissance, lutte contre la fraude…) a `lineCode: null` : il est affiché, mais pas retenu dans le calcul statique ;
- chaque mesure cite sa source ; privilégier un chiffrage tiers quand il existe ;
- candidats par ordre alphabétique, pas de score ni de classement.

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

## Données (Eurostat, sans clé d'API)

| Jeu | Usage |
| --- | --- |
| `gov_10a_exp` | Dépenses par fonction COFOG, France (Md€) et pays de l'UE (% du PIB) |
| `gov_10a_main` | Recettes totales, TVA, impôts sur la production et sur le revenu, cotisations, impôts en capital |
| `gov_10a_taxag` | Répartition impôt sur le revenu des ménages / impôt sur les sociétés ; taux de prélèvements obligatoires des pays de l'UE |
| `nama_10_gdp` | PIB en valeur, et croissance nominale |
| `gov_10dd_edpt1` | Dette publique (% du PIB) |

## Règles de faisabilité

- **Non soutenable** si le déficit dépasse 3 % du PIB ou si la dette augmente (déficit > dette × g / (1 + g), g = croissance nominale de l'année de référence).
- **Faisable, mais sans équivalent dans l'UE** si un poste de dépense non verrouillé ou le taux de prélèvements obligatoires sort de la fourchette min–max des États membres.
- **Faisable** sinon.

## Limites connues

- Calcul statique : pas d'effet de comportement, ni sur le PIB, ni sur la charge de la dette (verrouillée).
- Programmes : variation arrondie au % entier du poste, soit une granularité d'environ 2,5 Md€ sur la santé ; une petite mesure isolée peut s'arrondir à zéro.
- L'Irlande est exclue des comparaisons (PIB gonflé par les multinationales).
- La CSG est classée avec l'impôt sur le revenu (comptabilité nationale) ; Eurostat ne permet pas de la séparer de l'IR.
- La fonction « Retraites et vieillesse » (COFOG GF1002) inclut aussi la dépendance des personnes âgées.
- Priorités transversales approximées : « Climat » = environnement (GF05) + énergie (GF0403, qui inclut aussi les aides aux énergies fossiles) + transports (GF0405, route comprise) ; « Égalité femmes-hommes » = famille et enfance (GF1004). Aucune donnée publique ne ventile la dépense APU par sexe ou par impact climatique ; le budget vert et le document de politique transversale « égalité » ne couvrent que l'État.

## Déploiement

Branches : `develop` (intégration), `main` (production). `ci.yml` lance les tests sur chaque PR et sur `develop`.

Render n'a pas de SDK .NET pour les sites statiques, la construction se fait donc dans GitHub Actions : à chaque push sur `main`, `deploy.yml` teste, publie en Release et pousse `wwwroot` sur la branche `deploy`. Render Static Site (gratuit) publie cette branche :

- Branche : `deploy`, sans build command, publish directory `.`
- Rewrite rule : `/*` → `/index.html`

## Licences

- Code : [MIT](LICENSE).
- Données : `baseline.json` est dérivé d'Eurostat (CC BY 4.0, © Union européenne) ; `programmes.json` est publié sous [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/deed.fr), chaque mesure citant sa source.
