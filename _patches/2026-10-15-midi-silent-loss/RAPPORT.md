# Contrôleur MIDI « connecté » mais muet, et crash au Reconnect — Kontrols 0.5.1

**Date** : 15 octobre 2026
**Projet d'origine** : `toxxic` (Unity 6000.3.10f1, HDRP, Windows 11 26200), branche `master`
**Packages concernés** :

| Package | Version | Rôle |
|---|---|---|
| `com.blindspot.kontrols` | 0.5.1 + patches du 14/10 (colonnes 01/02, close-port-before-free) | `MidiHub` ouvre, surveille et ferme les ports |
| `jp.keijiro.rtmidi` | 2.2.0 (RtMidi 6.0, DLL compilée MinGW) | `Library/PackageCache`, **non modifié** |

**Fichiers livrés**

```
_patches/2026-10-15-midi-silent-loss/
├── RAPPORT.md                                          ← ce document
├── 01-kontrols-0.5.1--quarantine-failed-close.patch    fermeture sans crash
├── 02-kontrols-0.5.1--port-watchdog-reopen.patch       détection + réouverture auto
└── 03-kontrols-0.5.1--isolate-midi-subscribers.patch   un abonné qui lève n'éteint plus les autres
```

À appliquer **dans l'ordre, sur l'état actuel du package** (qui contient déjà les patches du 14/10). Le 02 dépend du 01, le 03 s'applique par-dessus le 02.

**Statut : les trois sont appliqués au package le 15/10 à 14:42, éditeur fermé.** Le résultat est identique à l'octet près à l'état compilé, et il recompile en place sans erreur ni warning. **Test au contrôleur (§7) non fait.**

Ce document **corrige le diagnostic** de `_patches/2026-10-14-midi-close-crash/` : voir §3.

---

## 1. Symptôme

Par intermittence, en répétition :

- l'onglet MIDI Map affiche le contrôleur en vert (`● MIDI Roto-Control 0`), mais les potards n'ont plus d'effet ;
- **Reconnect** ne règle rien, ou fait crasher l'éditeur ;
- seul un redémarrage complet d'Unity récupère le contrôleur.

## 2. Cause : un enchaînement en trois temps

### 2.1 Le contrôleur tombe du bus USB

Journal `Microsoft-Windows-Kernel-PnP/Device Management`, événement **1010** (« supprimé de manière inattendue, car il est signalé comme manquant sur le bus ») : **17 déconnexions en 10 jours** sur les deux contrôleurs. Et **tous les crashes Unity attribués à `RtMidi.dll` suivent l'une d'elles** (journal Application, événement 1000) :

| Déconnexion USB | Crash Unity | Module, code |
|---|---|---|
| 13/10 14:37 et 14:40 (nanoKONTROL2) | 13/10 14:58 | RtMidi.dll, `0x40000015` |
| 13/10 17:30 (nanoKONTROL2) | 13/10 17:58 | RtMidi.dll, `0x40000015` |
| 13/10 18:02 (Roto-Control) | 13/10 18:15 | RtMidi.dll, `0x40000015` |
| 14/10 16:56 (Roto-Control) | 14/10 17:09 | RtMidi.dll, `0x40000015` |
| 14/10 19:00 (Roto), 19:18 (nano) | 14/10 19:30 | RtMidi.dll, `0x40000015` |
| 15/10 **12:35:09** (Roto-Control) | 15/10 **12:35:34** | **ntdll.dll, `0xc0000374`** (corruption du tas) |

`0x40000015` = `STATUS_FATAL_APP_EXIT` (abort → `std::terminate`) ; `0xc0000374` = `STATUS_HEAP_CORRUPTION`.

On ne peut pas distinguer, dans ce journal, un débranchement volontaire d'une chute spontanée. La déconnexion du 14/10 10:18 coïncide avec l'apparition d'un `RPI RP2` (mode bootloader du Roto-Control, mise à jour de firmware). Les autres n'ont pas d'explication dans les journaux.

**Topologie** : le Roto-Control, le nanoKONTROL2 **et** la carte son Behringer UMC202HD sont tous sur le même hub Genesys GL850G (`USB\VID_05E3&PID_0608\5&1f1b5891&0&5`), un hub USB 2.0 à transaction translator unique. **Toutes les déconnexions enregistrées concernent des appareils de ce hub.** La suspension sélective USB est déjà désactivée (secteur et batterie).

### 2.2 `MidiHub` ne voit jamais la perte

