# Résolution scopée par scène — Kontrols 0.5.1 & Presets 0.3.0

**Date** : 13 octobre 2026
**Projet d'origine** : `toxxic` (Unity 6000.3.10f1, HDRP, Windows 11), branche `AntoineB`
**Packages concernés** :

| Package | Version | Chemin local sur la machine d'origine |
|---|---|---|
| `com.blindspot.kontrols` | 0.5.1 | `C:/Users/chamb/Documents/GitHub/NewVersions/Kontrols-0.5.1/Kontrols-0.5.1` |
| `com.blindspot.presets` | 0.3.0 | `C:/Users/chamb/Documents/GitHub/NewVersions/Presets-0.3.0/Presets-0.3.0` |

Les deux sont référencés en dépendances locales (`file:`) dans `Packages/manifest.json` du projet. Les patchs sont **déjà appliqués et validés** sur la machine d'origine ; ce document et les deux fichiers `.patch` qui l'accompagnent servent à les intégrer dans les dépôts amont.

**Fichiers livrés**

```
_patches/2026-10-13-editor-focus-lag/
├── RAPPORT.md                                          ← ce document
├── kontrols-0.5.1--scene-scoped-binding-resolution.patch
└── presets-0.3.0--scene-scoped-target-resolution.patch
```

---

## 1. Symptôme

Le projet tourne en **mode Play dans l'éditeur** (pas en build) : une caméra rend dans une `RenderTexture`, un `SpoutSender` l'envoie à MadMapper.

> Quand la fenêtre Unity reçoit le focus, le framerate s'effondre. Quand MadMapper reprend le focus, le framerate redevient normal.

Le FPS est mesuré côté Unity par `FPS.cs` (`1f / Time.deltaTime`), affiché dans le rendu envoyé en Spout : il s'agit donc bien du débit réel du player loop, pas d'une impression visuelle de la Game View.

Le problème est d'abord apparu sur une seule scène (`CLUB_V3`, devenue `01_STRIP_CLUB`), puis s'est étendu à `03_RAINBOW` après la refonte des presets de cette scène.

## 2. Pourquoi le focus change quelque chose

Dans l'éditeur Unity, une grande partie du travail « côté éditeur » n'est pas cadencée par le player loop :

- repaint des `EditorWindow` (Console, Inspector, Hierarchy, fenêtres custom),
- dispatch de `EditorApplication.update`,
- dispatch de `EditorApplication.hierarchyChanged`,
- rendu de la Scene View.

Unity **throttle** tout cela quand sa fenêtre n'a pas le focus, et le fait tourner à plein régime quand elle l'a. Conséquence : **n'importe quel travail coûteux côté éditeur se manifeste exactement comme ce symptôme** — lag au focus, fluide hors focus. Le travail en question est payé sur le même thread principal et le même GPU que la caméra Spout.

## 3. Architecture du show (indispensable pour comprendre la cause)

Quatre scènes :

| Scène | Rôle | Chargement |
|---|---|---|
| `00_MASTER_` | rig VR, Rokoko, OVR | toujours chargée |
| `01_STRIP_CLUB` | contenu | additive, une seule à la fois |
| `02_PELLETEUSE` | contenu | additive, une seule à la fois |
| `03_RAINBOW` | contenu | additive, une seule à la fois |

Les quatre sont présentes dans la Hierarchy ; `Presets.SceneSwitcher` charge/décharge la scène de contenu au rappel d'un preset. **À tout instant, deux des trois scènes de contenu sont présentes mais non chargées** (`isLoaded: 0`).

C'est le point central : les deux packages ancrent leurs cibles par `GlobalObjectId`, et un `GlobalObjectId` d'objet de scène ne peut être résolu que si sa scène est chargée.

## 4. Cause racine

Les deux packages appelaient `GlobalObjectId.GlobalObjectIdentifierToObjectSlow()` **sans vérifier au préalable si la scène de l'ancre était chargée**.

Pour une ancre pointant vers une scène non chargée, chaque appel coûtait :

1. le lookup lent lui-même (`...Slow`, le nom est honnête) ;
2. une assertion native `Assertion failed on expression: 'manager != NULL'` ;
3. `UnityEngine.StackTraceUtility.ExtractStackTrace()` — parcours de pile managé, avec allocations ; c'est de loin le poste le plus cher ;
4. une écriture synchrone dans `Editor.log` ;
5. **côté Presets uniquement** : un repli par nom via `Resources.FindObjectsOfTypeAll<GameObject>()`, soit un parcours de **tous** les GameObjects en mémoire (~30 000 objets dans ce projet, assets de prefabs inclus), avec comparaison de chaîne sur chacun.

