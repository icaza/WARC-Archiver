# WarcSigner 🛡️

Une application console Windows légère qui horodate les manifestes d'archives web à l'aide du protocole RFC 3161.
Elle est conçue pour fonctionner conjointement avec WARC-Archiver : elle obtient un horodatage cryptographique (`.tsr`) auprès d'une autorité d'horodatage (TSA), prouvant que le manifeste existait, sans modification, à un moment précis dans le temps.

# Fonctionnalités clés

- **Dépendances minimales :** la seule dépendance est le paquet NuGet natif de Microsoft `System.Security.Cryptography.Pkcs` (qui ne fait pas partie du runtime .NET de base). Aucune bibliothèque tierce (BouncyCastle ou similaire) n'est utilisée.
- **Vérification multicouche, appliquée plutôt que simplement affichée :**
  1. Vérification de la réponse : statut PKI, empreinte du message, politique et **nonce aléatoire par requête** (une réponse obsolète ou rejouée est rejetée).
  2. Vérification de la signature : la signature CMS est valide et liée au hachage SHA-256 du manifeste.
  3. Vérification du certificat TSA : il comporte l'utilisation de clé étendue (EKU) `id-kp-timeStamping`, était valide au moment de l'horodatage, et sa chaîne de certification est de confiance (magasin de confiance du système d'exploitation, ou racine épinglée avec `--ca`). La révocation est vérifiée lorsque les points de terminaison CRL/OCSP sont accessibles.
- **TSA authentifiées (payantes / qualifiées) :** authentification HTTP Basic et certificats clients TLS (fichiers `.pfx` protégés par mot de passe), configurés dans un fichier JSON externe. Les secrets ne transitent jamais par la ligne de commande et ne sont jamais affichés.
- **Sûr par défaut :** rien n'est écrit tant que toutes les vérifications n'ont pas réussi ; un fichier `.tsr` existant n'est jamais écrasé ; la réponse de la TSA est limitée à 1 Mio, les requêtes expirent après 30 s et les redirections HTTP sont refusées.
- **Mémoire constante :** le manifeste est haché sous forme de flux (stream), quelle que soit sa taille.
- **Prêt pour l'automatisation :** code de sortie `0` en cas de succès, `1` en cas d'échec ; les erreurs sont envoyées sur stderr.
- **Journaux prédictifs :** messages en anglais, marqueurs ASCII, heures UTC ISO 8601, culture invariante.

# Valeur juridique (RFC 3161 & eIDAS)

WARC-Archiver garantit l'intégrité des octets capturés, mais l'horloge système locale est auto-déclarée.
L'envoi de l'empreinte SHA-256 du fichier `.manifest.json` à une TSA ancré le manifeste à une horloge indépendante.

Il convient de distinguer les deux types d'horodatages prévus par le règlement européen n° 910/2014 (eIDAS) :

- **Horodatage électronique qualifié** (émis par un prestataire de services de confiance qualifié figurant sur la liste de confiance de l'UE) : bénéficie de la présomption d'exactitude de la date et de l'heure ainsi que de l'intégrité des données (article 41, paragraphe 2).
- **Tout autre horodatage** : ne peut se voir dénier d'effet juridique ou d'admissibilité en justice au seul motif qu'il n'est pas qualifié (article 41, paragraphe 1), mais il ne bénéficie pas de cette présomption.

WarcSigner **ne peut pas déterminer si une TSA est qualifiée**. La TSA par défaut est un service public générique et ne doit pas être considérée comme qualifiée. Pour des preuves nécessitant cette présomption, utilisez un prestataire qualifié et vérifiez-le dans la liste de confiance de l'UE.

### Prérequis

- Windows 10 / 11 (x64)
- .NET 10 SDK (pour compiler le code)

### Installation et Compilation

Clonez ce répertoire, puis dans le dossier de l'application, ajoutez le paquet Pkcs (une seule fois) et compilez en mode Release :

> dotnet add package System.Security.Cryptography.Pkcs

> dotnet build -c Release

L'exécutable est généré dans le dossier `bin/Release/net10.0/`.

# Mode d'emploi

Syntaxe de la commande :

> WarcSigner.exe <chemin_manifeste> [url_tsa] [--config <fichier.json>] [--ca <racine.cer>] [--allow-untrusted]

| Argument | Signification |
|---|---|
| `chemin_manifeste` | Le fichier manifeste à horoder (obligatoire). |
| `url_tsa` | URL absolue `http://` ou `https://` de la TSA. Par défaut : `http://timestamp.digicert.com`, ou `tsaUrl` défini dans le fichier de configuration. |
| `--config <fichier>` | Fichier JSON contenant l'URL de la TSA, le certificat racine et les identifiants (voir [Fichier de configuration](#fichier-de-configuration-identifiants-tsa)). |
| `--ca <fichier>` | Faire confiance **uniquement** à ce certificat racine (PEM ou DER) pour la chaîne de la TSA. À utiliser pour épingler la racine d'une TSA qui ne se trouve pas dans le magasin de confiance Windows, telle que la racine publiée par un prestataire qualifié. |
| `--allow-untrusted` | Enregistre la réponse même si la chaîne de la TSA n'est pas de confiance. Le jeton ne dispose alors d'aucune ancre de confiance vérifiée ; non recommandé. |

Exemples :

- TSA publique par défaut :

> WarcSigner.exe "C:\Archives\20261010-134511_veydunet.com.manifest.json"

- Une TSA de votre choix, en épinglant son certificat racine :

> WarcSigner.exe "C:\Archives\20261010-134511_veydunet.com.manifest.json" "https://tsa.example.com/rfc3161" --ca "C:\pki\tsa-root.cer"

### Fichier de configuration (identifiants TSA)

Les TSA commerciales et qualifiées nécessitent généralement une authentification. Décrivez la TSA une seule fois dans un fichier JSON et transmettez-le via l'argument `--config`. Les chemins relatifs à l'intérieur du fichier sont résolus à partir du dossier du fichier lui-même. Les commentaires et les virgules de fin sont tolérés ; les clés inconnues sont rejetées, de sorte qu'une faute de frappe telle que `pasword` génère une erreur au lieu d'ignorer silencieusement un identifiant manquant.

Connexion HTTP Basic (le mot de passe est lu depuis une variable d'environnement) :

```json
{
  "tsaUrl": "https://tsa.example.com/rfc3161",
  "rootCertificate": "tsa-root.cer",
  "authentication": {
    "username": "mon-compte",
    "passwordEnv": "WARCSIGNER_TSA_PASSWORD"
  }
}
```

Certificat client TLS (fichier `.pfx` protégé par mot de passe)

```json
{
  "tsaUrl": "https://tsa.example.com/rfc3161",
  "rootCertificate": "tsa-root.cer",
  "clientCertificate": {
    "pfxPath": "mon-certificat-client.pfx",
    "passwordEnv": "WARCSIGNER_PFX_PASSWORD"
  }
}
```

Les deux sections peuvent être combinées. Un modèle prêt à l'emploi est fourni dans `warcsigner.config.example.json`.

| Clé | Signification |
|---|---|
| `tsaUrl` | Point de terminaison de la TSA. **Obligatoire** lorsque le fichier contient des identifiants. |
| `rootCertificate` | Certificat racine public à épingler (similaire à `--ca` ; `--ca` l'emporte si les deux sont fournis). Il ne s'agit pas d'un fichier `.pfx`. |
| `authentication.username` | Nom d'utilisateur HTTP Basic (pas de `:`, pas de caractères de contrôle). |
| `authentication.passwordEnv` | Nom de la variable d'environnement contenant le mot de passe. **Recommandé.** |
| `authentication.password` | Mot de passe en clair dans le fichier. Fonctionne, mais un avertissement est affiché à chaque exécution. |
| `clientCertificate.pfxPath` | Fichier `.pfx` / `.p12` contenant la clé privée. |
| `clientCertificate.passwordEnv` / `password` | Les deux mêmes options que ci-dessus, pour le mot de passe du fichier `.pfx`. |

Définissez la variable avant l'exécution (PowerShell) :

> $env:WARCSIGNER_TSA_PASSWORD = "votre-mot-de-passe"

Si vous fournissez un nom d'utilisateur (ou un `.pfx` protégé) sans source de mot de passe, WarcSigner demande le mot de passe dans la console, en masquant les caractères saisis. Lorsque l'entrée est redirigée (scripts, CI), le programme ne s'arrête jamais pour attendre : il s'arrête avec une erreur explicite ; utilisez donc `passwordEnv` dans ces contextes.

Règles de sécurité appliquées par l'outil :

- **Les identifiants et certificats clients sont uniquement envoyés via `https://`.** Une configuration contenant des identifiants avec une URL `http://` est refusée avant même qu'aucune connexion ne soit établie.
- **Les identifiants sont liés à la TSA configurée.** Une valeur `url_tsa` différente passée en ligne de commande est refusée, empêchant ainsi de rediriger par erreur des identifiants vers un autre serveur.
- **Les redirections HTTP ne sont jamais suivies**, de sorte qu'une TSA ne peut pas réacheminer vos informations de connexion vers un autre hôte.
- Les secrets ne sont jamais affichés, consignés dans les journaux ou inclus dans les messages d'erreur.

Conservez ce fichier hors du système de gestion de version (ajoutez-le à `.gitignore`) et restreignez l'accès à votre compte. Avec un mot de passe en clair, quiconque peut lire le fichier peut utiliser votre compte TSA.

### Fichiers de sortie

En cas de succès, WarcSigner écrit la réponse de la TSA à côté du manifeste, en remplaçant la dernière extension par `.tsr` :

- 📄 `20261010-134511_veydunet.com.manifest.json` (fichier horodaté)
- 🛡️ `20261010-134511_veydunet.com.manifest.tsr` (preuve)

Le fichier `.tsr` est le **`TimeStampResp` RFC 3161 non modifié** renvoyé par la TSA, ce qui permet à des outils standard de le vérifier indépendamment :

> openssl ts -verify -data 20261010-134511_veydunet.com.manifest.json -in 20261010-134511_veydunet.com.manifest.tsr -CAfile tsa-root.pem

Si le fichier `.tsr` existe déjà, WarcSigner s'arrête sans y toucher. Conservez ensemble le manifeste, le fichier `.tsr` et le certificat racine de la TSA.

### Pipeline d'exécution

1. **Hachage :** transmet le manifeste par flux (stream) via SHA-256.
2. **Requête :** construit une `TimeStampReq` RFC 3161 avec un nonce aléatoire et `certReq` activé, afin que la TSA renvoie son certificat de signature.
3. **Transmission :** requête HTTP POST avec `Content-Type: application/timestamp-query`, délai d'attente de 30 s, réponse plafonnée à 1 Mio, absence de redirection.
4. **Vérification de la réponse :** statut, empreinte du message, nonce et politique, absence de données de fin de chaîne, puis signature CMS par rapport au hachage du manifeste.
5. **Vérification du certificat :** EKU `id-kp-timeStamping`, validité à l'heure de l'horodatage, confiance de la chaîne (magasin du SE ou `--ca`), révocation si accessible. Une chaîne non sécurisée entraîne un échec, sauf si l'argument `--allow-untrusted` est spécifié.
6. **Enregistrement :** la réponse n'est écrite qu'à ce stade, et jamais par-dessus un fichier existant.

### Codes de sortie

| Code | Signification |
|---|---|
| `0` | Horodatage obtenu, vérifié et enregistré (avec `--allow-untrusted`, un avertissement indique que la confiance n'a pas pu être établie). |
| `1` | Tout type d'échec : arguments incorrects, erreur réseau, réponse rejetée, échec de vérification, chaîne non sécurisée, fichier de sortie existant. |

### Limitations

- L'outil ne vérifie pas si la TSA est un prestataire qualifié eIDAS.
- La vérification de la révocation est effectuée au mieux des possibilités : si aucune réponse CRL/OCSP ne peut être obtenue, l'exécution se poursuit avec un avertissement. Pour une validation à long terme, archivez les données de révocation (ou utilisez un format à long terme) en plus du fichier `.tsr`.
- Seuls l'authentification HTTP Basic et les certificats clients TLS sont pris en charge. Les TSA qui s'authentifient à l'aide d'une clé d'API dans un en-tête personnalisé, d'un jeton du porteur (bearer token) ou d'OAuth ne le sont pas.
- Les points de terminaison TSA en `http://` en clair sont courants pour la RFC 3161 (le jeton s'authentifie lui-même), mais l'échange en soi n'est pas chiffré ; privilégiez `https://` lorsque la TSA le propose.