Un port dont l'appareil a disparu ne lève aucune erreur : son handle WinMM devient muet, définitivement. Quand l'appareil revient, Windows le liste comme un **nouveau** périphérique, que seule une nouvelle ouverture atteint. Or :

```csharp
public static bool IsConnected => _ports.Count > 0;      // compte des objets, ne vérifie rien

static void PollNewPorts() {
    ...
    if (portCount <= _ports.Count) return;                // retour du contrôleur = même nombre → rien
    for (int i = _ports.Count; i < portCount; i++) ...    // suppose les nouveaux ports en fin de liste
}
```

Le port mort reste dans `_ports` jusqu'au redémarrage : voyant vert, potards morts. **C'est exactement le symptôme.**

Défauts voisins, corrigés en passant :
- `PollNewPorts` suppose que les nouveaux ports arrivent en fin de liste. Windows renumérote à chaque arrivée : un appareil branché à un index inférieur faisait **rouvrir un port déjà ouvert** (sous son nouveau nom suffixé). Avec Windows MIDI Services, cette double ouverture réussit, et chaque CC arrive alors deux fois. Le nouvel appareil, lui, n'était jamais ouvert.
- Une ouverture refusée par RtMidi (signalée seulement par le drapeau `IsOk`) entrait quand même dans `_ports`, et le port s'affichait comme connecté.
- Le minuteur « 3 s » comptait en réalité des ticks d'éditeur (`_pollTimer += 0.016f`).

### 2.3 Fermer un port mort fait crasher l'éditeur (bug d'RtMidi 6.0)

Vérifié dans le source amont (`thestk/rtmidi`, tag 6.0.0, `MidiInWinMM::closePort`) :

1. la boucle libère chaque buffer sysex (`delete[] lpData`, `delete[] sysexBuffer[i]`) **avant** de tester le résultat de `midiInUnprepareHeader` ;
2. en cas d'échec (systématique sur un handle mort), la fonction appelle `midiInClose` et **retourne tôt** : `connected_` reste à `true`, la section critique reste prise, et la liste des buffers n'est pas vidée ;
3. toute fermeture suivante reparcourt la liste et **libère une seconde fois les mêmes buffers**.

Le destructeur appelé par `Dispose` (`rtmidi_in_free` → `~MidiInWinMM` → `closePort()`) est précisément une fermeture suivante. Trois chemins mènent à la fermeture d'un port mort : **Reconnect, l'entrée en Play (rechargement du domaine) et une recompilation.**

Dernières lignes de `Editor-prev.log`, juste avant le crash de 12:35:34 :

```
MidiInWinMM::initialize: no MIDI input devices currently available.   ← ×3 : le Roto a disparu
Scanning for USB devices : 4.811ms                                     ← ×8 : notifications de changement de périphérique
MidiInWinMM::openPort: error closing Windows MM MIDI input port (midiInUnprepareHeader).
<fin du fichier : processus mort>
```

Aucun « Reloading assemblies » juste avant : la fermeture vient très probablement du bouton **Reconnect**.

## 3. Ce que ça change pour le diagnostic du 14/10