### 4.1 Kontrols — `OnHierarchyChanged`

```csharp
void OnHierarchyChanged()
{
    // Cheap: only re-resolve bindings that are unresolved.
    bool any = false;
    foreach (var list in _refMap.Values)
    foreach (var b in list)
        if (b.IsComplete && !b.IsResolved) { ResolveBinding(b); any = true; }
    _optionCache.Clear();
    if (any) Repaint();
}
```

Le commentaire « Cheap » part d'une hypothèse qui ne tient pas dans un show multi-scènes : un binding dont la scène n'est pas chargée est **définitivement** non résolu, donc il est re-tenté à **chaque** `hierarchyChanged`, indéfiniment.

Les bindings sont persistés globalement dans les `EditorPrefs` sous `Kontrols_<classeFenêtre>_ref_<id>_<i>`, donc ils couvrent les quatre scènes. Répartition mesurée le 12 octobre 2026 (32 bindings) :

| Cible | Nb |
|---|---|
| `01_STRIP_CLUB` | 13 |
| `03_RAINBOW` | 7 |
| `02_PELLETEUSE` | 6 |
| `00_MASTER_` | 4 |
| vides | 2 |

Soit, selon la scène chargée :

| Scène de contenu chargée | Bindings résolus | **Bindings morts, re-tentés en boucle** |
|---|---|---|
| `01_STRIP_CLUB` | 17 | 13 |
| `03_RAINBOW` | 11 | **19** |

Cela explique précisément pourquoi `03_RAINBOW` est devenue le pire cas : la majorité des bindings vit dans la scène du club.

### 4.2 Presets — liste de capture globale

`config.goTargets` est **une seule liste** de 14 cibles, toutes activées, réparties sur les trois scènes de contenu :

| Scène | Cibles |
|---|---|
| `01_STRIP_CLUB` | 6 (`CONDUITE_stripClub`, `ANCHOR_VR`, `ANCHOR_Rokoko`, `Onyx_New_Unity`, `newPlaceAvatar1`, `AVATARS_2`) |
| `03_RAINBOW` | 5 (`DiscoLight_A`, `CONDUITE_Rainbow`, `ANCHOR_Rokoko`, `ANCHOR_VR`, `JEN_Avatar`) |
| `02_PELLETEUSE` | 3 (`---CONDUITE pelleteuse---`, `ANCHOR_VR`, `MarkerVR_new`) |

`GameObjectCapture.Capture()` et `GameObjectCapture.Apply()` parcourent la liste entière à chaque capture et à chaque rappel. Avec `03_RAINBOW` chargée, **9 cibles sur 14 sont inatteignables** — et chacune payait les 5 postes ci-dessus, y compris le scan complet des GameObjects.

Le banc comptait 59 slots ; un slot `Rainbow_*` porte 16 références dont **6 pointent hors de sa propre scène**.

### 4.3 Fréquence de déclenchement

C'est ce qui a fait basculer le problème d'« acceptable » à « bloquant » :

- chaque `LoadSlot` → `SceneSwitcher.SwitchThenApply` → `ApplySlot` brasse massivement la hiérarchie, donc déclenche `hierarchyChanged` à répétition ;
- `ApplySlotOnBeat` permet de quantifier les rappels **sur les temps musicaux** : ce n'est pas un coût ponctuel, il se répète pendant tout le show ;
- tout passe par `EditorApplication.update` et les repaints de fenêtres — donc throttlé hors focus, plein régime au focus.

### 4.4 Bug de correction découvert au passage

`SceneObjects.FindGOInScene(string goName)` retournait **le premier GameObject de ce nom dans n'importe quelle scène chargée**. Or plusieurs scènes portent des objets homonymes :

| Nom | `01_STRIP_CLUB` | `02_PELLETEUSE` | `03_RAINBOW` |
|---|---|---|---|
| `ANCHOR_VR` | 1 | 1 | 1 |
| `ANCHOR_Rokoko` | 1 | 0 | 1 |

Une cible `ANCHOR_VR` du club dont le `GlobalObjectId` échoue pouvait donc se rabattre sur l'`ANCHOR_VR` de `03_RAINBOW` et **lui appliquer la transform du preset du club**. Non observé sur site, déduit du code — mais c'est un candidat à vérifier en cas de dérive inexpliquée de placement d'ancres entre scènes.

