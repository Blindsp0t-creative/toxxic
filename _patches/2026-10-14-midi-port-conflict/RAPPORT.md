# Port MIDI refusé par un autre logiciel — Kontrols 0.5.1

**Date** : 14 octobre 2026
**Projet d'origine** : `toxxic` (Unity 6000.3.10f1, HDRP, Windows 11), branche `master`
**Package concerné** :

| Package | Version | Chemin local sur la machine d'origine |
|---|---|---|
| `com.blindspot.kontrols` | 0.5.1 | `C:/Users/chamb/Documents/GitHub/NewVersions/Kontrols-0.5.1/Kontrols-0.5.1` |

> ## ⚠ PATCH RETIRÉ — NE PAS APPLIQUER EN L'ÉTAT
>
> Appliqué le 14 octobre 2026 à 18h25 sur la machine d'origine, il a **cassé la
> réception MIDI** : après redémarrage de l'éditeur, le port s'ouvrait et
> s'affichait `listening`, aucune exception n'était levée, aucun port n'était
> signalé occupé — et aucun message n'arrivait. Le patch a été **reverté** à
> 18h55 (`patch -R -p1`), état d'origine restauré à l'octet près.
>
> La régression n'est pas isolée. Suspect principal : la suppression du retour
> anticipé `portCount <= _ports.Count` dans `PollNewPorts` (§3.2), qui fait
> créer et détruire une instance `MidiIn` par port **toutes les 3 s** via
> `ReadPortNames` (§3.4) alors qu'un port est ouvert — ce que le code d'origine
> ne faisait jamais en régime établi. Non démontré.
>
> Le diagnostic qui trancherait : réappliquer, cliquer **Reconnect**, bouger un
> potard aussitôt. S'il répond puis meurt en quelques secondes, c'est bien le
> poll ; s'il ne répond jamais, c'est le chemin d'ouverture.
>
> Tout ce qui suit décrit l'intention du patch, pas un état validé.

**Fichiers livrés**

```
_patches/2026-10-14-midi-port-conflict/
├── RAPPORT.md                                  ← ce document
└── kontrols-0.5.1--midi-port-refused.patch
```

Ce patch est **indépendant** de `_patches/2026-10-14-column-resize/` : vérifié, il s'applique proprement sur le 0.5.1 vierge comme sur le 0.5.1 déjà patché « colonnes », dans n'importe quel ordre. Il ne touche pas `CHANGELOG.md` pour cette raison ; l'entrée proposée est en fin de document.

---

## 1. Incident

Machine de show : l'éditeur Unity et **MadMapper** tournent côte à côte, avec deux contrôleurs — un Roto-Control et un nanoKONTROL2. En cours de session, la surface Kontrols cesse de réagir au Roto-Control : les boutons n'ont plus d'effet, alors que le bandeau de l'onglet MIDI Map affiche toujours `● MIDI  Roto-Control` en vert.

Diagnostic sur place : `Reconnect` ne suffisait pas, et seul un redémarrage complet d'Unity a ramené le contrôleur.

## 2. Cause

Sous Windows, une **entrée** MIDI est exclusive : `midiInOpen` échoue (`MMSYSERR_ALLOCATED`) si un autre processus détient déjà le périphérique. RtMidi — que Kontrols utilise via `jp.keijiro.rtmidi` — passe par WinMM, et remonte l'échec non pas par une valeur de retour mais par le couple `IsOk` / `Error`, lus en direct dans la structure native (`MidiBase.cs:42-43`) :

```csharp
public void OpenPort(int portNumber = 0, string portName = "RtMidi")
  => _OpenPort(this, (uint)portNumber, portName);       // void
```

Or `MidiHub.TryOpenPorts` ne testait `IsOk` qu'**avant** l'ouverture, et enregistrait le port quoi qu'il arrive :

```csharp
MidiIn port = MidiIn.Create();
if (port == null || !port.IsOk) continue;     // teste l'objet, pas l'ouverture

port.MessageReceived = OnMessageReceived;
port.OpenPort(i, name);                       // peut échouer silencieusement

_ports.Add(port);
_portNames.Add(name);                         // ← le nom entre dans la liste
```

D'où la chaîne complète :

1. MadMapper détient le Roto-Control ; l'ouverture côté Unity est refusée.
2. Le port refusé est quand même ajouté à `_ports` / `_portNames` → `IsConnected` est vrai, le bandeau est vert, le nom s'affiche.
3. Aucun message n'arrive jamais : le callback natif n'a pas été enregistré.
4. `PollNewPorts` saute tout nom déjà présent dans `_portNames` → **plus aucune tentative**, même après que MadMapper a relâché le port.
5. Seul un domain reload (`Cleanup` vide les listes) ou un redémarrage d'Unity permettait d'en sortir — et encore, à condition que l'autre logiciel ait lâché entre-temps.

Un second défaut aggravait le point 4 :

```csharp
if (portCount <= _ports.Count) return;
for (int i = _ports.Count; i < portCount; i++)
```

