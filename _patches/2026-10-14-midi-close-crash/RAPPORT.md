# Crash de l'éditeur à la fermeture des ports MIDI — Kontrols 0.5.1

**Date** : 14 octobre 2026
**Projet d'origine** : `toxxic` (Unity 6000.3.10f1, HDRP, Windows 11), branche `master`
**Packages concernés** :

| Package | Version | Rôle |
|---|---|---|
| `com.blindspot.kontrols` | 0.5.1 | `MidiHub.Cleanup` / `Reconnect` ferment les ports |
| `jp.keijiro.rtmidi` | 2.2.0 | wrapper RtMidi, `Library/PackageCache` (non modifié) |

**Fichiers livrés**

```
_patches/2026-10-14-midi-close-crash/
├── RAPPORT.md                                      ← ce document
└── kontrols-0.5.1--close-port-before-free.patch
```

Indépendant des deux autres patches du jour : il ne touche que `MidiHub.cs`, dans une région que ni le patch « colonnes » ni le patch « port occupé » (retiré) ne modifient.

---

## 1. Symptôme

Deux manifestations, sur la même machine de show, pendant toute la journée du 14 :

1. **Perte du contrôleur en cours de session.** Le contrôleur répond, puis cesse de répondre « au bout d'un certain temps », sans rien dans la console. Un redémarrage de l'éditeur (et parfois un débranchement/rebranchement) le ramène, jusqu'à la fois suivante.
2. **Crash sec de l'éditeur**, reproduit en entrant en mode Play avec deux contrôleurs connectés.

## 2. Cause

Trace complète relevée dans `Editor.log` (session de 19h45) :

```
Reloading assemblies for play mode.
[DIAG] Cleanup : fermeture de 2 port(s)
Kontrols.MidiHub:Cleanup ()                             MidiHub.cs:449
...
MidiInWinMM::openPort: error closing Windows MM MIDI input port (midiInUnprepareHeader).
terminate called after throwing an instance of 'rt::midi::RtMidiError'
  what():  MidiInWinMM::openPort: error closing Windows MM MIDI input port (midiInUnprepareHeader).
...
RtMidi.MidiIn:FreeDeviceHandle (intptr)                 MidiIn.cs:32
RtMidi.MidiBase:ReleaseHandle ()                        MidiBase.cs:20
System.Runtime.InteropServices.SafeHandle:Dispose ()
Kontrols.MidiHub:Cleanup ()                             MidiHub.cs:453
UnityEditor.AssemblyReloadEvents:OnBeforeAssemblyReload ()
```

La chaîne :

1. Un **domain reload** (recompilation, ou entrée en mode Play) déclenche `AssemblyReloadEvents.beforeAssemblyReload` → `MidiHub.Cleanup()`.
2. `Cleanup` appelait `port.Dispose()` sur chaque port ouvert.
3. `Dispose` → `SafeHandle.ReleaseHandle` → `MidiIn.FreeDeviceHandle` → **`rtmidi_in_free`**, qui fait `delete (RtMidiIn*) device->ptr`.
4. Le destructeur C++ appelle `closePort()`. Le backend **WinMM** échoue sur `midiInUnprepareHeader` — le pilote n'a pas encore rendu le buffer sysex — et **lève `RtMidiError`**.
5. `rtmidi_in_free` **n'a pas de `try/catch`**, contrairement à `rtmidi_close_port` qui, lui, attrape `RtMidiError` et la convertit en drapeau d'erreur sur le device. L'exception s'échappe donc d'un destructeur à travers l'API C → **`std::terminate`** → le processus meurt avant qu'Unity ne puisse logger quoi que ce soit.

C'est un défaut du couple RtMidi/WinMM, mais il est **déclenché par l'usage que Kontrols fait de `Dispose`** — et évitable depuis C#.

### 2.1 Pourquoi cela explique aussi la perte silencieuse

Quand le même échec de fermeture **n'abat pas** le processus, il laisse le périphérique WinMM à moitié fermé : le domaine suivant rouvre le port sans erreur apparente, mais plus aucun message n'est délivré. Rien n'est journalisé, ni côté RtMidi ni côté Kontrols, dont tous les chemins d'échec d'ouverture sont muets.

