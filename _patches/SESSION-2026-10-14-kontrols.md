# Session Kontrols — 14 octobre 2026 — passation technique

Document de reprise, destiné à **une future session Claude Code** et au **développeur de Kontrols** qui dispose du dépôt amont. Il décrit trois sujets traités indépendamment, l'état exact de chacun, et la méthode de diagnostic qui a fini par trouver la cause racine — ainsi que les erreurs commises en chemin, parce qu'elles ont coûté une soirée et qu'elles sont évitables.

> ## ⚠ Mise à jour du 15/10
>
> Le diagnostic du sujet C (§5) et la conclusion du §6 sur MadMapper sont **corrigés** par `_patches/2026-10-15-midi-silent-loss/RAPPORT.md`. Cause réelle : le contrôleur tombe du bus USB (Kernel-PnP 1010, 17 fois en 10 jours, tous sur le même hub) ; `MidiHub` garde le port mort ; le fermer (Reconnect, Play, recompilation) libère deux fois les buffers d'RtMidi. Le `SafeDispose` du sujet C n'empêche pas ce crash. Windows MIDI Services (`midisrv`) est actif : les ports acceptent plusieurs clients, la sonde `midi-probe.ps1` ne prouve rien.

---

## 1. Environnement

| | |
|---|---|
| Machine | Windows 11 Home 26200, projet `toxxic` (HDRP, VR, Spout → MadMapper) |
| Unity | 6000.3.10f1 |
| Kontrols | `com.blindspot.kontrols` 0.5.1, dépendance **locale** `file:C:/Users/chamb/Documents/GitHub/NewVersions/Kontrols-0.5.1/Kontrols-0.5.1` (donc mutable, et **hors** du dépôt `toxxic`) |
| Presets | `com.blindspot.presets` 0.3.0, même mécanique (`.../Presets-0.3.0/Presets-0.3.0`) |
| RtMidi | `jp.keijiro.rtmidi` 2.2.0, dans `Library/PackageCache` (immuable, régénéré) |
| Fenêtre du projet | `Assets/_scripts/Kontrols_interface/kontrolsToxxic01.cs` — onglet `TOXXIC`, 3 colonnes : `[Column]`, `[Column(1.4f)]`, `[Column]` |
| Matériel MIDI | Roto-Control (knobs motorisés) + nanoKONTROL2, **partagés avec MadMapper** qui tourne en parallèle |

Patch antérieur, non traité ici mais utile au contexte : `_patches/2026-10-13-editor-focus-lag/` (résolution de bindings scopée par scène, Kontrols + Presets).

---

## 2. Inventaire des livrables

```
_patches/
├── SESSION-2026-10-14-kontrols.md          ← ce document
├── 2026-10-14-column-resize/               [A] feature, appliqué, test souris NON FAIT
│   ├── RAPPORT.md
│   ├── 01-kontrols-0.5.1--mouse-resizable-columns.patch
│   └── 02-kontrols-0.5.1--narrow-column-control-metrics.patch
├── 2026-10-14-midi-port-conflict/          [B] RETIRÉ — régression non isolée
│   ├── RAPPORT.md                          (bandeau d'avertissement en tête)
│   └── kontrols-0.5.1--midi-port-refused.patch
└── 2026-10-14-midi-close-crash/            [C] correctif critique, appliqué
    ├── RAPPORT.md
    ├── kontrols-0.5.1--close-port-before-free.patch
    └── midi-probe.ps1                      ← sonde WinMM hors Unity
```

| Sujet | Fichiers Kontrols touchés | Statut dans le package local | Indépendance |
|---|---|---|---|
| **A** colonnes | `KontrolsWindow.cs`, **nouveau** `KontrolsWindow.Layout.cs`, `KontrolsWindow.Midi.cs`, `README.md`, `CHANGELOG.md` | **appliqué** | 01 puis 02 ; indépendant de C |
| **B** port refusé | `MidiHub.cs`, `KontrolsWindow.Midi.cs` | **reverté** (package propre) | s'applique sur vierge comme sur A |
| **C** crash fermeture | `MidiHub.cs` | **appliqué** | indépendant de A et B |

