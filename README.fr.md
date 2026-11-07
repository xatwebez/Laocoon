# Laocoön · Sonde de fenêtres

[中文](README.md) · [English](README.en.md) · [Русский](README.ru.md) · **Français** · [Español](README.es.md) · [العربية](README.ar.md) · [Deutsch](README.de.md) · [Português](README.pt.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [हिन्दी](README.hi.md) · [Italiano](README.it.md)

> Devant Troie, tout le monde voulait faire entrer ce cheval de bois dans la ville. Seul Laocoon refusa.
> Il enfonça sa lance dans son ventre — et entendit des armes s'entrechoquer à l'intérieur.
>
> Cet outil fait exactement la même chose : **pique une fenêtre, écoute ce qu'elle cache.**

Vous voyez une fenêtre que vous ne reconnaissez pas — peut-être quelque chose de douteux. Appuyez sur `F2`, amenez le réticule dessus, cliquez. Il déroule tout ce qu'on peut extraire de cette fenêtre et du processus qui la porte.

- Un seul fichier `Laocoon.exe`, environ **199 Ko**
- Aucune dépendance tierce — uniquement le .NET Framework 4.x fourni avec Windows (Win10 / Win11, rien à installer)
- Pas de réseau, pas d'envoi, pas d'écriture dans le registre, pas d'agent en arrière-plan (un seul `Laocoon.ini` à côté de l'exe, pour retenir la langue)
- Douze langues intégrées : **中文 / English / Русский / Français / Español / العربية / Deutsch / Português / 日本語 / 한국어 / हिन्दी / Italiano**

> Le nom de fichier reste `Laocoon.exe` en ASCII ; le titre de la fenêtre garde le tréma — `Laocoön`.
> Ainsi les chemins, les lignes de commande, les pièces jointes et la CI ne butent jamais sur des caractères non ASCII.

---

## Utilisation

1. Double-cliquez sur `Laocoon.exe` (pour le maximum d'informations : **clic droit → Exécuter en tant qu'administrateur**)
2. Appuyez sur `F2`, le curseur devient un réticule
3. Passez sur la fenêtre visée : un cadre bleu l'entoure et un petit HUD noir flotte à côté, affichant en direct classe / titre / PID / fenêtre parente
4. **Clic gauche** pour verrouiller et capturer ; **clic droit** ou **Échap** pour annuler
5. Le rapport complet s'affiche aussitôt : fenêtre, processus, fichier, signature, fenêtres masquées et contrôles enfants, tout au même endroit

La capture est unique : rien ne se rafraîchit à intervalle régulier, rien ne reste en arrière-plan à vous surveiller. Fermez la fenêtre et il ne reste rien — sauf le fichier que vous avez vous-même écrit avec « Enregistrer le rapport ».

## Langues de l'interface

**12 langues** intégrées, changeables à tout moment via la liste en bas à droite, **sans redémarrage** :

| Code | Langue | Titre de la fenêtre |
|---|---|---|
| `zh` | 中文 | Laocoön 拉奥孔 · 窗口探针 |
| `en` | English | Laocoön · Window Probe |
| `ru` | Русский | Laocoön · Зонд окон |
| `fr` | Français | Laocoön · Sonde de fenêtres |
| `es` | Español | Laocoön · Sonda de ventanas |
| `ar` | العربية | Laocoön · فاحص النوافذ |
| `de` | Deutsch | Laocoön · Fenster-Sonde |
| `pt` | Português | Laocoön · Sonda de janelas |
| `ja` | 日本語 | Laocoön · ウィンドウ探針 |
| `ko` | 한국어 | Laocoön · 창 프로브 |
| `hi` | हिन्दी | Laocoön · विंडो प्रोब |
| `it` | Italiano | Laocoön · Sonda finestre |

