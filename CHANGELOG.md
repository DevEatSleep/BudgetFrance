# Changelog

Format : [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/), versionnage [SemVer](https://semver.org/lang/fr/).

## [Unreleased]

### Ajouté

- Effort restant : montant en Md€ à trouver pour passer sous 3 % de déficit et pour stabiliser la dette.
- Variation en Md€ par poste et par total (dépenses, recettes).
- Remise à 0 % d'un poste (↺) et repère du zéro sur les curseurs.
- Bouton « Partager ce scénario » (copie le lien).
- Message d'erreur si les données ne se chargent pas.
- Priorités du citoyen (3 au plus, partagées dans l'URL) : santé, éducation, climat, sécurité, retraites, solidarité, égalité femmes-hommes, culture. Postes concernés mis en avant, bilan par priorité dans le verdict.
- Pistes de financement selon les priorités : paniers fiscaux, économies hors priorités, mixte ; chaque piste est évaluée et applicable en un clic.
- Programmes 2027 : programmes des candidats encodés en mesures sourcées (`programmes.json`), évalués avec le même calcul, comparés sens par sens aux priorités du citoyen, applicables en un clic. Catalogue validé par les tests ; livré vide.
- Nouveaux postes COFOG de niveau 2 : énergie (GF0403), transports (GF0405), famille et enfance (GF1004).

- Licence MIT (code) et CC BY 4.0 (`programmes.json`).
- Intégration continue (tests sur PR et `develop`) et déploiement sur Render via la branche `deploy`, construite par GitHub Actions.
- Pages « Méthode » (périmètre, calcul statique, règles de faisabilité, sources Eurostat, charte de neutralité, règles d'encodage, limites ; chiffres de l'année de référence lus dans les données) et « Mentions légales ».
- Pied de page commun : simulateur, méthode, mentions légales, code source, signalement d'erreur.
- « Signaler une erreur d'encodage » sur chaque programme, vers une issue GitHub pré-remplie.
- Modèle d'issue « Erreur d'encodage » (correction sourcée obligatoire).

### Modifié

- Verdict compact sur mobile : détails repliables.
- Accessibilité : verdict annoncé aux lecteurs d'écran, curseurs avec valeur textuelle, unités insécables.
- Actions GitHub `checkout` et `setup-dotnet` en v5.
- README : URL du site ; règles, sources et limites publiées sur `/methode`.
- Pied de page de l'accueil allégé : renvoi vers la méthode.

## [0.1.0] - 2026-10-03

### Ajouté

- Importeur Eurostat : dépenses COFOG 2024 des administrations publiques françaises (12 postes, dont retraites et charge de la dette isolées), 8 postes de recettes, PIB, dette, fourchettes des États membres de l'UE.
- Évaluation de faisabilité : plafond de 3 % de déficit, stabilisation de la dette, fourchettes européennes de dépense par fonction et de prélèvements obligatoires.
- Client Blazor WebAssembly : curseurs par poste, comparaison européenne, verdict en direct, scénario partageable par URL.