A et C sont tous deux dans le package au moment où ce document est écrit. B ne l'est pas.

Chaque `.patch` a été vérifié par `patch -p1` sur une copie vierge, résultat comparé **à l'octet près** (`cmp`) à l'état de référence.

---

## 3. Sujet A — bande de colonnes redimensionnable à la souris

**Demande** : régler la largeur des colonnes d'un onglet à la souris, plutôt que par `[Column(weight)]` dans le code. Puis, en cours de route : pouvoir réduire davantage la première colonne, et régler la largeur de la dernière colonne (celle qui borde la zone vide à droite).

**Patch 01** — strip de 5 px à droite de chaque colonne, drag qui déplace la largeur entre les deux slots qu'il sépare (somme préservée → les autres colonnes ne bougent pas), double-clic / clic droit pour revenir aux attributs. Une bande a **un slot de plus que de colonnes** : le dernier est le vide à droite, ce qui permet de ne pas remplir toute la fenêtre. Persistance par fenêtre et par onglet dans `EditorPrefs` (`Kontrols_<ns>_colw_<tab>`, CSV de poids), rejetée si le nombre de colonnes a changé. `MinColumnWidth` devient `protected virtual` (120 px).

**Patch 02** — le vrai plancher n'était pas le clamp mais le **contenu** : une ligne de contrôle est `[label 120][contrôle][bouton CC 72][poignée LFO 30]`, toutes largeurs fixes, donc elle cesse de rétrécir vers 300 px et déborde sur la colonne voisine. Le label (120 → 48) et le bouton CC (72 → 36) suivent maintenant la largeur de colonne, et les sliders portent un `MinWidth(40)` explicite. Au-dessus de ~355 px, rendu identique au pixel près.

**Décisions à connaître** : les poids plutôt que des pixels (une bande réglée reste proportionnelle au redimensionnement) ; un drag ne touche que deux slots ; les strips sont *prélevés* sur la largeur partagée, pas ajoutés ; les splitters sont traités **après** la bande, parce que leur hauteur n'est connue qu'une fois le groupe horizontal fermé (`GUILayoutUtility.GetLastRect()`), et jamais pendant la passe `EventType.Layout`.

**Statut** : compile proprement, patches vérifiés. **Le test souris n'a jamais été fait** — ni par moi, ni confirmé par l'utilisateur ; la session a basculé sur l'incident MIDI avant. Protocole dans le `RAPPORT.md` du dossier.

---

## 4. Sujet B — port MIDI refusé et liste d'exclusion — **RETIRÉ**

**Intention** : `MidiIn.OpenPort` est `void` ; RtMidi signale un refus via le couple `IsOk`/`Error` lu en direct, que `MidiHub` ne relisait jamais. Un port refusé était donc enregistré comme ouvert → bandeau vert, zéro message, et `PollNewPorts` ne le réessayait plus jamais (son nom était déjà dans `_portNames`). Le patch ajoutait : contrôle de `IsOk` après ouverture, liste `busy` affichée dans le bandeau, balayage de tous les index, cadencement de `TryOpenPorts`, puis une **liste d'exclusion par nom de port** persistée (`MidiHub_IgnoredPorts`) avec une case par entrée dans l'onglet MIDI Map.

**Pourquoi il est retiré** : appliqué à 18h25, il a cassé la réception MIDI. Après redémarrage de l'éditeur, le port s'ouvrait, s'affichait `listening`, aucune exception, aucun port signalé occupé — et rien n'arrivait. Reverté à 18h55, package restauré à l'octet près.

**Ce qui est su** : la régression n'est pas isolée. Suspect principal — la suppression du retour anticipé `portCount <= _ports.Count` dans `PollNewPorts`, qui fait créer/détruire une instance `MidiIn` par port **toutes les 3 s** pendant qu'un port est ouvert, ce que le code d'origine ne faisait jamais en régime établi. **Non démontré.**