Pourquoi ces douze-là : les langues des cinq membres permanents du Conseil de sécurité de l'ONU comme base, plus l'**arabe** (la seule langue de travail de l'ONU absente de cette liste), l'**allemand / le portugais / le japonais / le coréen** (grandes économies et écosystèmes logiciels), l'**hindi** (le plus grand marché monolingue en population) et l'**italien** (culture européenne). Le turc, l'indonésien, le vietnamien et le polonais ne sont pas là — le point d'extension existe : ajouter une langue = ajouter une table dans `Lang.cs`.

Au premier lancement, la langue est choisie dans cet ordre, et le choix est écrit dans `lang=` du fichier `Laocoon.ini` :

```
--lang=xx en ligne de commande  >  lang=xx dans Laocoon.ini  >  langue du système  >  anglais
```

Autrement dit, un système chinois démarre en chinois, un système russe / français / espagnol / arabe / allemand / portugais / japonais / coréen / hindi / italien affiche la sienne, tout le reste tombe sur l'anglais (les variantes régionales comme `pt-BR`, `zh-Hant`, `ar-EG` sont reconnues). Pour figer la langue, écrivez `lang=ru` dans `Laocoon.ini`.

Ce qui est traduit ne s'arrête pas à l'interface : **le corps du rapport** aussi — libellés, titres de sections, notes heuristiques (« l'exécutable se trouve dans le dossier temporaire », « le processus a N autres fenêtres de premier niveau masquées ») sont générés dans la langue courante.

### Comment l'arabe (droite à gauche) est géré

L'arabe, ce n'est pas « remplacer la table de chaînes ». Trois pièges sont apparus aux tests :

1. **Interface miroir** : toute la fenêtre est retournée via `RightToLeftLayout`, la barre d'outils va de droite à gauche et le bouton principal se retrouve tout à droite.
2. **Le rapport n'est pas mis en miroir** : il est rempli de chemins, de hachages, de noms de classes. Le miroir inverse le début et la fin de chaque ligne, donc la zone de rapport reste de gauche à droite. Seule la **page d'accueil** (texte courant) est alignée à droite en arabe.
3. **Le deux-points saute à gauche de son libellé** : le moteur BiDi de GDI considère le deux-points de « libellé arabe + `:` + valeur latine » comme appartenant au segment RTL de droite et affiche `0 :المقبضx0001048E`. La parade : un LRM de largeur nulle (U+200E) de part et d'autre du deux-points pour forcer le sens, la même isolation étant appliquée aux arguments de formatage (dates, PID, noms de classes) — sinon une date devient `04:28:37 08-11-2026`. L'expérience comparative de toutes ces écritures est conservée dans le diagnostic `--biditest`, pour pouvoir la rejouer en cas de réécriture.

Le **devanagari** (hindi) n'a pas plus de largeur fixe que l'arabe : « compléter avec des espaces pour aligner » ne fait qu'empirer. Ces deux langues passent donc le rapport au format `libellé: valeur` — mieux vaut pas d'alignement qu'un faux alignement.
Arabe, hindi, japonais et coréen utilisent chacun la police fournie par Windows (Segoe UI / Nirmala UI / Yu Gothic UI / Malgun Gothic) ; si elle manque, le repli est automatique, sans carrés ni exception.

## Ce qui est collecté

| Catégorie | Contenu |
|---|---|
| Fenêtre | Handle, classe, titre, bits de style (décodés en noms `WS_*` / `WS_EX_*`), rectangle, zone cliente, visible / réduite, **toujours au premier plan**, **fenêtre d'outil**, **masquée par DWM**, fenêtre propriétaire |
| Contrôle touché | Jusqu'au contrôle enfant exact : classe, ID, texte, chemin depuis la fenêtre racine |
| Processus | PID / TID, chemin complet, **ligne de commande**, parent (nom + PID), architecture (x86 / x64 / WOW64), **élévation**, session, heure de démarrage |
| Fichier | Taille, création / modification, **MD5 / SHA1 / SHA256**, société, produit, description, version, copyright |
| Signature | Présence d'une signature intégrée, signataire, émetteur, validité, résultat de la chaîne |
| Modules | Chemins complets de toutes les DLL chargées par le processus |
| Fenêtres du processus | **Toutes** les fenêtres de premier niveau de ce processus, y compris masquées (marquées `[masquée]`) — la fenêtre de fond d'un logiciel indésirable ne peut pas s'y cacher |
| Arbre des contrôles | Tout l'arbre des contrôles enfants de la fenêtre visée : indentation, taille, ID, texte |
| Indices de suspicion | Heuristiques : dans Temp / Téléchargements / AppData, non signé, sans nom de société, au premier plan, nombre de fenêtres masquées, fichier créé depuis moins de 7 jours, etc. (**des indices, pas un verdict**) |

Le rapport entier est du texte brut : colonnes alignées, sections séparées — tout sélectionner pour copier, ou « Enregistrer le rapport » pour écrire un `.txt` en UTF-8. Pour vérifier un fichier plus loin, copiez le `sha256` vers le service de votre choix — cet outil ne fait que capturer en local, sans connexion ni envoi.

## Limites connues

- **Signature numérique** : seule la signature intégrée au PE est vérifiée. Les fichiers de Windows (`notepad.exe` par exemple) sont signés par catalogue, d'où la mention « aucune signature intégrée » — c'est normal.
- Sans droits d'administrateur, les processus élevés ou protégés ne livrent ni chemin, ni modules, ni ligne de commande (« accès refusé »), les autres champs restant disponibles.
- `WM_GETTEXT` pour le texte des contrôles applique un délai de 60 ms sur les fenêtres qui ne répondent pas (`SMTO_ABORTIFHUNG`), pour ne jamais figer l'interface — d'où l'absence de texte sur de rares fenêtres bloquées.
- L'arbre des contrôles est plafonné à 240 entrées, la liste des modules à 400 ; au-delà, une mention de troncature apparaît.
- Le hachage s'arrête à 512 Mo, au-delà il est ignoré (pour éviter une attente interminable).
- La langue suit le système ; hors des douze, retour à l'anglais. Les traductions sont écrites à la main, et les termes propres à Windows (`WS_*` / `WS_EX_*`, `DWM Cloaked`) restent volontairement en anglais pour que chaque langue n'invente pas son vocabulaire.
- L'interface arabe est en miroir, mais **le rapport reste toujours de gauche à droite** — ce sont des données techniques, le miroir les rend illisibles. C'est un choix, pas une traduction inachevée.

## Journal des versions

- **1.4.2** — Les trois fichiers de langues réunis en un seul `Lang.cs` ; simple rangement, aucun changement d'interface ni de texte
- **1.4.1** — Corrigé : la liste des langues perdait le focus en passant à l'arabe ou en revenant, ce qui cassait le changement au clavier sans rien signaler ; le focus revient dans la liste
- **1.4** — Nouvelle icône pixel art : 8 tailles de 16 à 256 intégrées à l'exe, identiques dans la barre de titre, la barre des tâches et l'explorateur
- **1.3** — sortie JSON et onglet JSON supprimés ; rapport texte uniquement
- **1.2** — suppression de la vérification en ligne (modèle d'URL + « ouvrir dans le navigateur »), collecte locale uniquement ; `Laocoon.ini` ne retient que la langue ; README en 12 langues
- **1.1** — interface et rapport en 12 langues ; traitement RTL dédié à l'arabe
- **1.0** — première version : visée, capture, rapport

## Recompiler soi-même

Il faut le SDK .NET (8 ou plus récent). Double-cliquez sur `build.bat`, ou :

```bat
dotnet build src\Laocoon.csproj -c Release
```

Le résultat est `src\bin\Release\net48\Laocoon.exe`.

> `build.bat` pointe volontairement `TEMP` vers un dossier local `.tmp` : sur certaines machines le dossier temporaire système n'est pas inscriptible et la compilation échoue immédiatement.

## Sources

- `src/Laocoon.cs` — le programme, environ 2 225 lignes, uniquement du P/Invoke Win32, aucun paquet NuGet
- `src/Lang.cs` — cœur des langues (énumération, détection, police, largeur, recherche, repli) et les douze tables dans un seul fichier, environ 2 250 lignes
- `src/Laocoon.ico` — l'icône pixel art elle-même (8 tailles, 16 à 256), générée depuis une grille 16×16 par `tools/make_icon.py`

Chaque table compte 124 clés, toutes compilées dans l'exe. Pas de `.resources`, pas de satellite assembly : sur n'importe quelle machine Win10/11 tout fonctionne sans « pack de langue manquant, repli sur l'anglais ».

Points d'entrée principaux :
- `Probe.HitTest` — parcourt lui-même les fenêtres de premier niveau dans l'ordre z (pas `WindowFromPoint`, pour ne jamais toucher sa propre surcouche)
- `Probe.DeepestAt` — descend avec `ChildWindowFromPointEx` jusqu'au contrôle le plus interne
- `Probe.Collect` — rassemble fenêtre / processus / fichier / signature / modules / fenêtres sœurs / arbre des contrôles
- `FrameForm` / `HudForm` — le cadre bleu transparent et le HUD qui suit pendant la visée (tous deux `WS_EX_TRANSPARENT | WS_EX_NOACTIVATE`, ils ne captent jamais la souris)
- `L.T` / `L.F` — recherche de chaîne et formatage à trous ; une clé manquante retombe sur l'anglais au lieu de rester vide

## Options de diagnostic (pour travailler sur le code)

```
Laocoon.exe --selftest out.txt     capturer la fenêtre de console, imprimer le rapport, l'écrire dans un fichier
Laocoon.exe --aimtest              auto-test du hit-testing et de la surcouche de visée
Laocoon.exe --i18ncheck            vérifier les 12 langues : clés, largeur de colonne, polices, sens
Laocoon.exe --langswitch           parcourir toutes les langues et vérifier interface + rapport
Laocoon.exe --focustest f.log [shot.png]  enchaîner les 12 langues et vérifier que la liste garde le focus
Laocoon.exe --biditest out.png     comparatif de 8 mises en page bidi (arabe + données latines)
Laocoon.exe --lang=ru              démarrer dans une langue donnée (non écrit dans l'ini)
Laocoon.exe --uishot out.png --lang=ar            rendre l'interface en PNG
Laocoon.exe --uishot out.png --lang=ar --welcome  page d'accueil seulement
Laocoon.exe --uishot out.png --lang=ar --screen   capture d'écran (DrawToBitmap simule une sélection en RTL)
```

Combiné à `--lang=`, `--uishot` rend les douze interfaces d'un coup pour un contrôle visuel.