`PollNewPorts` ne regardait que les index **au-delà** du nombre de ports ouverts, en supposant que les ports ouverts occupent les premiers index. Un port refusé n'étant pas dans `_ports`, la boucle ne repassait jamais dessus.

Et une cause de fond derrière les deux : Kontrols n'a aucune sélection de port, `TryOpenPorts` ouvre **tous** les ports d'entrée disponibles. Sur une machine à deux logiciels et deux contrôleurs, les deux se disputent donc chaque périphérique, et le premier démarré gagne.

## 3. Correctif

### 3.1 Tester le refus (In et Out)

Après `OpenPort`, `IsOk` est relu. Un In refusé est enregistré dans `_busyPorts`, journalisé **une fois** avec le message du driver, `Dispose()`, et **laissé hors** de `_ports` / `_portNames`. Résultat immédiat : `IsConnected` dit la vérité, et le retry a de nouveau prise sur ce port.

L'Out reçoit le même test : un Out refusé ne coûte que le retour moteur des knobs, et l'avertissement « no Out » existant couvre déjà le cas, donc il est simplement libéré sans bruit supplémentaire.

### 3.2 Le retry balaie tous les index

```csharp
for (int i = 0; i < portCount; i++)
```

Le filtrage par nom (`_portNames.Contains(name)`) suffit à sauter les ports déjà ouverts, et reste robuste si l'ordre des index change (débranchement/rebranchement). La condition de sortie anticipée `portCount <= _ports.Count` disparaît : avec des ports refusés, elle était fausse par construction.

### 3.3 `TryOpenPorts` passe sous cadence

Conséquence directe de 3.1 : tant qu'un port est absent **ou** occupé, `TryOpenPorts` reste abonné à `EditorApplication.update`, donc appelé à chaque tick. Sonder le driver à chaque tick coûterait des frames dans un éditeur au premier plan — exactement le genre de coût éditeur documenté dans `_patches/2026-10-13-editor-focus-lag/RAPPORT.md`. La méthode adopte donc la cadence de 3 s déjà utilisée par le poll (`OPEN_RETRY_S`, une seule constante pour les deux).

Le champ est initialisé à `OPEN_RETRY_S` pour que la toute première tentative parte immédiatement, et `Reconnect()` le réarme de la même façon pour tenir sa promesse de rouvrir « au tick suivant ».

### 3.4 Une seule passe de noms

`ReadPortNames(portCount)` lit tous les noms en une passe, met à jour `_availablePorts` et notifie l'UI quand l'ensemble change. Les deux chemins d'ouverture la partagent, là où chacun créait auparavant un `MidiIn` par index juste pour lire un nom (deux `MidiIn.Create()` par index et par passage). C'est aussi ce qui permet à l'UI de lister un port **qui n'est pas ouvert** — condition nécessaire pour pouvoir l'exclure.

### 3.5 Le dire dans l'UI

`BusyPortNames` / `AnyPortBusy` / `BusyPortError` sont exposés, et le bandeau de l'onglet MIDI Map affiche une ligne orange `busy : <ports>` avec un tooltip qui explique l'exclusivité Windows. C'est le seul cas où « no controller detected » serait un mensonge : le contrôleur est là, il est simplement pris.

### 3.6 Liste d'exclusion par nom de port

Survivre au conflit ne suffit pas : encore faut-il pouvoir l'éviter. `SetPortIgnored(name, bool)` réserve une entrée à l'autre logiciel.