**Attention au contexte** : ce patch a été écrit sur un diagnostic (« MadMapper tient le port ») qui s'est révélé **faux** — voir §6. L'analyse du défaut `IsOk` reste exacte et vaut d'être reprise, mais l'incident qui l'a motivée avait une autre cause (sujet C). Si on le reprend : repartir du strict minimum (contrôle d'`IsOk` + journalisation), sans toucher au cadencement ni au balayage, et valider **une modification à la fois** avec un contrôleur réel.

---

## 5. Sujet C — crash de l'éditeur à la fermeture des ports — **cause racine de la journée**

Trace complète dans `Editor.log` :

```
Reloading assemblies for play mode.
Kontrols.MidiHub:Cleanup ()                             MidiHub.cs:453
RtMidi.MidiIn:FreeDeviceHandle (intptr)                 MidiIn.cs:32
MidiInWinMM::openPort: error closing Windows MM MIDI input port (midiInUnprepareHeader).
terminate called after throwing an instance of 'rt::midi::RtMidiError'
```

Un domain reload (recompilation, ou entrée en Play) déclenche `Cleanup()` → `port.Dispose()` → `rtmidi_in_free` → `delete` du `RtMidiIn` → le destructeur appelle `closePort()` → le backend WinMM échoue sur `midiInUnprepareHeader` et **lève**. Or `rtmidi_in_free` n'a pas de `try/catch`, contrairement à `rtmidi_close_port` qui, lui, attrape `RtMidiError` et la convertit en drapeau d'erreur. Une exception qui s'échappe d'un destructeur à travers l'API C atteint `std::terminate` : le processus meurt avant tout log Unity.

**Correctif** : fermer avant de libérer.

```csharp
static void SafeDispose(MidiBase port)
{
    if (port == null) return;
    try { port.ClosePort(); } catch { }   // rtmidi_close_port attrape
    try { port.Dispose();   } catch { }   // le destructeur n'a plus rien à fermer
}
```

dans `Cleanup()` et `Reconnect()`. Deux hunks, aucune API modifiée.

**Pourquoi cela explique aussi la perte « silencieuse »** : quand le même échec de fermeture n'abat pas le processus, il laisse le périphérique WinMM à moitié fermé ; le domaine suivant rouvre le port sans erreur apparente et ne reçoit plus rien, sans que rien ne soit journalisé — tous les chemins d'échec d'ouverture de `MidiHub` étant muets. D'où le cycle vécu toute la journée : **recompiler → perdre le contrôleur → redémarrer l'éditeur → le retrouver → recompiler…**

**Conséquence de méthode** : toute édition de script pendant que la surface MIDI tourne coûtait le contrôleur. Les diagnostics intermédiaires de la session en ont été faussés, y compris les miens.

**Statut** : appliqué, compile proprement, patch vérifié. **Non validé à l'usage** : le défaut est intermittent (il dépend de l'état des buffers du pilote à la fermeture), seule la répétition des allers-retours Play / recompilation le confirmera.

**Défaut de fond, en amont** : `rtmidi_in_free` devrait attraper, ou le backend WinMM ne devrait pas échouer sur `midiInUnprepareHeader` après `midiInReset`. À remonter à `jp.keijiro.rtmidi`. Tout consommateur du wrapper qui appelle `Dispose` sans fermer d'abord reste exposé.

---

## 6. Hypothèses explorées et écartées (avec la mesure qui les a tuées)