## 5. Mesures

Toutes relevées dans `%LOCALAPPDATA%/Unity/Editor/Editor.log`.

| | Avant patch (12 oct., session de travail) | Après patch (12 oct. 17:59 → 20:16) |
|---|---|---|
| Taille du log | 10 Mo / 101 832 lignes | **2,4 Mo / 24 714 lignes** |
| `Assertion failed ... 'manager != NULL'` | **868** | **0** |
| … dont via `KontrolsWindow:OnHierarchyChanged` | 868 (100 %) | **0** |
| `[Presets] ... not found — skipped` (chacun avec stack trace + 1 scan complet des GameObjects) | **283** | **0** |
| Rappels de presets dans la session (`[Presets] Loaded`) | ~230 `LoadSlot` / 235 `ApplySlot` | **78** |
| `error CS` | 0 | **0** |

La session post-patch comporte 78 rappels de presets réels et produit **zéro** assertion et **zéro** scan de repli. Les patchs compilent sans erreur ni warning sous Unity 6000.3.10f1.

> Contexte : deux sessions antérieures atteignaient 34 Mo et 90 Mo de log, dominées par une autre cause (un `AnimateOnInput` qui écrivait 4 paramètres d'Animator inexistants par frame — 78 196 entrées). Celle-là a été corrigée côté projet, pas dans ces packages. Elle est mentionnée ici seulement pour expliquer les tailles de log historiques.

## 6. Décisions de conception

Chaque décision est volontaire ; elles priment sur le détail d'implémentation si l'amont veut réécrire le patch autrement.

### D1 — Scoper via le GUID de scène contenu dans le `GlobalObjectId`, sans toucher au format des données

Un `GlobalObjectId` d'objet de scène porte le GUID de son asset de scène (`identifierType == 2`, `assetGUID`). On peut donc décider « cette ancre est hors jeu » **sans migration de données**, sans changer le format du banc de presets ni celui des `EditorPrefs` de Kontrols.

*Alternative écartée* : scoper les données elles-mêmes (une liste de cibles par scène, des bindings par scène). C'est la bonne cible à long terme (§8) mais cela imposait une migration et un risque de perte de câblage, inacceptable en cours de production.

### D2 — Échouer en mode ouvert (*fail open*)

Dès que la scène d'une ancre ne peut pas être déterminée avec certitude — ancre non-scène, GUID nul, chemin introuvable, chemin qui n'est pas un `.unity` — **on repasse par le chemin d'origine à l'identique**.

Raison : un faux « scène non chargée » casserait silencieusement un câblage qui fonctionnait. Le pire cas acceptable est « on n'optimise pas », jamais « on perd une résolution ».

Côté Kontrols, cela va jusqu'à : si le jeu de GUID de scènes chargées est vide (état inattendu), `TargetSceneLoaded` renvoie `true` et ne bloque rien.

### D3 — Les ancres qui ne sont pas des objets de scène passent sans filtre

`identifierType != 2` (asset importé, source asset) → résolution normale. Certains bindings pointent vers des assets, par exemple le `Rokoko.CommandAPI.StudioCommandAPI` du projet d'origine (`GlobalObjectId_V1-1-...`). Les filtrer les aurait cassés.

### D4 — L'état « non résolu » reste exactement le même

Un binding dont la scène est déchargée est laissé non résolu (`ResolvedGo/Component/Method = null`), et `ResolveGameObject` renvoie `null` — **exactement ce qui se passait avant**, en sautant simplement le travail inutile. Les appelants gèrent déjà `null` (ils `continue`). Aucune sémantique visible ne change.

### D5 — Confiner le repli par nom, plutôt que le supprimer

Le repli par nom a une raison d'être légitime : retrouver un objet recréé (nouveau `fileID`), et trouver les objets **inactifs**, que `GameObject.Find` ignore — ce qui est une exigence dure pour un système qui sauvegarde/restaure l'état actif.

Le supprimer aurait régressé cette capacité de récupération. Il est donc **restreint à la scène de l'ancre** quand on la connaît : on garde le bénéfice et on ferme le trou du §4.4.

### D6 — `OnHierarchyChanged` : conditionner la passe, et abonner les vrais événements

Deux volets complémentaires, parce que l'un sans l'autre serait soit insuffisant, soit risqué :

- **la garde dans `ResolveBinding`** (D1–D4) supprime le coût ; c'est elle qui fait le gain mesuré ;
- **le conditionnement de la passe** dans `OnHierarchyChanged` évite en plus de boucler pour rien : un changement de hiérarchie ne peut *jamais*, à lui seul, rendre résoluble un binding hors scène.

Pour ne pas régresser le *timing* de résolution, les événements qui peuvent réellement changer l'issue sont abonnés explicitement : `EditorSceneManager.sceneOpened` / `sceneClosed`, `SceneManager.sceneLoaded` / `sceneUnloaded`, `EditorApplication.playModeStateChanged`. Chacun programme une passe **différée d'une frame** via `EditorApplication.delayCall`, parce qu'un événement de scène peut survenir avant que tous les objets de cette scène soient en place.

### D7 — `_optionCache.Clear()` reste inconditionnel

Les listes d'options du tab References sont indexées sur des `GetInstanceID()`, qui changent dès qu'un objet apparaît ou disparaît. Les invalider à chaque changement de hiérarchie est une exigence de correction, pas une optimisation ratée. Le coût de reconstruction n'est payé que si ce tab est visible (voir §8).

### D8 — Les avertissements « not found » sont dégradés, pas supprimés

Une cible dont la scène n'est pas chargée est un cas **normal** dans ce workflow : la signaler produisait une stack trace par cible hors scène, à chaque capture et chaque rappel. Elle est maintenant silencieuse, via le helper public `SceneObjects.TargetSceneUnloaded(id)`.

Un objet réellement manquant **dans une scène chargée** reste un avertissement : c'est une vraie anomalie de configuration.

Dans la Capture tab, le `HelpBox` distingue les deux cas (`MessageType.Info` « Its scene is not loaded » vs `MessageType.Warning` d'origine).

### D9 — Mettre en cache le jeu de GUID de scènes chargées

Côté Kontrols, `TargetSceneLoaded` est appelé une fois par binding et par événement. Une signature bon marché (`handle` + `isLoaded` de chaque scène) sert de clé d'invalidation, pour éviter un `AssetDatabase.AssetPathToGUID` par binding.

Côté Presets, les appels sont bien moins fréquents (par cible, par capture/rappel) : pas de cache, le code reste direct et lisible.

### D10 — Deux patchs séparés, un par dépôt

Kontrols et Presets sont deux dépôts distincts. Un patch combiné aurait été impossible à appliquer proprement.

## 7. Implémentation

### 7.1 Presets — `Editor/SceneObjects.cs`

Nouvelle classification d'ancre, privée :

```csharp
enum IdScope { Unknown, SceneLoaded, SceneNotLoaded }

static IdScope ScopeOf(string id, out Scene scene)
{
    scene = default;
    if (string.IsNullOrEmpty(id)) return IdScope.Unknown;
    if (!GlobalObjectId.TryParse(id, out var gid)) return IdScope.Unknown;
    if (gid.identifierType != SceneObjectIdType) return IdScope.Unknown;   // 2

    string guid = gid.assetGUID.ToString();
    if (string.IsNullOrEmpty(guid) || guid == NullGuid) return IdScope.Unknown;

    string path = AssetDatabase.GUIDToAssetPath(guid);
    if (string.IsNullOrEmpty(path)) return IdScope.Unknown;
    if (!path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)) return IdScope.Unknown;

    scene = SceneManager.GetSceneByPath(path);
    if (!scene.IsValid() || !scene.isLoaded) { scene = default; return IdScope.SceneNotLoaded; }
    return IdScope.SceneLoaded;
}
```

`IdScope.SceneNotLoaded` couvre les deux cas inatteignables : scène absente de la hiérarchie, et scène présente mais déchargée.

`ResolveGameObject` sort tôt, et confine le repli :

```csharp
public static GameObject ResolveGameObject(string id, string nameFallback)
{
    var scope = ScopeOf(id, out var scene);
    if (scope == IdScope.SceneNotLoaded) return null;   // ni lookup lent, ni scan

    var obj = Resolve(id);
    if (obj is GameObject go) return go;
    if (obj is Component c)   return c.gameObject;

    return FindGOInScene(nameFallback, scope == IdScope.SceneLoaded ? scene : default);
}
```

`ResolveComponent` reçoit la même garde avant son `Resolve(compId)`.

**Surface publique** — ajouts uniquement, aucune suppression :

| Membre | Nature |
|---|---|
| `bool TargetSceneUnloaded(string id)` | nouveau |
| `GameObject FindGOInScene(string goName, Scene restrictTo)` | nouvelle surcharge |
| `GameObject FindGOInScene(string goName)` | **conservé**, délègue à la surcharge avec `default` |

Nouveaux `using` : `System`, `UnityEngine.SceneManagement`.

### 7.2 Presets — `Editor/GameObjectCapture.cs` et `Editor/PresetsWindow.cs`

Les quatre sites d'avertissement sont ajustés comme en D8. Les avertissements au niveau *composant* sont inchangés : ils ne se déclenchent qu'après résolution du GameObject, ce qui implique désormais que sa scène est chargée — un composant manquant y est donc une vraie anomalie.

### 7.3 Kontrols — `Editor/KontrolsWindow.References.cs`

Helpers de scoping, avec le cache de D9 :

```csharp
static readonly HashSet<string> s_LoadedSceneGuids = new HashSet<string>();
static string s_LoadedSceneSig;

static bool LoadedSceneSetChanged()   // rafraîchit le set, dit s'il a bougé
static bool TargetSceneLoaded(GlobalObjectId gid)
```

Garde dans `ResolveBinding`, juste après le parse :

```csharp
if (!GlobalObjectId.TryParse(binding.GoId, out var goid)) return;

// Scène hors jeu : rien à résoudre, et le chemin lent ne coûterait
// qu'une assertion + une stack trace. Le binding est laissé non résolu,
// exactement comme il l'aurait été.
if (!TargetSceneLoaded(goid)) return;

var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(goid);
```

`OnHierarchyChanged` conditionné, passe extraite, événements de scène abonnés (D6) :

```csharp
void OnHierarchyChanged()
{
    _optionCache.Clear();                       // toujours (D7)
    if (!LoadedSceneSetChanged()) return;
    ResolveUnresolvedBindings();
}

void ScheduleResolvePass()                      // différé d'une frame
{
    EditorApplication.delayCall -= ResolveUnresolvedBindings;
    EditorApplication.delayCall += ResolveUnresolvedBindings;
}
```

`SubscribeReferences` / `UnsubscribeReferences` sont symétriques, `delayCall` inclus.

**Surface publique** : inchangée. Tous les ajouts sont privés ou statiques privés.

Nouveaux `using` : `UnityEditor.SceneManagement`, `UnityEngine.SceneManagement`.

**Chemins non touchés** : `LoadReferences()` et la réassignation depuis le tab References appellent `ResolveBinding` directement, donc le câblage manuel et le chargement initial ne passent pas par le conditionnement.

## 8. Limites assumées et pistes amont

Ce que ces patchs **ne** font **pas** :

1. **Le modèle de données reste global.** Les bindings Kontrols sont dans les `EditorPrefs` sous la classe de fenêtre, et `Presets.config.goTargets` est une liste unique. Le patch rend ce modèle *bon marché*, il ne le restructure pas. Un scoping par scène dans les données serait plus propre, réduirait la taille du banc (618 Ko ici) et supprimerait la notion de cible « hors scène » — au prix d'une migration.
2. **`_optionCache` est toujours vidé à chaque changement de hiérarchie** (D7). Quand le tab References est visible, les listes d'options sont donc reconstruites par réflexion au repaint suivant. Contournement d'usage : ne pas laisser la fenêtre sur ce tab pendant un show. Piste amont : indexer le cache sur le `GlobalObjectId` plutôt que sur l'`InstanceID`, ce qui le rendrait stable à travers les changements de hiérarchie.
3. **Les homonymes inter-scènes subsistent** (`ANCHOR_VR` dans trois scènes). Le scoping protège du mauvais appariement, mais des noms uniques resteraient plus robustes. À arbitrer côté projet, pas côté package.
4. **`identifierType` est comparé à la constante littérale `2`.** Unity n'expose pas d'énumération publique pour ce champ. Si l'amont préfère, un helper nommé serait plus lisible, mais la valeur est stable depuis 2018.3.

Hors périmètre de ces packages, relevé pendant l'investigation et laissé au projet :

- avertissements `XR_ERROR_ACTIONSET_NOT_ATTACHED` émis chaque frame par `OVRHaptics.Config.Load()` depuis `OVRManager.LateUpdate()` (Meta XR SDK) ;
- `03_RAINBOW` rend trois caméras (principale 1920×1080 vers Spout, miroir 1024×1024, VR) ; la Scene View en ajoute une quatrième quand Unity a le focus ;
- un `Debug.Log` latent dans `Update()` de `WalkingController.cs:73`.

## 9. Application sur la machine cible

Les patchs sont au format `diff -u` standard, chemins **relatifs à la racine du package** (le dossier qui contient `package.json`), avec préfixes `a/` et `b/`.

Depuis la racine de chaque dépôt :

```bash
git apply --check kontrols-0.5.1--scene-scoped-binding-resolution.patch && git apply kontrols-0.5.1--scene-scoped-binding-resolution.patch
```

```bash
git apply --check presets-0.3.0--scene-scoped-target-resolution.patch && git apply presets-0.3.0--scene-scoped-target-resolution.patch
```

Ou, sans git :

```bash
patch -p1 --dry-run < kontrols-0.5.1--scene-scoped-binding-resolution.patch
```

Si la racine du dépôt n'est pas celle du package (par exemple le package vit dans un sous-dossier), ajuster le niveau : `-p0`, `-p2`… ou appliquer depuis le dossier du package.

**Bases de départ** : `com.blindspot.kontrols` 0.5.1 et `com.blindspot.presets` 0.3.0, telles que distribuées. Si l'amont a divergé depuis, les hunks de `SceneObjects.cs` et du bloc `SubscribeReferences` / `OnHierarchyChanged` sont les plus susceptibles de nécessiter une reprise manuelle — ce sont les seules zones réécrites en profondeur.

Les patchs ont été validés par application sur des copies vierges des deux packages : le résultat est **identique octet pour octet** aux fichiers compilés et mesurés sur la machine d'origine.

## 10. Vérification après intégration

1. **Compilation** : aucune erreur attendue. Vérifier l'absence de `error CS` dans la Console et dans `Editor.log`.
2. **Non-régression du câblage** — c'est le point sensible, à faire avant toute mesure :
   - ouvrir la fenêtre Kontrols, tab References : les bindings de la scène **chargée** doivent apparaître résolus comme avant ;
   - ceux des scènes déchargées restent non résolus — comportement inchangé, mais ils doivent **redevenir résolus après un changement de scène** (c'est ce que valide D6) ;
   - enchaîner deux rappels de presets qui changent de scène dans les deux sens, et confirmer que les bindings de chaque scène reprennent vie à son chargement.
3. **Mesure du gain**, après une session de travail normale :

```bash
grep -c "manager != NULL" "$LOCALAPPDATA/Unity/Editor/Editor.log"; grep -c "not found — skipped" "$LOCALAPPDATA/Unity/Editor/Editor.log"
```

Attendu : `0` et `0`. Référence avant patch sur la machine d'origine : `868` et `283`.

4. **Taille du log** : indicateur indirect mais parlant. 2,4 Mo pour 78 rappels de presets après patch, contre 10 Mo pour ~230 rappels avant.

## 11. Rollback

Sur la machine d'origine, sauvegarde complète des deux packages avant modification :

```
C:/Users/chamb/Documents/GitHub/NewVersions/_backup-20261012-175423/
├── Kontrols-0.5.1/
└── Presets-0.3.0/
```

Sur la machine cible, `git apply -R <patch>` ou `patch -p1 -R < <patch>` annule proprement chaque patch.

---

## Annexe — fichiers modifiés

| Dépôt | Fichier | + | − |
|---|---|---|---|
| Kontrols 0.5.1 | `Editor/KontrolsWindow.References.cs` | 117 | 4 |
| Presets 0.3.0 | `Editor/SceneObjects.cs` | 88 | 2 |
| Presets 0.3.0 | `Editor/GameObjectCapture.cs` | 8 | 2 |
| Presets 0.3.0 | `Editor/PresetsWindow.cs` | 8 | 1 |

Aucun autre fichier touché. Aucune dépendance ajoutée. Aucun membre public supprimé ou modifié.

**Validation des patchs** (faite sur la machine d'origine, sur des copies vierges des deux packages) :

- `patch -p1` : applique les 4 fichiers sans rejet ;
- `git apply --check` puis `git apply` : applique les 4 fichiers sans rejet, diffstat conforme au tableau ci-dessus ;
- comparaison octet pour octet du résultat avec les fichiers effectivement compilés et mesurés : **identiques**.