D'où le cycle vécu toute la journée : **recompiler → perdre le contrôleur → redémarrer l'éditeur → le retrouver → recompiler…** Chaque édition de script faite pendant que la surface MIDI tournait coûtait le contrôleur, ce qui a aussi rendu les diagnostics intermédiaires trompeurs.

### 2.2 Ce que les mesures ont écarté

| Hypothèse | Mesure | Verdict |
|---|---|---|
| Conflit de port avec MadMapper | ouverture des deux entrées via `winmm` depuis un processus tiers : `OK` pour les deux | écartée — personne ne tenait les ports |
| Deuxième éditeur Unity résiduel | `Win32_Process` : le second processus est `unity.exe serve` du Hub (15 Mo) | écartée |
| Boucle éditeur étranglée hors focus | `Interaction Mode` déjà sur `No Throttling` | écartée |
| MIDI Learn resté armé | `_learnId` nul dans la trace (`learn=-`) | écartée |
| Périphériques disparus | `Get-PnpDevice` : Roto-Control `OK`, et 2 entrées MIDI vues par WinMM | écartée |
| Messages non dispatchés | trace `[DIAG] rx status=0xB2 d1=1 …` avec `ccSubs=3` | écartée — ils arrivaient bien, et étaient distribués |

## 3. Correctif

```csharp
static void SafeDispose(MidiBase port)
{
    if (port == null) return;
    try { port.ClosePort(); } catch { }
    try { port.Dispose();   } catch { }
}
```

utilisé pour les deux listes de ports dans `Cleanup()` et dans `Reconnect()`.

`ClosePort()` passe par **`rtmidi_close_port`**, qui attrape `RtMidiError`. Le port est alors marqué fermé (`connected_ = false` côté RtMidi), si bien que le destructeur appelé juste après par `Dispose` n'a plus rien à fermer : **la levée n'a pas lieu**. Un port jamais ouvert n'est pas affecté, `closePort()` étant un no-op dans ce cas.

Deux `try/catch` managés encadrent les appels : ils ne peuvent rien contre un `std::terminate`, mais couvrent les erreurs managées (handle déjà libéré, double dispose).

## 4. Fichiers touchés

| Fichier | Nature |
|---|---|
| `Editor/MidiHub.cs` | `SafeDispose` + son usage dans `Cleanup()` et `Reconnect()` |

Aucune signature publique modifiée, aucune migration, 2 hunks.

## 5. Vérification

- **Compilation** : `Kontrols.Editor` recompilée hors éditeur avec le Roslyn d'Unity 6000.3.10f1 et la response file de Bee du projet : **0 erreur, 0 warning**.
- **Patch** : `patch -p1` sur une copie vierge reproduit `MidiHub.cs` **à l'octet près**.
- **Test manuel attendu, et non encore fait** :
  1. contrôleur branché, entrer puis sortir du mode Play plusieurs fois → l'éditeur ne doit plus mourir ;
  2. modifier un script pour forcer une recompilation, contrôleur branché → les potards doivent répondre **après** le reload, sans redémarrage ni rebranchement ;
  3. répéter quelques fois : c'est la répétition qui valide, le défaut étant intermittent par nature (il dépend de l'état des buffers du pilote au moment de la fermeture).

## 6. Limites connues

1. **Le défaut de fond est en amont.** `rtmidi_in_free` devrait attraper, ou le backend WinMM ne devrait pas échouer sur `midiInUnprepareHeader` après un `midiInReset`. Ce patch contourne proprement depuis l'appelant ; il ne corrige ni RtMidi ni le wrapper, et un autre consommateur de `jp.keijiro.rtmidi` qui appellerait `Dispose` sans fermer d'abord reste exposé. À remonter à `jp.keijiro.rtmidi`.
2. **Si `closePort()` échoue malgré tout**, l'erreur est avalée et le port peut rester dans un état dégradé pour le domaine suivant, de façon silencieuse. Rendre cet échec visible est l'objet du patch `_patches/2026-10-14-midi-port-conflict/` (actuellement **retiré**, régression non isolée) : contrôler `IsOk` après l'ouverture et le dire dans le bandeau. Les deux se complètent, mais celui-ci est le seul des deux à être nécessaire.
3. **La fenêtre de risque n'est pas nulle** : le crash se produisait dans le destructeur, le correctif le déplace dans un appel protégé, mais rien ne garantit que le backend WinMM ne puisse pas lever ailleurs dans le même chemin sur un autre pilote.