| Hypothèse | Mesure | Verdict |
|---|---|---|
| MIDI Learn resté armé | `[MidiHub] Learn CC` en fin de log, puis `learn=-` dans la trace | vraie une fois (un clic involontaire sur le bouton CC), sans rapport avec la panne de fond |
| Conflit de port avec MadMapper | `midi-probe.ps1` : les deux entrées s'ouvrent depuis un processus tiers | **écartée** — personne ne tenait les ports, et l'éditeur n'en avait aucun d'ouvert |
| Deuxième éditeur Unity résiduel | `Win32_Process` : le second processus est `unity.exe serve` du Hub, 15 Mo | **écartée** — j'avais conclu trop vite sur le nom |
| Boucle éditeur étranglée hors focus | `Interaction Mode` déjà sur `No Throttling` | **écartée** |
| Périphériques disparus | `Get-PnpDevice -Class Media` : Roto-Control `OK` | **écartée** |
| Mappings écrasés | lecture des 22 clés `MidiHub_CC_*` dans le registre | **écartée** — intactes |
| Messages non distribués | trace `[DIAG] rx status=0xB2 d1=1 …` avec `ccSubs=3` | **écartée** — ils arrivaient et étaient distribués |

---

## 7. Outils et méthodes qui ont servi (à réutiliser)

### 7.1 Compiler Kontrols sans ouvrir Unity

Le plus utile de la session. Unity écrit la ligne de compilation exacte de chaque assembly dans une *response file* Bee ; on la rejoue avec le Roslyn livré par Unity :

```bash
cd /c/Users/chamb/Documents/GitHub/toxxic
S=<scratchpad>
sed -e 's#^-out:.*#-out:"<scratchpad_win>/check.dll"#' \
    -e 's#^-refout:.*##' -e '/^-analyzer:/d' \
    Library/Bee/artifacts/1900b0aE.dag/Kontrols.Editor.rsp > "$S/check.rsp"
dotnet "/c/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Data/DotNetSdkRoslyn/csc.dll" "@$S/check.rsp"
```

Sortie vide = 0 erreur, 0 warning. Le hash du dossier `.dag` change quand Unity régénère ses artefacts : le relire plutôt que le supposer. La rsp liste les sources en absolu, donc elle inclut automatiquement tout nouveau fichier **après** qu'Unity l'a vu ; avant, l'ajouter à la main en fin de fichier (en vérifiant qu'il y a bien un saut de ligne avant).

### 7.2 Lire ce que fait l'éditeur

- `C:\Users\chamb\AppData\Local\Unity\Editor\Editor.log` (session courante) et `Editor-prev.log` (précédente, **c'est là qu'est le crash**).
- Repères utiles : `Reloading assemblies`, `Begin MonoManager`, `error CS`, `terminate called`, `Scanning for USB devices`.
- Les logs Unity n'ont pas d'horodatage : se repérer par **position relative** des lignes (`awk -F: '$1 > <ligne>'`), pas par heure.

### 7.3 Sonde MIDI hors Unity

`2026-10-14-midi-close-crash/midi-probe.ps1` — P/Invoke `winmm.dll`, liste les entrées et tente de les ouvrir. Une entrée MIDI est **exclusive** sous Windows, donc `OK` prouve que personne ne la tient, pas même l'éditeur. C'est l'outil qui a démoli l'hypothèse du conflit applicatif.

### 7.4 EditorPrefs

Sous Windows, `HKCU\Software\Unity Technologies\Unity Editor 5.x`, noms suffixés d'un hash (`_h1234567890`).

```powershell
$k = Get-Item 'HKCU:\Software\Unity Technologies\Unity Editor 5.x'
$k.GetValueNames() | Where-Object { $_ -like 'MidiHub_CC_*' }
```

Clés Kontrols : `Kontrols_<ns>_val_<id>`, `Kontrols_<ns>_ref_<id>_*`, `MidiHub_CC_<ns>_<id>` (valeur = `(channel << 8) | cc`), et, avec le patch A, `Kontrols_<ns>_colw_<tab>`.

### 7.5 Instrumentation temporaire

Marquer `// [DIAG]`, **copier le fichier d'origine dans le scratchpad avant**, et restaurer depuis cette copie plutôt que de tenter de défaire les ajouts. Un battement périodique (toutes les 5 s) vaut mieux qu'une trace plafonnée : c'est l'instant où ça s'arrête qui compte, pas les premiers messages.

---