- Le patch `close-port-before-free` partait du principe que fermer d'abord « laisse au destructeur rien à faire ». C'est vrai quand la fermeture **réussit**, c'est-à-dire précisément quand le patch est inutile. Quand elle échoue, il garantit la double libération. Il a **changé la forme du crash** (`std::terminate` → corruption du tas, une seule ligne « error closing » dans le log au lieu de deux) sans le supprimer. Le patch 01 le remplace.
- L'échec de `midiInUnprepareHeader` ne venait pas de « buffers sysex pas encore rendus au moment du reload ». Il venait d'un **handle mort** : chaque crash suit une déconnexion USB.
- **Windows MIDI Services est actif sur cette machine** (`midisrv` en cours d'exécution ; le nanoKONTROL2 utilise le pilote `usbmidi2.inf` ; le Roto-Control passe par le transport `MIDIU_KSA_…`). Les ports y acceptent plusieurs clients à la fois : c'est ce qui permet à MadMapper et Unity d'écouter les mêmes contrôleurs. La sonde `midi-probe.ps1` (« OK = personne ne tient le port ») ne prouve donc rien dans cette configuration.

## 4. Correctifs

### 01 — Fermeture sans double libération (`MidiHub.cs`)

`rtmidi_close_port` attrape l'erreur et efface le drapeau `ok` du wrapper : c'est ainsi qu'un échec de fermeture devient visible depuis C#. `SafeDispose` devient :

```csharp
if (port.IsOk) {
    try { port.ClosePort(); } catch { }
    if (!port.IsOk) {                                  // fermeture ratée : objet natif corrompu
        if (port is MidiIn input) input.MessageReceived = null;   // annule le callback
        port.SetHandleAsInvalid();                     // ni Dispose ni le finaliseur ne le libéreront
        _quarantine.Add(port);                         // fuite volontaire : quelques Ko
        return;
    }
}
port.Dispose();
```

Le handle système est bien libéré : le chemin d'erreur d'RtMidi a appelé `midiInClose`. Un port dont l'ouverture a échoué n'est pas connecté : il est libéré sans fermeture. Le message d'erreur du wrapper n'est **pas** lu, car `rtmidi_c` y range `err.what()` d'une exception qui n'existe plus à la sortie du `catch`.

### 02 — Détection et réouverture automatiques (`MidiHub.cs`, `KontrolsWindow.Midi.cs`)

`TryOpenPorts` et `PollNewPorts` sont remplacés par un **watchdog** unique, cadencé sur le temps réel (`EditorApplication.timeSinceStartup`). Deux fois par seconde, il liste les entrées que Windows propose et les compare aux ports ouverts :

- **ouvert mais plus listé** → fermé (avec la quarantaine du 01), signalé `[MidiHub] Lost : Roto-Control`, ajouté à `MidiHub.LostPortNames` (avertissement dans l'onglet MIDI Map) ;
- **listé mais pas ouvert** → ouvert : première ouverture, après un Reconnect, ou retour d'un port perdu (`[MidiHub] Back : …`). Une ouverture refusée n'est pas gardée : elle est journalisée une fois puis retentée toutes les 2 s.

Les ports sont identifiés par **leur nom sans le suffixe d'index** qu'RtMidi ajoute sous WinMM (`"Roto-Control 0"` → `"Roto-Control"`), jamais par leur index. Un deuxième appareil identique reçoit `" #2"`. Chaque entrée est gardée avec la sortie qui lui est appariée, et les deux sont fermées ensemble. Sans aucune entrée listée, la sonde passe à une fois toutes les 2 s (RtMidi écrit un avertissement à chaque sonde dans cet état).

`Reconnect` ferme tout, vide la liste des ports perdus et laisse le watchdog rouvrir au tick suivant.

Côté fenêtre : quand le nombre de ports ouverts **augmente**, la fenêtre renvoie l'état de tous ses paramètres au contrôleur (`SendAllFeedback`), ce qui remet les potards motorisés à leur place. Cela vaut aussi après un rechargement du domaine, où le `SendAllFeedback` d'`OnEnable` part avant toute ouverture de port. C'est limité à **une fois toutes les 5 s** : si c'est le mouvement simultané de tous les moteurs qui fait tomber un contrôleur alimenté par l'USB, une resynchro non bridée le ferait rebondir en boucle.

API publique ajoutée : `MidiHub.LostPortNames`, `MidiHub.HasOutput`. `PortNames` contient désormais les clés (`"Roto-Control"` au lieu de `"Roto-Control 0"`) ; Presets ne s'en sert que pour une infobulle.

### 03 — Abonnés isolés (`MidiHub.cs`)

`?.Invoke` s'arrête au premier abonné qui lève : les suivants ne reçoivent pas le message, `ProcessQueue` s'interrompt (le reste de la file attend le tick suivant), et `EditorApplication.update` saute ses autres callbacks pour la frame. `OnCC` a trois abonnés ici (`KontrolsWindow.OnMidiCC`, `KontrolsWindow.OnTempoCC`, `PresetRecall.OnCc`). Un abonné qui lève à chaque message rendrait muets tous ceux inscrits après lui, port vivant : **même symptôme qu'un contrôleur perdu, sans contrôleur perdu**.

Chaque abonné d'`OnCC`, `OnNote`, `OnPitchBend`, `OnRealtime`, `OnConnectionChanged` et des callbacks de Learn tourne désormais dans son propre `try/catch` (`Each<T>`, sur `GetInvocationList`). Un abonné défaillant est journalisé avec son type et sa méthode, **une fois toutes les 5 s** et pas à chaque message : un potard tourné envoie des dizaines de messages par seconde, et une console inondée ralentit l'éditeur.

Les journaux du 14/10 et du 15/10 ne montrent aucune exception dans ce chemin. C'est un durcissement, pas la cause de l'incident.

## 5. Fichiers touchés

| Patch | Fichiers | Hunks |
|---|---|---|
| 01 | `Editor/MidiHub.cs`, `CHANGELOG.md` | 2 |
| 02 | `Editor/MidiHub.cs`, `Editor/KontrolsWindow.Midi.cs`, `CHANGELOG.md` | 9 |
| 03 | `Editor/MidiHub.cs`, `CHANGELOG.md` | 10 |

Aucune signature publique retirée ni modifiée ; deux propriétés ajoutées. `MidiHub.cs` et `KontrolsWindow.Midi.cs` sont de l'UTF-8 valide (LF) : l'avertissement du 14/10 sur l'encodage ne s'applique plus. Les `U+FFFD` présents sont de vrais caractères de remplacement, encodés proprement.

## 6. Vérifications faites

- **Compilation hors Unity**, avec le Roslyn d'Unity 6000.3.10f1 et la *response file* Bee du projet (`Library/Bee/artifacts/1900b0aE.dag/Kontrols.Editor.rsp`, méthode du §7.1 de la session du 14/10), **après chaque patch** : 0 erreur, 0 warning.
- **Assemblies dépendantes** recompilées contre le `Kontrols.Editor` patché : `Presets.Editor` et `Assembly-CSharp-Editor` (la fenêtre `kontrolsToxxic01`) : 0 erreur.
- **Patches** : `patch -p1` des trois, dans l'ordre, sur une copie de l'état actuel du package, reproduit l'état final **à l'octet près** (`cmp`), sans fuzz.

**Rien de tout cela n'est un test.** Compiler et s'appliquer sont des conditions nécessaires (leçon du 14/10, §11). Seul le protocole ci-dessous, contrôleur en main, valide.

## 7. Protocole de test (à faire, un patch à la fois)

**Appliquer Unity fermé**, ou au minimum contrôleur fonctionnel : la recompilation déclenche un rechargement du domaine, qui ferme les ports. Si un port est mort à ce moment, le code actuel crashe.

```bash
cd /c/Users/chamb/Documents/GitHub/NewVersions/Kontrols-0.5.1/Kontrols-0.5.1
patch -p1 --dry-run < /c/Users/chamb/Documents/GitHub/toxxic/_patches/2026-10-15-midi-silent-loss/01-kontrols-0.5.1--quarantine-failed-close.patch
```

(sans `--dry-run` pour appliquer ; même chose pour 02 puis 03).

Le bug est **reproductible à la demande** : débrancher le Roto-Control 3 s, puis le rebrancher.

**Après 01 seul**
1. Contrôleur branché, potards actifs. Débrancher 3 s, rebrancher. Attendu : voyant toujours vert, potards morts (le 02 n'y est pas encore).
2. Cliquer **Reconnect**. Attendu : **pas de crash** ; dans la console, `Closing a MIDI port failed (device gone ?) : the port is quarantined` ; les potards répondent à nouveau.
3. Recommencer le débranchement, puis **entrer en Play** au lieu de Reconnect. Attendu : pas de crash, et les potards répondent après le rechargement.

**Après 02**
1. Débrancher le Roto-Control. Attendu en moins d'une seconde : `[MidiHub] Lost : Roto-Control`, avertissement dans l'onglet MIDI Map.
2. Rebrancher. Attendu : `[MidiHub] Back : Roto-Control`, potards actifs **sans rien toucher**, moteurs replacés sur les valeurs de la fenêtre.
3. Même chose avec le nanoKONTROL2, puis les deux à la fois. Brancher le nano *après* le Roto pour provoquer une renumérotation : la ligne d'état de l'onglet MIDI Map doit citer **chaque contrôleur une seule fois** (`Roto-Control, nanoKONTROL2`). L'ancien code pouvait afficher `Roto-Control 0, Roto-Control 1`, signe d'une double ouverture.
4. Entrer et sortir du mode Play, forcer une recompilation : les ports se rouvrent, les moteurs se replacent.
5. Laisser tourner 10 min, tout branché : aucun `Lost` dans la console. Coût du watchdog à vérifier dans le Profiler (`MidiHub.Watchdog`) : une sonde RtMidi toutes les 0,5 s, non mesurée dans Unity.

**Après 03**

Script **temporaire**, à placer dans `Assets/Editor/` puis à supprimer :

```csharp
using UnityEditor;
using Kontrols;

static class MidiThrowTest
{
    static void Throw(byte ch, byte cc, byte v) => throw new System.Exception("test : abonne OnCC defaillant");
    [MenuItem("Tools/MIDI test/Abonne qui leve ON")]  static void On()  => MidiHub.OnCC += Throw;
    [MenuItem("Tools/MIDI test/Abonne qui leve OFF")] static void Off() => MidiHub.OnCC -= Throw;
}
```

Activer `ON`, puis **fermer et rouvrir** la fenêtre ToxxicKontrols (elle se réabonne alors *après* l'abonné défaillant). Tourner un potard. Attendu : le paramètre bouge, et une erreur `MidiThrowTest.Throw threw while handling MIDI` apparaît toutes les 5 s au plus. Sans le 03, le potard serait mort.

## 8. Limites connues

1. **Une coupure plus courte que 0,5 s** (disparu puis revenu entre deux sondes) n'est pas vue. Une ré-énumération USB complète (reset, descripteurs, pilote, point de terminaison `midisrv`) prend vraisemblablement plus longtemps, mais ce n'est pas mesuré. Le bouton **Reconnect**, désormais sans danger, reste le recours manuel. Pour aller plus loin : les notifications de périphériques de Windows (`CM_Register_Notification`) attrapent toutes les arrivées et suppressions, même brèves (non fait ici).
2. **Un port muet qui reste listé** (service `midisrv` redémarré en conservant les noms, pilote bloqué) n'est pas détecté. Même recours : Reconnect.
3. **Deux appareils identiques** sont distingués par leur ordre d'énumération (`" #2"`). Si le premier tombe, les clés glissent : le second est fermé à tort puis rouvert, et le port mort du premier est gardé sous la clé restante. Ne concerne pas la configuration actuelle.
4. **Quarantaine** : quelques Ko de mémoire native perdus par incident, jusqu'à la fermeture de l'éditeur.
5. **Resynchro des moteurs** bridée à une fois toutes les 5 s : si deux contrôleurs reviennent à 3 s d'intervalle, le second n'est pas resynchronisé automatiquement (bouton « Resend All Feedback »).
6. **La cause première, les chutes USB, n'est pas traitée par le code** : voir §9.
7. **Le bug d'RtMidi reste en amont** : tout autre consommateur de `jp.keijiro.rtmidi` qui ferme un port mort puis le libère est exposé.

## 9. Hors code : le matériel

- Brancher le **Roto-Control directement sur un port arrière de la carte mère** (contrôleur Intel, hors du GL850G), séparé de l'UMC202HD. S'il faut un hub, en prendre un **alimenté par secteur**. Vérifier le câble.
- Hypothèse à tester (non démontrée) : le mouvement simultané de tous les moteurs (rappel de preset, resynchro) provoque un pic de courant qui fait tomber le contrôleur. Test : enchaîner des rappels de presets qui font bouger tous les potards, puis lire le journal :

```powershell
Get-WinEvent -FilterHashtable @{LogName='Microsoft-Windows-Kernel-PnP/Device Management'; Id=1010; StartTime=(Get-Date).AddHours(-2)} | Format-List TimeCreated,Message
```

- Topologie (*Gestionnaire de périphériques → Affichage → Périphériques par connexion*), ou :

```powershell
Get-PnpDeviceProperty -InstanceId 'USB\VID_2E8A&PID_F010\E6634CA5DB904A28' -KeyName DEVPKEY_Device_Parent | Select-Object Data
```

## 10. Reste à faire

1. **Tester** 01, puis 02, puis 03, contrôleur en main (§7).
2. **Matériel** (§9) : c'est la seule action qui supprime le déclencheur.
3. **Remonter en amont** : à `thestk/rtmidi`, le chemin d'erreur de `MidiInWinMM::closePort` (buffers libérés avant le test, retour sans `LeaveCriticalSection` ni `connected_ = false`, liste non vidée) ; à `jp.keijiro.rtmidi`, `rtmidi_in_free` sans `try/catch` et le pointeur `msg` vers une exception détruite.
4. Pistes non traitées, relevées pendant l'analyse :
   - un bandeau « contrôleur perdu » dans l'onglet principal, pas seulement dans MIDI Map ;
   - un verrou « live » qui désactive les boutons de Learn : un clic involontaire capte le prochain potard et le réassigne ;
   - `MidiHub` n'appelle jamais `IgnoreTypes` : RtMidi filtre l'horloge MIDI (`0xF8`) par défaut, si bien que la source de tempo « MIDI Clock » ne reçoit rien.
