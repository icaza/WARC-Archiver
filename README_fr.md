# WARC Archiver — Archivage web à valeur probante

**Langue :** Français · [English (en-US)](README.md)

Application Windows qui **conserve une page web et tout ce qu'elle charge**
sous la forme d'une archive **WARC 1.1 (ISO 28500)**, la **scelle par deux empreintes cryptographiques (SHA-256 et SHA-512)**, consigne
la capture dans un **journal d'audit chaîné** et peut la **signer** avec un certificat X.509. Un vérificateur intégré — et des outils
tiers — permettent de re-contrôler le tout, sans avoir à faire confiance au programme.

> **Pourquoi pas un PDF ou une capture d'écran ?** Ils montrent à quoi une page *ressemblait*. Une archive WARC conserve l'échange HTTP
> lui-même — requête et réponse, en-têtes bruts, HTML, scripts, feuilles de style, images — octet pour octet, avec la date, l'adresse IP
> du serveur et le certificat TLS présentés à ce moment-là. Le contenu peut être relu, rejoué et **contrôlé mathématiquement** des années plus tard.

## Sommaire

1. [Ce que fait l'application](#1-ce-que-fait-lapplication)
2. [Pourquoi cette archive est digne de confiance](#2-pourquoi-cette-archive-est-digne-de-confiance)
3. [Cadre juridique (France / Union européenne)](#3-cadre-juridique-france--union-européenne)
4. [Protocole recommandé pour renforcer la valeur probante](#4-protocole-recommandé-pour-renforcer-la-valeur-probante)
5. [Vérifier une archive sans l'application](#5-vérifier-une-archive-sans-lapplication)
6. [Limites — à lire avant de s'y fier dans un litige](#6-limites--à-lire-avant-de-sy-fier-dans-un-litige)
7. [Installation et lancement](#7-installation-et-lancement)
8. [Utilisation de l'interface](#8-utilisation-de-linterface)
9. [Configuration (`config.json`)](#9-configuration-configjson)
10. [Sécurité et sobriété en ressources](#10-sécurité-et-sobriété-en-ressources)
11. [Pour les développeurs](#11-pour-les-développeurs)
12. [Glossaire](#12-glossaire)

---

## 1. Ce que fait l'application

### 1.1 Déroulé d'une capture

Vous saisissez l'adresse d'une page, éventuellement une référence de dossier, votre nom et une note, puis vous cliquez sur **Start capture**.

1. **Contrôle de l'adresse.** Seules les adresses `http://` et `https://` sont acceptées. Une adresse contenant un nom d'utilisateur ou un
   mot de passe est refusée (les identifiants se retrouveraient dans l'archive).
2. **Entrée au journal d'audit** (`capture.start`) : URL, nom de l'archive, référence du dossier, opérateur, empreinte du `config.json`
   utilisé. Cette entrée sert d'*ancre* à la capture (voir 1.5).
3. **Récupération de la page.** Une requête `GET` en HTTP/1.1, sur une connexion dédiée. Les octets échangés avec le serveur sont enregistrés
   tels quels, avant toute interprétation (pas de décompression automatique, pas de cache, pas de cookies). Les **redirections** sont suivies une
   à une et **chaque étape est archivée**.
4. **Analyse de la page** pour trouver ce qu'elle charge : scripts, feuilles de style, images (y compris `srcset`, `<picture>`, affiches vidéo),
   polices, médias, cadres (`iframe`), objets intégrés ; puis, dans les feuilles de style, les `@import` et les `url(...)` (profondeur configurable).
5. **Récupération des ressources** en parallèle (4 téléchargements simultanés par défaut), chacune avec le même soin que la page elle-même.
   Un échec, une ressource ignorée (limite atteinte) ou une réponse incomplète ne font pas échouer la capture : ils sont **notés dans le manifeste**.
6. **Écriture de l'archive WARC.** Pour chaque échange : un enregistrement `response`, un `request` et un `metadata` ; en tête, un `warcinfo`.
7. **Scellement** : empreintes SHA-256 et SHA-512 de l'archive, du manifeste et de chaque enregistrement ; fichiers de contrôle `.sha256` et `.sha512`.
8. **Signature** facultative du manifeste avec un certificat de votre magasin Windows.
9. **Entrée au journal d'audit** (`capture.complete`) avec les empreintes finales, puis fichiers passés en **lecture seule** (réglage `security.markFilesReadOnly`).

Une capture annulée ou en échec **supprime l'archive inachevée** (aucune pièce partielle ne reste présentable comme complète) ; l'annulation ou l'échec est tout de même consigné au journal.

### 1.2 Ce qui est capturé

| Élément | Où le trouver dans l'archive |
|---|---|
| **Requête HTTP brute** (ligne de requête, en-têtes envoyés) | enregistrement `request` |
| **Réponse HTTP brute** (statut, en-têtes reçus, corps tel que le serveur l'a envoyé — compressé s'il l'était) | enregistrement `response` |
| **HTML** de la page (le « DOM » *tel que servi*, voir [limites](#6-limites--à-lire-avant-de-sy-fier-dans-un-litige)) | `response` de la page principale |
| **Scripts, feuilles de style, images, polices, médias, cadres** | un `response` par ressource |
| **Adresse IP du serveur**, adresses renvoyées par le DNS | en-tête `WARC-IP-Address` et enregistrement `metadata` |
| **Certificat TLS** du serveur (sujet, émetteur, n° de série, validité, empreinte, chaîne, certificat complet en DER/base64), erreurs de validation éventuelles | enregistrement `metadata` (`tls-*`) |
| **Date et heure** de chaque échange (UTC et heure locale avec décalage), durée, fuseau horaire | `WARC-Date` et `metadata` |
| **En-tête `Date` du serveur** de la page principale et **écart avec l'horloge locale** | manifeste (`capture.serverDateHeader`, `capture.clockSkewSeconds`) |
| **Logiciel et version, `User-Agent`** ; **.NET, système d'exploitation, réglages utilisés** | `warcinfo` et `metadata` (logiciel, `User-Agent`) ; manifeste (tout le reste) |
| **Référence du dossier, opérateur, notes** | `warcinfo` et manifeste |

### 1.3 Fichiers produits

Tout est écrit dans le dossier de sortie (`outputDirectory` dans `config.json`). Les noms d'archive sont horodatés (heure locale) ; un fichier existant
n'est **jamais** écrasé.

| Fichier | Rôle |
|---|---|
| `20261010-125353_example.com.warc.gz` | L'archive : un membre gzip par enregistrement (`warcinfo`, `request`, `response`, `metadata`). Fichier `.warc` simple si `compressWarc` vaut `false`. |
| `….manifest.json` | Le **procès-verbal technique** : voir 1.4. |
| `….sha256`, `….sha512` | Empreintes de l'archive **et** du manifeste, au format de `sha256sum -c` / `sha512sum -c`. |
| `….manifest.json.sig` | Seulement si un certificat a été choisi : signature détachée du manifeste (RSA PKCS#1 v1.5 ou ECDSA), avec le certificat public du signataire. |
| `audit-log.jsonl` | Le journal d'audit du dossier (un seul par dossier de sortie) : voir 1.5. |

### 1.4 Le manifeste

Fichier JSON lisible, écrit à côté de l'archive. Il contient :

* **`tool`** — nom, version, version de .NET, système d'exploitation ;
* **`case`** — référence du dossier, opérateur, notes ;
* **`capture`** — URL demandée et URL finale, statut HTTP, début et fin **en heure locale avec décalage UTC et en UTC**, fuseau, en-tête `Date` du serveur,
  écart d'horloge, nombre de ressources archivées / en échec / ignorées, volume ;
* **`settings`** — ce qui a été inclus, `User-Agent`, limites, options de sécurité, **empreinte SHA-256 du `config.json`** utilisé ;
* **`warc`** — nom, taille, SHA-256 et SHA-512 de l'archive, nombre d'enregistrements ;
* **`audit`** — numéro et empreinte de l'entrée du journal écrite au début de la capture ;
* **`issues`** — tout ce qui s'est mal passé ou n'a pas été fait : ressource en échec, ignorée, tronquée, page répondant 404, redirection non suivie… ;
* **`records`** — pour chaque enregistrement : identifiant, type, URL, date, position et longueur dans le fichier, statut, type de média, IP, et **SHA-256 + SHA-512 du bloc et de la charge utile**.

Extrait réel (simplifié) :

```json
{
  "schema": "warc-archiver-manifest/1",
  "tool": { "name": "WarcArchiver", "version": "1.0.0", "runtime": ".NET 10.0.12", "operatingSystem": "…" },
  "case": { "reference": "CASE-2026-001", "operator": "…", "notes": "…" },
  "capture": {
    "targetUrl": "https://example.com/", "finalUrl": "https://example.com/", "mainStatus": 200,
    "startedUtc": "2026-10-10T16:42:49.271Z", "startedLocal": "2026-10-10T18:42:49.271+02:00",
    "timeZoneId": "Europe/Paris", "utcOffset": "+02:00",
    "serverDateHeader": "Sat, 10 Oct 2026 16:42:49 GMT", "clockSkewSeconds": -0.3
  },
  "warc": { "fileName": "….warc.gz", "version": "WARC/1.1", "bytes": 3174041,
            "sha256": "9058…4378", "sha512": "52c2…f9cd", "recordCount": 52 },
  "audit": { "logFile": "audit-log.jsonl", "anchorSeq": 1, "anchorHash": "ba69…a433" },
  "issues": [ { "type": "warning", "url": "…/missing.png", "message": "The server answered HTTP 404." } ],
  "records": [ { "id": "<urn:uuid:…>", "type": "response", "uri": "…", "httpStatus": 200, "ipAddress": "…",
                 "blockSha256": "…", "blockSha512": "…", "payloadSha256": "…", "payloadSha512": "…" } ]
}
```

### 1.5 Le journal d'audit

`audit-log.jsonl` contient une ligne JSON par événement : `capture.start`, `capture.complete`, `capture.cancelled`, `capture.failed`, `capture.rejected`,
`verify.run`, `audit.restart`. Chaque ligne porte un numéro, la date (UTC et locale), les données de l'événement, **l'empreinte SHA-256 de la ligne
précédente** et sa propre empreinte. Modifier, supprimer, insérer ou réordonner une ligne **casse tous les maillons suivants**, ce que la page *Audit log*
et le vérificateur détectent. Un journal endommagé n'est jamais écrasé : il est mis de côté (`audit-log.jsonl.corrupt-…`) et son empreinte est inscrite
dans le nouveau journal.

> **À faire :** copier l'**empreinte de tête** (bouton *Copy head hash*) et l'envoyer à un tiers (courriel à votre avocat, dépôt chez un notaire ou un commissaire de justice…).
> Elle **ancre l'ensemble de l'historique à cette date** : sans ancrage externe, une personne ayant accès au dossier pourrait, en théorie, recalculer tout le journal (voir [limites](#6-limites--à-lire-avant-de-sy-fier-dans-un-litige)).

### 1.6 La vérification intégrée

La page **Verify** (glisser-déposer d'un `.warc`, `.warc.gz` ou `.manifest.json`) re-contrôle, avec **son propre lecteur WARC** :

1. les empreintes SHA-256 / SHA-512 du fichier d'archive, comparées au manifeste ;
2. les fichiers `.sha256` et `.sha512` (archive **et** manifeste) ;
3. la structure WARC et **chaque enregistrement** : empreintes `WARC-Block-Digest` et `WARC-Payload-Digest` recalculées, longueurs, identifiants ;
4. la concordance **enregistrement par enregistrement** avec le manifeste (aucun ajout, aucun retrait, mêmes SHA-256 et SHA-512) ;
5. la signature numérique, si elle existe (le manifeste a-t-il changé depuis la signature ?) ;
6. le journal d'audit : chaîne recalculée, entrée d'ancrage conforme au manifeste, entrée de fin de capture portant l'empreinte exacte de l'archive.

Le rapport (PASS / FAIL / WARNING / SKIPPED / INFO) s'exporte en texte et sert de pièce annexe. Chaque vérification est elle-même inscrite au journal (`verify.run`).

---

## 2. Pourquoi cette archive est digne de confiance

Le principe de conception : **ne rien demander à croire**. Chaque affirmation de l'archive peut être re-contrôlée par un tiers (partie adverse, expert,
juge) avec des outils qu'il choisit lui-même. Les garanties reposent sur sept piliers.

| # | Ce qu'on attend d'une preuve numérique | Ce que fait l'application | Comment le contrôler soi-même |
|---|---|---|---|
| 1 | **Fidélité** — ce qui est présenté est ce qui a été reçu | Enregistre les octets réels échangés avec le serveur (requête et réponse), sans décompression ni reformatage ; une connexion par requête ; HTTP/1.1 seulement | Lire le `.warc.gz` avec un lecteur WARC conforme (relecture testée ici avec `warcio`) |
| 2 | **Intégrité** — rien n'a été modifié depuis | Double empreinte SHA-256 + SHA-512 à trois niveaux : fichier d'archive, manifeste, chaque enregistrement (bloc et charge utile). Fichiers passés en lecture seule | `sha256sum -c`, `sha512sum -c`, `warcio check` (section 5) |
| 3 | **Datation** — quand ? | Heure locale avec décalage UTC, UTC, fuseau, durée ; **en-tête `Date` du serveur** et écart avec l'horloge locale, comme référence indépendante | Comparer `startedUtc` et `serverDateHeader` dans le manifeste |
| 4 | **Traçabilité** — qui, quoi, avec quoi, dans quelles conditions | Référence du dossier, opérateur, notes ; version du logiciel, .NET, système ; réglages et empreinte du `config.json` ; adresse IP, DNS, certificat TLS | Lire `warcinfo`, les `metadata` et le manifeste |
| 5 | **Chaîne de conservation** — l'historique est-il intact ? | Journal d'audit chaîné par hachage, entrée d'ancrage dans le manifeste, vérifications elles-mêmes journalisées, empreinte de tête exportable vers un tiers | Page *Audit log* ; recalcul indépendant (la formule est publique : section 5) |
| 6 | **Imputabilité** — de qui émane la pièce ? | Signature détachée X.509 (RSA ou ECDSA) du manifeste, qui épingle l'archive et chaque enregistrement ; certificat public inclus | `openssl dgst -verify …` (section 5) |
| 7 | **Transparence et contradictoire** — la partie adverse peut-elle vérifier ? | Format ouvert (ISO 28500) ; manifeste en JSON clair ; **toutes les défaillances sont consignées** (échecs, ressources ignorées, réponses tronquées) ; vérificateur indépendant du code d'écriture | Relire le manifeste ; relancer la vérification avec un autre outil |

### Ce qui renforce ces garanties

* **Un format de conservation reconnu.** WARC est la norme ISO 28500, utilisée par les bibliothèques nationales et l'Internet Archive. Les archives produites ici sont
  relues sans erreur par `warcio` (bibliothèque tierce), et une archive volontairement altérée est rejetée par `warcio` comme par le vérificateur intégré.
* **Deux fonctions de hachage.** SHA-256 et SHA-512 sont calculées en parallèle ; falsifier une archive sans que l'une ou l'autre ne le révèle n'est pas réaliste.
* **Un procédé de capture sans interprétation.** Pas de navigateur embarqué qui réécrirait la page, pas de décompression automatique, pas de cookies, pas de
  cache : ce qui est enregistré est ce qui a transité sur la connexion (l'enregistrement se fait sur le flux déchiffré, au niveau TLS).
* **Des preuves de provenance réseau.** Le certificat TLS complet, sa chaîne, l'adresse IP et les adresses renvoyées par le DNS sont conservés. Le serveur contacté est
  identifiable, et d'éventuelles anomalies (certificat invalide, redirection inattendue) sont visibles.
* **Aucun maquillage des incidents.** Une page renvoyant une erreur est archivée telle quelle (« la preuve que la page répondait 404 » est aussi une preuve) ; une limite de
  taille atteinte est marquée `WARC-Truncated` et signalée au manifeste.
* **Un procédé sûr et non intrusif.** TLS 1.2/1.3 uniquement, certificats vérifiés par défaut, adresses privées/locales refusées (y compris après redirection), aucun
  identifiant accepté dans l'URL, création de fichiers sans écrasement, un seul processus à la fois sur le journal (section 10).
* **Des tests.** Le moteur est couvert par 155 contrôles automatiques de bout en bout (`tests/WarcArchiver.SelfTest`) contre un site local HTTP/HTTPS : capture, relecture,
  détection de falsification enregistrement par enregistrement, signatures RSA et ECDSA (y compris échec après modification du manifeste), journal d'audit,
  limites, annulation, sécurité réseau.

### Ce que l'archive établit — et ce qu'elle n'établit pas

**Elle établit** qu'à une date donnée, depuis une machine donnée (logiciel, version, système, horloge), l'opérateur nommé a demandé l'adresse `X` et a reçu d'un serveur
d'adresse IP `Y`, présentant le certificat `Z`, **exactement ces octets**, et que ceux-ci n'ont pas changé depuis (empreintes), ce que tout tiers peut re-contrôler.

**Elle n'établit pas, à elle seule :** que le contenu est *vrai* ; que tous les visiteurs voyaient la même chose (contenu personnalisé, géolocalisé, A/B) ; ce qu'un navigateur aurait
affiché après exécution de JavaScript ; une date opposable sans horodatage par un tiers de confiance ; l'identité civile de l'opérateur au-delà du certificat qu'il utilise.
Ces points sont traités en section 4 (bonnes pratiques) et en section 6 (limites).

---

## 3. Cadre juridique (France / Union européenne)

> **Avertissement.** Cette section informe ; elle n'est **pas un avis juridique**. L'admissibilité et la force probante d'une pièce relèvent de l'appréciation du juge
> et dépendent de la juridiction, de la nature du litige et des circonstances. L'application **n'est ni certifiée ni qualifiée** (ni ANSSI, ni eIDAS, ni NF Z67-147).
> Faites valider votre usage par un avocat ou un commissaire de justice avant un litige important.

**Principe : en France, la preuve est le plus souvent libre.** Prouver qu'un contenu était en ligne à telle date est, la plupart du temps, prouver un *fait* : la preuve peut
être apportée par tout moyen (art. 1358 du Code civil ; en matière commerciale art. L110-3 du Code de commerce ; en matière pénale art. 427 du Code de procédure pénale). Il n'existe pas de
« label » légal obligatoire pour une capture web : le juge apprécie la **fiabilité** de la pièce, et la partie adverse peut la contester. C'est précisément là que se joue l'intérêt de cet outil :
fournir des éléments **techniques, documentés et vérifiables** plutôt qu'une simple image.

| Texte | En substance | Lien avec l'application |
|---|---|---|
| Code civil, art. 1366 | L'écrit électronique a la même force probante que l'écrit papier, sous réserve que **la personne dont il émane soit dûment identifiée** et qu'il soit **établi et conservé dans des conditions de nature à en garantir l'intégrité**. | Identification : opérateur, référence du dossier, signature X.509 facultative, journal d'audit. Intégrité : double empreinte à trois niveaux, lecture seule, vérificateur, conservation recommandée (section 4). |
| Code civil, art. 1367 ; décret n° 2017-1416 | Une signature électronique **qualifiée** bénéficie d'une présomption de fiabilité. | La signature de l'application est une **signature simple** : elle n'est pas, en l'état, une signature qualifiée. Elle reste un élément d'imputabilité librement apprécié. |
| Règlement (UE) n° 910/2014 (eIDAS), art. 25 | Une signature électronique ne peut être refusée comme preuve en justice **au seul motif** qu'elle est électronique ou qu'elle n'est pas qualifiée. | La signature facultative du manifeste ne peut être écartée *pour ce seul motif*. |
| Règlement (UE) n° 910/2014, art. 41 | Un **horodatage électronique qualifié** bénéficie d'une présomption d'exactitude de la date et de l'heure et d'intégrité des données. | **Non intégré.** Fortement recommandé en complément pour dater de façon opposable (section 4, étape 6). |
| Constat de commissaire de justice (ex-huissier) ; norme AFNOR NF Z67-147 | Force probante renforcée d'un constat réalisé par un officier public, avec procédure normalisée pour les constats sur Internet. | L'application **ne remplace pas** un constat. Elle le **complète** (collecte rapide, conservation complète, contrôle ultérieur) ou le prépare. |
| ISO/IEC 27037 | Lignes directrices pour l'identification, la collecte, l'acquisition et la préservation des preuves numériques : documentation, traçabilité, reproductibilité, altération minimale. | La conception s'en **inspire** (journal, manifeste, empreintes, procédé sans interprétation). Aucune certification n'est revendiquée. |
| RGPD | Une page archivée peut contenir des données personnelles. | La conservation à des fins de constatation, d'exercice ou de défense d'un droit en justice peut se justifier ; définissez finalité, durée de conservation et accès. |
| À titre d'information, hors UE | États-Unis : règles 902(13) et 902(14) des *Federal Rules of Evidence* (authentification de données électroniques, notamment par valeur de hachage, sur attestation d'une personne qualifiée). | Même logique : l'empreinte est le support de l'authentification. |

**Loyauté de la collecte.** L'application n'accède qu'à ce qui est **publiquement accessible sans authentification** : pas de cookies, pas d'identifiants, aucune protection contournée.
Elle ne consulte pas `robots.txt` (la capture est un acte ponctuel de l'opérateur ; le choix est consigné dans le `warcinfo`). Elle ne contourne ni mur de connexion, ni
paywall, ni limitation d'accès.

**Ce que le juge ou l'expert regardera, et ce que l'application lui donne :**

| Question probable | Réponse que l'archive permet de documenter |
|---|---|
| Le contenu a-t-il pu être modifié depuis ? | Empreintes du fichier, de chaque enregistrement et du manifeste ; signature ; journal chaîné |
| La capture est-elle complète ? | Liste des ressources, des échecs et des limites dans `issues` ; `WARC-Truncated` |
| Le serveur contacté était-il bien le bon ? | IP, DNS, certificat TLS et chaîne conservés |
| Quand a-t-elle été faite ? | Heure locale + UTC + fuseau, `Date` du serveur, écart d'horloge ; horodatage tiers si vous l'ajoutez |
| Qui l'a faite, comment ? | Opérateur, version de l'outil, système, réglages, empreinte du `config.json` |
| Le procédé est-il reproductible et contrôlable ? | Format ouvert, vérification indépendante, commandes de la section 5 |

---

## 4. Protocole recommandé pour renforcer la valeur probante

L'outil produit des éléments techniques solides ; **la manière de s'en servir** fait le reste.

1. **Préparer le poste.** Système à jour, horloge **synchronisée** (`w32tm /query /status`, ou *Paramètres → Heure et langue → Synchroniser maintenant*),
   pas de VPN ni de proxy inutiles (sinon, notez-le). Fermez les programmes superflus.
2. **Renseigner la capture.** Référence du dossier, nom de l'opérateur (réglable une fois pour toutes via `defaults.operatorName`), note sur la raison de la capture.
3. **Choisir un certificat de signature** (page *Capture* → *Sign the manifest with*). Importez au préalable votre certificat `.pfx` dans *Utilisateur actuel → Personnel*.
   Préférez un certificat émis par une autorité reconnue (idéalement qualifiée) à un certificat auto-signé.
4. **Capturer, puis vérifier immédiatement** (*Verify this archive*) et **exporter le rapport** de vérification.
5. **Ancrer le journal.** Page *Audit log* → *Copy head hash* → envoyez-le par courriel (à vous-même, à votre avocat) ou faites-le déposer chez un commissaire de justice ou un notaire.
6. **Horodater par un tiers de confiance** (recommandé). Obtenez un jeton RFC 3161 pour le **manifeste** (qui épingle l'archive entière) auprès d'un prestataire de services de confiance
   **qualifié** (liste de confiance européenne). Avec OpenSSL :
   ```bash
   openssl ts -query -data 20261010-125353_example.com.manifest.json -sha256 -cert -out demande.tsq
   curl -H "Content-Type: application/timestamp-query" --data-binary @demande.tsq https://<URL-DU-SERVICE-D-HORODATAGE> -o reponse.tsr
   openssl ts -verify -in reponse.tsr -queryfile demande.tsq -CAfile <chaine-de-certification-du-prestataire>.pem
   ```
   Conservez `demande.tsq`, `reponse.tsr` et la chaîne de certification du prestataire avec l'archive.
7. **Conserver correctement.** Gardez ensemble l'archive, le manifeste, les `.sha256`/`.sha512`, la signature, le **journal d'audit** et le rapport de vérification. Faites **plusieurs copies**
   (dont une hors ligne, sur support non réinscriptible si possible), notez l'empreinte SHA-256 dans un document séparé et **travaillez toujours sur une copie**, jamais sur l'original.
8. **Pour un enjeu important,** doublez d'un **constat de commissaire de justice**. L'archive reste utile : elle conserve davantage (toutes les ressources, les échanges bruts) et se re-contrôle.
9. **Communiquer la pièce** avec la présente documentation, la version de l'outil, le rapport de vérification et les commandes de contrôle indépendant (section 5) afin que la partie adverse
   et l'expert puissent tout refaire.

---

## 5. Vérifier une archive sans l'application

L'intérêt d'un format ouvert est que personne n'a à croire ce programme.

```bash
sha256sum -c 20261010-125353_example.com.sha256      # empreintes de l'archive et du manifeste
sha512sum -c 20261010-125353_example.com.sha512
gzip -t 20261010-125353_example.com.warc.gz           # chaque membre gzip est intact
pip install warcio && warcio check 20261010-125353_example.com.warc.gz   # digests de chaque enregistrement
```

Sous Windows (PowerShell), sans rien installer :

```powershell
Get-FileHash .\20261010-125353_example.com.warc.gz -Algorithm SHA256   # à comparer au fichier .sha256 et au manifeste
Get-FileHash .\20261010-125353_example.com.warc.gz -Algorithm SHA512
```

**Signature** (fichier `.sig` : JSON contenant `signature` en base64, `signerCertificateDer` en base64, `hashAlgorithm` et `signatureAlgorithm`).
Exemple pour une signature **RSA** avec OpenSSL (cas `"signatureAlgorithm": "RSA-PKCS1"` et `"hashAlgorithm": "SHA256"` ; sinon adaptez `-sha256` en `-sha512`) :

```powershell
# OpenSSL est fourni, par exemple, avec Git for Windows ; PowerShell 5.1 : n'utilisez pas « > » pour écrire un fichier PEM (il l'enregistrerait en UTF-16), d'où « -out ».
$s = Get-Content .\20261010-125353_example.com.manifest.json.sig -Raw | ConvertFrom-Json
[IO.File]::WriteAllBytes((Join-Path $PWD "signataire.cer"), [Convert]::FromBase64String($s.signerCertificateDer))
[IO.File]::WriteAllBytes((Join-Path $PWD "signature.bin"),  [Convert]::FromBase64String($s.signature))
openssl x509 -inform DER -in signataire.cer -pubkey -noout -out signataire.pub
openssl dgst -sha256 -verify signataire.pub -signature signature.bin 20261010-125353_example.com.manifest.json
# → "Verified OK" ; toute modification du manifeste donne "Verification failure"
```

Pour une signature **ECDSA**, la signature est au format « r‖s » brut (IEEE P1363) : convertissez-la en DER avant de la donner à OpenSSL, ou utilisez le vérificateur intégré.
Pensez aussi à contrôler **le certificat lui-même** (émetteur, validité, empreinte `signerThumbprintSha256`).

**Journal d'audit.** La formule de chaînage est simple : l'empreinte d'une entrée est le SHA-256 de la concaténation (séparée par des sauts de ligne) du numéro, de la date UTC, de la date
locale, de l'événement, des données (clés triées, une ligne par donnée au format `longueur(clé):clé=longueur(valeur):valeur`, longueurs en unités UTF-16 comme dans .NET, texte encodé en UTF-8) et de
l'empreinte précédente (64 zéros pour la première). Toute personne peut la recalculer (voir `src/WarcArchiver.Core/Audit/AuditLog.cs`, méthode `ComputeHash`) ; cette formule a été
re-calculée avec un script indépendant sur un journal produit par l'application (résultat identique).

---

## 6. Limites — à lire avant de s'y fier dans un litige

* **Le « DOM » est le HTML tel que servi.** Les scripts sont archivés mais **pas exécutés** : le contenu qu'une page fabrique avec JavaScript après son chargement (appels `fetch`/XHR, rendu côté client) **n'est pas dans l'archive**.
  Une capture par navigateur sans interface pourrait être ajoutée.
* **Pas d'authentification.** Les pages qui exigent une connexion (réseaux sociaux privés, espaces clients) ne sont pas accessibles ; il n'y a ni cookies ni session.
* **Ce que *vous* avez reçu, pas forcément ce que tous voient.** Un site peut varier son contenu selon l'adresse IP, la langue, le `User-Agent` ou l'heure. Les réglages utilisés sont consignés pour pouvoir l'expliquer.
* **L'heure est celle de votre PC.** Le manifeste ajoute l'en-tête `Date` du serveur et l'écart mesuré, mais **l'horodatage reste auto-déclaré** (de même que l'heure de signature, `signedUtc`).
  Pour une datation indépendante, ajoutez un horodatage RFC 3161 (section 4, étape 6).
* **Le journal d'audit n'est pas inviolable seul.** Il est *résistant à la falsification apparente*, pas infalsifiable : le hachage n'étant pas à clé, quelqu'un qui contrôle tout le dossier pourrait recalculer
  la chaîne entière. C'est pourquoi l'**ancrage externe** de l'empreinte de tête (section 1.5) est important. De même, l'attribut « lecture seule » n'est qu'un garde-fou : ce sont les empreintes et la signature qui protègent.
* **« Signature SHA-256/SHA-512 »** signifie ici : empreintes cryptographiques + signature X.509 *facultative* du manifeste. Ce n'est pas une signature électronique qualifiée au sens d'eIDAS.
* **Le certificat de signature** doit être dans le magasin Windows *Personnel* (importez un `.pfx`) ; un certificat expiré est signalé dans la liste.
* **Réseau.** HTTP/1.1 uniquement ; une requête `GET` par ressource ; DNS de votre machine (les adresses obtenues sont consignées, mais le DNS n'est pas signé). Les téléchargements dépassant les limites sont tronqués et marqués comme tels ;
  `robots.txt` n'est pas consulté.
* **Données personnelles et droits des tiers.** L'archive est conçue pour la preuve, pas pour la republication. Respectez le RGPD et les droits d'auteur dans l'usage et la conservation.
* **Aucun avis juridique.** L'admissibilité d'une pièce, la procédure complémentaire attendue (témoin, commissaire de justice, notaire) et sa force probante dépendent de votre juridiction et du litige. L'outil fournit la preuve **technique**.

---

## 7. Installation et lancement

Prérequis : Windows 10/11 x64 et le [SDK .NET 10](https://dotnet.microsoft.com/download) pour compiler.

```powershell
dotnet build WarcArchiver.slnx -c Release
dotnet run --project src/WarcArchiver.App -c Release
```

**Dossier prêt à copier** (`publish\`) :

```powershell
.\publish.cmd            # petite version : l'ordinateur cible doit avoir le « .NET Desktop Runtime 10 » (x64)
.\publish.cmd portable   # version autonome (environ 150 Mo) : fonctionne sur tout Windows 10/11 x64, sans rien installer
```

Copiez **le dossier entier** (`WarcArchiver.exe`, `WarcArchiver.dll`, `WarcArchiver.runtimeconfig.json`, `config.json`…), pas seulement l'exécutable, pour la petite version.

### « You must install .NET Desktop Runtime to run this application »

La petite version nécessite le **.NET Desktop Runtime 10 (x64)** sur le PC qui l'exécute. Vérifiez avec `dotnet --list-runtimes` : la liste doit contenir `Microsoft.WindowsDesktop.App 10.0.x`.
(Le « .NET Runtime » seul et l'« ASP.NET Core Runtime » ne le contiennent **pas**, et une installation x86 ne convient pas à un programme x64.) Installez-le depuis
<https://dotnet.microsoft.com/download/dotnet/10.0> ou compilez la version autonome avec `.\publish.cmd portable`.

**Test du moteur** (démarre un site de test local HTTP + HTTPS ; fonctionne sous Windows et Linux) :

```
dotnet run --project tests/WarcArchiver.SelfTest -- <dossier-de-travail>
```

---

## 8. Utilisation de l'interface

L'interface est en anglais (en-US), thème sombre, navigation par le menu de gauche.

* **Capture** — collez l'URL ; renseignez au besoin référence du dossier, opérateur et notes ; choisissez ce qu'il faut inclure (scripts, feuilles de style, images, polices/médias/cadres,
  ressources d'autres domaines) et un éventuel certificat de signature ; cliquez sur *Start capture*. Chaque requête s'affiche en direct dans le tableau (heure, statut, type, URL, taille, SHA-256, durée, remarque).
  *Cancel* abandonne la capture et supprime l'archive inachevée. À la fin : *Open folder*, *Verify this archive*, *Copy SHA-256*.
* **Archives** — tout ce qui se trouve dans le dossier de sortie, avec filtre et tri ; double-clic pour vérifier, copie du SHA-256, ouverture du dossier.
* **Verify** — déposez un `.warc`, `.warc.gz` ou `.manifest.json` sur la page. Le rapport s'exporte en texte.
* **Audit log** — parcourez le journal, recalculez la chaîne (*Verify chain*), copiez l'**empreinte de tête** (*Copy head hash*), affichez le fichier.
* **Settings** — affiche le `config.json` actif, l'ouvre dans votre éditeur, le recharge (*Reload*), change le dossier de sortie **sans perdre vos commentaires**.

Une seule opération longue (capture, vérification, contrôle du journal) à la fois : cela ménage les ressources et évite que le lecteur et l'écrivain du journal se croisent.

---

## 9. Configuration (`config.json`)

Le fichier se trouve à côté de `WarcArchiver.exe` (ou dans `%LOCALAPPDATA%\WarcArchiver` si ce dossier est en lecture seule). Les commentaires `//` et les virgules finales sont autorisés. Les valeurs hors plage sont
**ramenées dans une plage sûre**. Un fichier contenant une erreur de syntaxe n'est **jamais écrasé** : l'application démarre avec les valeurs par défaut, vous le signale, et sur *Reload* conserve les réglages valides précédents.
L'empreinte du fichier utilisé est inscrite au journal et au manifeste de chaque capture.

| Clé | Défaut | Signification |
|---|---|---|
| `outputDirectory` | `%USERPROFILE%\Documents\WARC Archives` | Dossier des archives, manifestes et du journal. Les `%VARIABLES%` sont développées. |
| `compressWarc` | `true` | `.warc.gz` (un membre gzip par enregistrement) ou `.warc` simple. |
| `warcPrimaryDigest` | `"sha256"` | Empreinte écrite dans les en-têtes WARC (`"sha256"` ou `"sha512"`). Le manifeste et les fichiers de contrôle portent **toujours les deux**. |
| `network.userAgent`, `network.acceptLanguage` | proche de Chrome / `en-US,en;q=0.9` | Envoyés avec chaque requête (consignés dans l'archive). |
| `network.connectTimeoutSeconds`, `network.requestTimeoutSeconds`, `network.maxRedirects` | 15 / 90 / 10 | Les redirections sont suivies une à une et chaque étape est archivée. |
| `network.maxParallelDownloads` | 4 (1–16) | Plus bas = plus léger pour votre PC et pour le site. |
| `network.allowPrivateNetworks` | `false` | Adresses de bouclage, privées, link-local, CGNAT, ULA… **refusées**, y compris après une redirection. |
| `network.allowInvalidCertificates` | `false` | `true` permet de capturer un site au certificat invalide ; le problème est alors **enregistré comme preuve**. |
| `network.proxyUrl` | `""` | Proxy HTTP facultatif (la protection contre les réseaux privés ne voit pas derrière un proxy). |
| `limits.maxResources`, `maxResourceMegabytes`, `maxTotalMegabytes` | 500 / 50 / 1000 | Garde-fous ; une limite atteinte est inscrite au manifeste. |
| `limits.maxCssDepth` | 2 | Niveaux de `@import` / `url()` suivis dans les feuilles de style. |
| `limits.maxParseMegabytes`, `maxMemoryKilobytesPerBuffer` | 8 / 256 | Taille maximale analysée ; tampon mémoire par téléchargement (au-delà : fichier temporaire). |
| `defaults.*` | tout à `true` | État initial des cases de la page *Capture* ; `operatorName` préremplit le champ opérateur. |
| `signing.certificateThumbprint` | `""` | Empreinte SHA-1 d'un certificat (avec clé privée) de *Utilisateur actuel → Personnel*, utilisé par défaut. |
| `security.markFilesReadOnly` | `true` | Attribut lecture seule sur les fichiers terminés. |

---

## 10. Sécurité et sobriété en ressources

**Sécurité**

* TLS 1.2/1.3 uniquement ; une erreur de certificat interrompt la capture, sauf si vous l'autorisez (et dans ce cas l'erreur est consignée).
* Seules les adresses `http`/`https` sont acceptées ; les URL contenant des identifiants sont refusées.
* Les adresses non publiques (loopback, réseau privé, link-local, métadonnées cloud…) sont bloquées **au moment de la connexion, après la résolution DNS**, donc ni un DNS piégé ni une redirection ne peuvent
  amener l'application à sonder votre réseau.
* Fichiers créés en `CreateNew` (aucun écrasement), champs d'en-tête nettoyés contre l'injection, valeurs de configuration bornées, configuration invalide jamais écrasée.
* Un verrou d'instance unique empêche deux processus d'écrire dans le même journal d'audit.

**Ressources**

* Aucun moteur de navigateur embarqué ; les téléchargements passent par de petits tampons (débordement vers un fichier temporaire supprimé automatiquement).
* Récupérateur de mémoire en mode « station de travail non concurrent » avec `ConserveMemory` ; `InvariantGlobalization` évite de charger ICU.
* Quatre téléchargements en parallèle par défaut, réglables.

---

## 11. Pour les développeurs

```
src/WarcArchiver.Core        moteur : écriture/lecture WARC, capture HTTP, analyse, journal d'audit, signature, vérificateur   (net10.0)
src/WarcArchiver.App         interface WinForms ; chaque contrôle est déclaré dans MainForm.Designer.cs                     (net10.0-windows)
tests/WarcArchiver.SelfTest  155 contrôles de bout en bout sur un site local HTTP/HTTPS
```

### Modifier l'interface dans Visual Studio

Ouvrez `MainForm.cs` dans le Designer : chaque contrôle, couleur, police, marge et événement est une simple affectation de propriété dans `MainForm.Designer.cs` ; ce que le Designer montre est ce qui s'exécute.

* **Gardez `InitializeComponent()` pur.** Du code ajouté à cet endroit (méthodes d'aide, boucles, `var`, appels à `DarkTheme`) n'est pas compris par le Designer et **supprimé à la prochaine
  sauvegarde du formulaire**, ce qui laisse des boutons vides et des mises en page écrasées. Placez votre code dans `MainForm.cs`, après l'appel à `InitializeComponent()`.
* **Les cinq pages sont empilées.** Dans le Designer, seule la page *Capture* est au premier plan. Pour travailler sur une autre : *Affichage → Autres fenêtres → Plan du document*, sélectionnez par exemple
  `archivesPage`, clic droit → *Mettre au premier plan*. L'ordre d'empilement est sans effet à l'exécution (le code du menu décide quelle page est visible).
* **Les couleurs sont dans le fichier Designer** (fond `18,20,24`, surface `26,29,35`, relevé `34,38,46`, bordure `52,58,68`, texte `230,232,235`, atténué `154,163,175`, accent `76,154,255`).
  `Theme/DarkTheme.cs` n'ajoute que ce qu'un fichier Designer ne peut pas stocker : le rendu sombre de la barre d'état, l'aspect grisé des boutons désactivés, les grilles sans scintillement et les barres de défilement/titre sombres.
* `Button.Tag` a un sens : `"nav"` pour les boutons du menu, `"primary"` pour les boutons d'action bleus.
* Le texte *Version*, le changement de page et la liste des certificats sont renseignés par le code ; ne les saisissez pas dans le Designer.

---

## 12. Glossaire

| Terme | Signification |
|---|---|
| **WARC** | *Web ARChive*, format de conservation d'échanges web normalisé par l'ISO 28500. Un fichier contient des « enregistrements » (`warcinfo`, `request`, `response`, `metadata`) mis bout à bout, ici chacun dans son propre membre gzip. |
| **Empreinte (hash)** | Valeur de longueur fixe calculée à partir d'un contenu (SHA-256 : 256 bits, SHA-512 : 512 bits). Modifier un seul bit du contenu change complètement l'empreinte. |
| **Bloc / charge utile** | Le *bloc* d'un enregistrement est tout son contenu (en-têtes HTTP + corps) ; la *charge utile* est le seul corps de la réponse, tel que le serveur l'a envoyé (découpage « chunked » retiré, compression conservée). |
| **Manifeste** | Procès-verbal technique en JSON qui récapitule la capture et épingle l'archive par ses empreintes. |
| **Journal d'audit** | Registre des actions de l'application où chaque ligne contient l'empreinte de la précédente. |
| **Ancrage** | Fait d'inscrire l'empreinte de tête du journal (ou du manifeste) chez un tiers, qui fixe l'état de l'historique à cette date. |
| **Signature X.509 détachée** | Signature numérique d'un fichier avec la clé privée d'un certificat ; le fichier `.sig` contient la signature et le certificat public, sans modifier le fichier signé. |
| **Horodatage RFC 3161** | Jeton délivré par un tiers de confiance (autorité d'horodatage) attestant qu'une empreinte existait à un instant donné ; *qualifié* s'il provient d'un prestataire qualifié eIDAS. |
| **Écart d'horloge (`clockSkewSeconds`)** | Différence entre l'heure indiquée par le serveur (en-tête `Date`) et l'horloge UTC du PC au moment de la requête (inclut la latence réseau et un arrondi à la seconde). |
| **eIDAS** | Règlement européen n° 910/2014 sur l'identification électronique et les services de confiance (signatures, cachets, horodatages). |
| **Commissaire de justice** | Officier public et ministériel issu de la fusion des huissiers de justice et des commissaires-priseurs judiciaires ; peut dresser des constats, y compris sur Internet. |