## 8. Pièges de cet environnement

1. **`MidiHub.cs` n'est pas de l'UTF-8 propre** : il contient des `U+FFFD` à la place des accents de ses commentaires français (dégâts préexistants, en amont). Le lire/écrire en **latin-1** (aller-retour fidèle octet par octet) et n'y écrire que de l'ASCII. `KontrolsWindow.Midi.cs` et `KontrolsWindow.cs`, eux, sont de l'UTF-8 valide.
2. **Ne jamais écrire un fichier avec `open(p,"w")` suivi d'un `encode` qui peut lever** : l'ouverture tronque avant l'erreur. **Un fichier a été vidé ainsi pendant cette session.** Encoder d'abord, écrire dans un `.tmp`, puis `os.replace`.
3. **Le heredoc du shell mange les antislashs** : `\\n` devient un saut de ligne réel au milieu d'un littéral C#. Écrire les scripts Python dans un fichier (outil Write) puis les exécuter, plutôt qu'en heredoc.
4. **Recompiler pendant que la surface MIDI tourne coûtait le contrôleur** (sujet C). Prévenir avant de toucher au package, et grouper les éditions.
5. **`diff` renvoie 1 quand il y a des différences** : un `{ diff …; } > x && cat …` ne s'exécute pas. Utiliser `;`.
6. Le package Kontrols **n'est pas un dépôt git** : pas de `git diff`, il faut garder des copies de référence dans le scratchpad pour produire les patches.

---

## 9. Format des patches (convention du projet)

En-tête façon courriel (`Subject:`, corps en prose, `---`), puis diff unifié avec préfixes `a/` et `b/` **relatifs à la racine du package**, pas du projet Unity. Un `RAPPORT.md` en français accompagne chaque dossier : symptôme, cause avec extraits de code, décisions, fichiers touchés, vérifications, limites connues. Convention héritée de `_patches/2026-10-13-editor-focus-lag/`.

---

## 10. Ce qui reste à faire

1. **Valider C** par la répétition : entrer/sortir du mode Play et forcer des recompilations, contrôleur branché. C'est le seul point bloquant pour l'utilisateur.
2. **Tester A à la souris** (protocole dans son `RAPPORT.md`) : les trois strips, la réduction de la colonne 1, le strip de droite qui ouvre la zone vide, le double-clic de remise à zéro, la persistance après recompilation.
3. **Reprendre B** à partir de zéro si le sujet revient : contrôle d'`IsOk` après ouverture **seul**, validé avec un contrôleur, avant d'envisager quoi que ce soit d'autre.
4. **Remonter en amont** à `jp.keijiro.rtmidi` le défaut `rtmidi_in_free` sans `try/catch` (sujet C, §5).
5. **Nettoyage d'encodage** de `MidiHub.cs` (les `U+FFFD`), en changement isolé pour ne pas polluer un patch fonctionnel.

---

## 11. Honnêteté sur la conduite de cette session

Trois erreurs valent d'être écrites, parce qu'elles ont orienté le travail dans le vide :

- **Un correctif de 300 lignes écrit sur un diagnostic non vérifié.** L'hypothèse « MadMapper tient le port » était plausible, cohérente avec la documentation de `Reconnect`, et fausse. Une sonde de quinze lignes (§7.3) l'aurait écartée en deux minutes — elle a été écrite trois heures trop tard.
- **« Compile proprement » et « s'applique à l'octet près » présentés comme des vérifications.** Ce sont des conditions nécessaires. Le seul test qui comptait — un contrôleur qui bouge, une souris qui tire un strip — n'a jamais été fait avant livraison.
- **Un fichier tronqué** par un script dont l'encodage a levé après l'ouverture en écriture (§8.2). Restauré depuis une copie, sans conséquence, mais le garde-fou (encoder avant d'ouvrir) aurait dû être là dès le départ.

La leçon opérationnelle pour la suite : sur ce projet, **mesurer avant de corriger**, et livrer un changement à la fois quand du matériel est dans la boucle.