- **Persistée** sous `MidiHub_IgnoredPorts` (EditorPrefs), à l'échelle de l'éditeur comme les autres clés `MidiHub_*` : le conflit est entre deux applications, pas entre deux projets. Séparateur `'\n'`, qui ne peut pas apparaître dans un nom de port (contrairement à la virgule ou à l'espace).
- **Exclusion immédiate** : cocher « ignore » ferme le port sur-le-champ (`ClosePort`), pour que l'autre logiciel puisse le prendre sans attendre un redémarrage. Décocher réarme les deux timers de retry — `_openTimer` **et** `_pollTimer`, parce que l'un ou l'autre est abonné selon qu'un premier port a déjà été ouvert — et le port est repris au tick suivant.
- **Jamais ouverte, jamais signalée occupée** : une entrée ignorée sort des deux boucles avant la tentative d'ouverture, et son éventuel état `busy` est effacé, puisque plus rien n'essaie de la prendre.
- **`ClosePort(name)`** ferme un In **et le port Out qui l'accompagne**. Ce couplage n'était suivi nulle part : `_outPorts` était une liste parallèle sans correspondance de nom. `_outPortNames` l'ajoute, ce qui évite de continuer à envoyer du feedback (donc à bouger les LED et les knobs motorisés) sur un contrôleur qu'on vient de rendre à l'autre logiciel.
- **UI** : un repli `Inputs (n)` sous le bandeau, une ligne par entrée exposée par le driver, avec une case « écouter / laisser » et son état (`listening` / `busy` / `ignored`). Replié par défaut : ça ne concerne que les machines qui partagent des contrôleurs.

Les mappings d'un port ignoré ne sont pas touchés : ils dorment et reprennent tels quels si on le réactive.

### 3.7 Ce qui change à l'usage

Avant : contrôleur muet, bandeau vert, récupération par redémarrage d'Unity.
Après : bandeau rouge + `busy : Roto-Control`, un avertissement en console avec le message du driver, et **récupération automatique dans les 3 s** dès que l'autre logiciel lâche le port. Et le contrôleur destiné à l'autre logiciel se décoche une fois pour toutes.

## 4. Fichiers touchés

| Fichier | Nature |
|---|---|
| `Editor/MidiHub.cs` | `_busyPorts`, `_ignoredPorts`, `_availablePorts`, `_outPortNames`, `OPEN_RETRY_S` ; test `IsOk` après `OpenPort` (In et Out) ; balayage de tous les index ; cadence de `TryOpenPorts` ; `ReadPortNames`, `ClosePort`, `NoteBusyPort`, `ClearBusyPort`, `SetPortIgnored`, `Load/SaveIgnoredPorts` ; `Cleanup` et `Reconnect` étendus |
| `Editor/KontrolsWindow.Midi.cs` | ligne `busy :` dans `DrawMidiConnectionStatus` ; `DrawMidiPortList` (repli + case par entrée) appelé depuis `DrawMidiMapTab` |

Surface publique : uniquement des ajouts — `BusyPortNames`, `AnyPortBusy`, `BusyPortError`, `AvailablePortNames`, `IsPortOpen`, `IsPortBusy`, `IsPortIgnored`, `SetPortIgnored`. Aucune signature modifiée, aucune migration ; sans exclusion déclarée, le comportement est celui d'avant, en plus honnête.

## 5. Vérification

- **Compilation** : `Kontrols.Editor` recompilée hors éditeur avec le Roslyn d'Unity 6000.3.10f1 et la response file de Bee du projet : **0 erreur, 0 warning**.
- **Patch** : `patch -p1` sur une copie de l'état courant reproduit les deux fichiers **à l'octet près** ; le même patch s'applique proprement sur le 0.5.1 **vierge**, ce qui garantit l'indépendance vis-à-vis du patch « colonnes ».
- **Test manuel attendu** :
  1. ouvrir le Roto-Control dans MadMapper, puis (re)démarrer l'éditeur → bandeau rouge, `busy : Roto-Control`, avertissement console ;
  2. décocher l'entrée dans MadMapper → la surface reprend la main **seule** en moins de 3 s ;
  3. dans `Inputs`, décocher le nanoKONTROL2 → il doit passer `ignored` et redevenir disponible immédiatement pour MadMapper ;
  4. redémarrer l'éditeur → l'exclusion doit tenir.

## 6. Limites connues

1. **L'exclusion est indexée par nom.** Windows suffixe les homonymes (`Roto-Control 2`), donc deux contrôleurs identiques sont difficiles à distinguer, et un nom peut changer au rebranchement : l'exclusion suivrait alors le mauvais appareil, ou plus aucun. C'est la seule identité stable que RtMidi expose ; `MidiHub` utilise déjà le nom partout (y compris `StripPortSuffix` pour apparier In et Out).
2. **Portée éditeur, pas projet.** Un seul jeu d'exclusions pour toutes les fenêtres et tous les projets ouverts avec cet éditeur, comme les mappings `MidiHub_*`.
3. **Le diagnostic dépend du message de RtMidi.** Si le driver ne remplit pas `Error`, le log retombe sur « the driver refused the port ». La distinction « occupé » / « cassé » n'est donc pas garantie, seule l'absence d'ouverture l'est.
4. **Cadence de 3 s.** Un port libéré est repris en 3 s au pire ; `Reconnect` reste là pour forcer tout de suite.
5. **Le retry tourne indéfiniment** tant qu'un port reste occupé (une passe de noms toutes les 3 s). Négligeable, mais pas nul — et c'est précisément ce que l'exclusion permet d'éteindre pour un port qu'on sait réservé.

## 7. Entrée CHANGELOG proposée

À placer sous `## [Unreleased]` :

```markdown
### Added

- **Per-port MIDI input exclusion** — a fold-out under the MIDI Map banner
  lists every input the driver exposes and lets one be left to another
  application. An ignored input is never opened and is closed on the spot if it
  was open, so a controller can be reserved for a VJ tool running alongside the
  editor. Persisted editor-wide (`MidiHub_IgnoredPorts`). New API :
  `SetPortIgnored`, `IsPortIgnored`, `AvailablePortNames`, `IsPortOpen`,
  `IsPortBusy`.

### Fixed

- **MIDI In port refused by another application** — `OpenPort` is `void` and
  RtMidi reports a refusal through `IsOk` / `Error`, which `MidiHub` never read
  back : a port held by another application (MIDI inputs are exclusive on
  Windows) was registered as open, showed as connected, delivered nothing and
  was never retried. Refused ports are now logged once, left out of the open
  set, listed as `busy` in the MIDI Map banner, and retried every 3 s until they
  free up. `TryOpenPorts` runs on that same cadence instead of every editor
  tick, and `PollNewPorts` scans every port index instead of only those past the
  open count.
```
