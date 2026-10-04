---
name: encode-programme
description: Encode ou met à jour le programme budgétaire d'un candidat à la présidentielle 2027 dans BudgetFrance.Web/wwwroot/data/programmes.json, à partir d'une source fournie par l'utilisateur. Utiliser quand l'utilisateur demande d'intégrer, d'encoder, d'ajouter ou de mettre à jour un programme ou un chiffrage de candidat.
---

# Encoder un programme 2027

La neutralité de l'application repose sur une règle unique, appliquée de la même façon à tous les candidats. Ne t'en écarte pas, même pour un cas qui semble évident.

La version publique de ces règles est `BudgetFrance.Web/Pages/Methode.razor` (page `/methode`) : tout changement de règle se reporte aux deux endroits.

## Préalables

- **Source obligatoire, fournie par l'utilisateur** (lien ou document). Ne cherche pas toi-même de chiffres pour compléter un programme et **n'invente jamais un montant**. Si un montant manque dans la source, la mesure n'est pas encodée : signale-la dans le récapitulatif.
- Lis `BudgetFrance.Web/wwwroot/data/baseline.json` pour connaître les codes de postes et leurs montants de référence, et `programmes.json` pour savoir si le candidat est déjà encodé.
- Travaille sur une branche `programme/<code-candidat>` créée depuis `develop`.

## Règles d'encodage

1. **Une mesure = un montant annuel en Md€, en régime de croisière** (fin de mandat si la source donne une trajectoire), tel que publié par la source. Signe : positif = plus de dépense ou plus de recette ; une baisse d'impôt est négative, une économie aussi.
2. **Source par mesure** : privilégie un chiffrage indépendant (Institut Montaigne, Fipeco, IFRAP, OFCE…). À défaut, prends le chiffrage du programme lui-même. Une mesure ne cite qu'une source : celle dont vient le montant. `sourceLabel` doit permettre de retrouver le chiffre (titre, page ou section).
3. **Rattachement à un poste** (`lineCode`) : un seul poste par mesure. Si une mesure touche deux postes et que la source ventile le montant, crée deux mesures. Sinon, rattache au poste principal et dis-le dans le récapitulatif.
4. **`lineCode: null`** pour tout effet sans poste budgétaire : croissance attendue, lutte contre la fraude, « économies de fonctionnement » non ventilées, effet retour d'une mesure. Ces mesures sont affichées, pas calculées. Applique cette règle même quand la source les compte dans l'équilibre du programme.
5. **Pas de compensation implicite** : n'ajoute aucune mesure que la source ne mentionne pas, même pour équilibrer.
6. **Ordre des mesures** : celui de la source.

## Correspondances usuelles

| Mesure | `lineCode` |
| --- | --- |
| Âge de départ, durée de cotisation, indexation des pensions, minimum vieillesse, dépendance des personnes âgées | `RETRAITES` |
| Hôpital, soignants, remboursements, assurance maladie | `SANTE` |
| Enseignants, école, université | `ENSEIGNEMENT` |
| Armées, programmation militaire | `DEFENSE` |
| Police, gendarmerie, justice, prisons | `SECURITE` |
| Chômage, RSA, minima sociaux, APL, handicap | `PROTECTION_SOCIALE` |
| Allocations familiales, crèches, congés parentaux | `FAMILLE` |
| Logement social, équipements collectifs | `LOGEMENT` |
| Rénovation thermique, biodiversité, déchets | `ENVIRONNEMENT` |
| Aides à l'énergie, boucliers tarifaires, nucléaire | `ENERGIE` |
| Rail, routes, transports en commun | `TRANSPORTS` |
| Aides aux entreprises, agriculture, R&D | `ECONOMIE` |
| Culture, sport, audiovisuel public | `CULTURE` |
| Fonctionnement de l'État, collectivités, aide au développement | `SERVICES_GENERAUX` |
| TVA, TVA sociale | `TVA` |
| Barème de l'IR, CSG, CRDS | `IMPOTS_REVENU` |
| Impôt sur les sociétés | `IMPOT_SOCIETES` |
| CVAE, C3S, taxes foncières, accises, taxes sur les salaires | `IMPOTS_PRODUCTION` |
| ISF, IFI, taxe d'habitation | `AUTRES_IMPOTS_COURANTS` |
| Droits de succession et de donation | `IMPOTS_CAPITAL` |
| Cotisations sociales, baisse de charges | `COTISATIONS` |

`DETTE` et `AUTRES_RECETTES` sont verrouillés : une mesure qui les vise passe en `lineCode: null`. Vérifie toujours les codes dans `baseline.json`, qui fait foi.

## Format

```json
{
  "code": "NOM_CANDIDAT",
  "candidate": "Prénom Nom",
  "party": "Parti",
  "publishedOn": "2027-01-15",
  "measures": [
    { "label": "…", "lineCode": "SANTE", "amountBn": 5, "sourceLabel": "…", "sourceUrl": "https://…" }
  ]
}
```

- `code` : nom du candidat en majuscules, sans accent, stable dans le temps.
- `publishedOn` : date de publication de la source principale.
- Mise à jour d'un programme existant : remplace toutes ses mesures, ne fusionne pas avec l'ancienne version, et change `publishedOn`.
- Mets `updatedOn` du catalogue à la date du jour.

## Étapes

1. Lis la source en entier, puis liste les mesures chiffrées.
2. Encode-les dans `programmes.json` en suivant les règles ci-dessus.
3. Si une règle d'encodage change, mets à jour `/methode` (`Methode.razor`) dans le même commit.
4. Lance `dotnet test`. Le test `ProgrammeCatalogTests` refuse un poste inconnu ou verrouillé, une variation au-delà de ±50 %, un code en double et une URL invalide. Ne contourne jamais un échec : il signale une erreur d'encodage.
5. Ajoute une ligne sous `[Unreleased]` dans `CHANGELOG.md`, rubrique « Données » (crée-la si besoin) : « Programme de X encodé (source, date) ».
6. Commite sur la branche `programme/<code>`, sans fusionner.
7. Donne à l'utilisateur un **récapitulatif à relire** : un tableau mesure | Md€ | poste | source, puis séparément les mesures en `null`, les mesures non encodées faute de montant et les rattachements discutables. La relecture humaine des rattachements est la garantie de neutralité : demande-la explicitement.
