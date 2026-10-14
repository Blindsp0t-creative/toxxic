# Colonnes redimensionnables à la souris — Kontrols 0.5.1

**Date** : 14 octobre 2026
**Projet d'origine** : `toxxic` (Unity 6000.3.10f1, HDRP, Windows 11), branche `master`
**Package concerné** :

| Package | Version | Chemin local sur la machine d'origine |
|---|---|---|
| `com.blindspot.kontrols` | 0.5.1 | `C:/Users/chamb/Documents/GitHub/NewVersions/Kontrols-0.5.1/Kontrols-0.5.1` |

Référencé en dépendance locale (`file:`) dans `Packages/manifest.json` du projet. Le patch est **déjà appliqué** sur la machine d'origine ; ce document et le `.patch` qui l'accompagne servent à l'intégrer dans le dépôt amont.

**Fichiers livrés**

```
_patches/2026-10-14-column-resize/
├── RAPPORT.md                                      ← ce document
└── kontrols-0.5.1--mouse-resizable-columns.patch
```

---

## 1. Demande

Pouvoir régler la largeur des colonnes d'un onglet Kontrols **à la souris**, au lieu de passer par l'attribut `[Column(weight)]` dans le code. Deux compléments demandés après le premier jet :

1. pouvoir **réduire la première colonne davantage** ;
2. pouvoir régler la largeur de la **dernière colonne**, celle qui borde la zone grise à droite de la surface.

## 2. État avant patch

`KontrolsWindow.DrawUserTab` (`Editor/KontrolsWindow.cs`) construit la bande de colonnes `1..N` d'un onglet et répartit la largeur disponible au prorata des poids :

```csharp
sumWeights += Mathf.Max(0.0001f, c.Weight);      // somme des [Column(weight)]
...
float avail = Mathf.Max(120f, EditorGUIUtility.currentViewWidth - 24f);
float w     = Mathf.Max(100f, avail * (Mathf.Max(0.0001f, col.Weight) / sumWeights));
```

`Weight` vient de `ColumnAttribute` (`Editor/KontrolsAttributes.cs`), donc :

- il est **compilé dans la classe** : changer une largeur = éditer le script + attendre un domain reload ;
- il est **unique pour toutes les situations d'affichage**, alors que la bonne répartition dépend de l'écran : la même fenêtre est utilisée dockée étroite à côté de l'Inspector et flottante large sur un second moniteur ;
- aucune largeur n'était persistée (seules les *valeurs* des contrôles le sont, via `KontrolsWindow.Persistence.cs`) ;
- la bande occupait **toute** la largeur utile : les poids étant normalisés sur `avail`, aucune disposition ne pouvait laisser de place à droite.

## 3. Décisions de conception

### 3.1 Poids, pas pixels

Le drag écrit dans les **poids**, pas dans des largeurs en pixels. Conséquence : une bande redimensionnée reste proportionnelle quand on redimensionne ensuite la fenêtre — exactement le comportement qu'avaient déjà les `[Column(weight)]`. Une persistance en pixels aurait cassé ça, ou obligé à stocker aussi la largeur de référence.

Conversion : `poids par pixel = sumWeights / avail`.

### 3.2 Un drag ne touche que deux slots

`DragSplitter` prend la paire autour du splitter et **préserve sa somme** :

```csharp
float pair    = weights[left] + weights[right];
float newLeft = Mathf.Clamp(weights[left] + dx * perPixel, minLeft, pair - minRight);
weights[left]  = newLeft;
weights[right] = pair - newLeft;
```

Donc `sumWeights` reste valide pour toute la bande pendant le drag, et **aucun autre slot ne bouge** — c'est ce qu'on attend d'un splitter, et ça évite de recalculer la bande entière à chaque event.

### 3.3 Un slot de plus que de colonnes (point 2 de la demande)

Une bande de `N` colonnes a `N + 1` slots : les colonnes, puis **l'espace vide à droite de la dernière**. Ce slot de fin a un poids (0 par défaut, d'où le comportement d'origine : la bande remplit la largeur), il participe à `sumWeights`, mais rien n'y est dessiné.

Conséquences :

- chaque colonne porte un strip **à sa droite**, la dernière incluse : il y a donc `N` splitters, et le dernier arbitre entre la dernière colonne et le vide. Tirer vers la gauche rétrécit la colonne et agrandit la zone grise, tirer vers la droite fait l'inverse ;
- `SlotMinWidth` donne un plancher de `MinColumnWidth` aux colonnes et de **0** au slot de fin : la zone grise peut se fermer complètement, ce qui redonne exactement l'état d'avant ;
- le vide étant un poids, il reste proportionnel quand la fenêtre change de taille (une bande réglée à 70 % de la largeur reste à 70 %), cohérent avec le reste.

Alternative écartée : stocker une marge droite en pixels. Plus intuitive sur un seul écran, mais elle se comporte mal en docking étroit (la marge mange toute la bande) et aurait introduit une seconde unité dans la même donnée persistée.

### 3.4 Le vrai plancher était le contenu, pas le clamp (point 1 de la demande)

Diagnostic : la première colonne du projet (`kontrolsToxxic01`, onglet `TOXXIC`) contient des `[Slider]`. Une ligne de contrôle est

```
[label 120px fixe][contrôle][bouton CC 72px fixe][poignée LFO 30px]
```

soit un minimum d'environ **300 px**, très au-dessus du clamp de 100 px. Sous ce seuil la ligne ne rétrécissait plus : elle débordait à droite de son groupe à largeur fixe, par-dessus la colonne suivante. Le drag « fonctionnait » (le splitter bougeait) mais la colonne 1 ne maigrissait plus — d'où le ressenti « je ne peux pas la réduire ».

Les deux largeurs qui nous appartiennent dans cette ligne suivent maintenant la colonne :

| Partie | Largeur | Devient |
|---|---|---|
| Label latéral | `120` fixe | `clamp(colW * 0.34, 48, 120)` |
| Bouton MIDI-Learn | `72` fixe | `clamp(colW * 0.22, 36, 72)` |
| Slider | minimum interne d'`EditorGUILayout` (non mesuré ici, largement supérieur à 40 px) | `GUILayout.MinWidth(40)` explicite |

Les bornes hautes sont les valeurs d'origine : une colonne assez large pour elles s'affiche **exactement** comme avant le patch (au-dessus de ~355 px pour le label, ~327 px pour le bouton CC). Seules les colonnes étroites changent d'aspect, et elles changent pour rester lisibles.

`MinColumnWidth` est passé de la constante `100` à une **propriété `protected virtual` à 120 px** : une fenêtre qui veut serrer plus peut l'abaisser sans patcher le package. 120 reste sous le plancher de contenu d'une ligne à slider (~150 px, somme des minimums ci-dessus plus les marges de boîte), donc un chevauchement léger est encore atteignable en bas de course sur ce type de colonne ; une colonne de boutons et de toggles, elle, descend proprement à 120. Le dernier mot reste à l'utilisateur de l'API.

### 3.5 Les attributs restent la source par défaut

L'override persisté **gagne sur l'attribut**, mais ne le remplace pas :

- `[Column(1.4f)]` continue de décrire la mise en page livrée avec le code (et c'est elle qu'on versionne) ;
- la souris n'est qu'un ajustement local, par poste et par écran ;
- double-clic ou clic droit sur un splitter → `ResetColumnLayout` supprime la clé et revient aux attributs (slot de fin remis à 0).

### 3.6 Persistance : clé par onglet, validée par forme

Clé `Kontrols_<PrefsNamespace>_colw_<nomDeLOnglet>`, valeur = poids des slots en CSV (`InvariantCulture`, `F5`), dans la continuité des clés `Kontrols_<ns>_val_<id>` existantes.

`LoadColumnLayout` **rejette** un override dont le nombre de valeurs ne correspond pas à la bande actuelle, ou qui contient une valeur non parsable / `NaN` / négative (le slot de fin accepte `0`, les colonnes non). C'est le point important : si on ajoute ou retire un `[Column]` dans cet onglet, l'ancienne disposition est **abandonnée** (retour aux attributs) plutôt que ré-appliquée de travers sur des colonnes qui ne sont plus les mêmes.

Cas particulier : une disposition enregistrée **une valeur plus courte** que le nombre de slots est une disposition écrite avant l'existence du slot de fin ; elle est reprise avec un vide nul, c'est-à-dire telle qu'elle s'affichait. Ça évite de jeter les réglages faits entre les deux versions du patch.

Écriture au `MouseUp` (et non à `OnDisable`) : la disposition survit à un crash de l'éditeur ou à un kill du process, ce qui arrive en show.

### 3.7 Pourquoi les splitters sont traités *après* la bande

La hauteur d'un strip doit couvrir toute la bande, et cette hauteur n'est connue qu'une fois le groupe horizontal fermé. Plutôt que de parier sur le comportement de `GUILayout.ExpandHeight` dans un groupe horizontal, `DrawUserTab` :

1. réserve le strip avec `GUILayout.Space(SPLITTER_W)` après chaque colonne et note son `x` via `GUILayoutUtility.GetLastRect()` ;
2. ferme le groupe, récupère le rect de la bande avec `GUILayoutUtility.GetLastRect()` ;
3. appelle `HandleColumnSplitters`, qui reconstruit chaque strip en `Rect(gapX[i], bandRect.y, 5, bandRect.height)`, y dessine la poignée, pose `AddCursorRect(ResizeHorizontal)` et traite les events.

Effet de bord bienvenu : les poignées sont dessinées **par-dessus** la bande.

Les rects ne sont pas connus pendant la passe `EventType.Layout` ; tout ce bloc est donc gardé par `laying`.

Pas de test `Contains` sur `MouseDrag` / `MouseUp` : la fenêtre conserve le drag dès que le bouton est descendu sur le strip, le curseur peut donc sortir du strip (et même de la colonne) sans interrompre le redimensionnement.

### 3.8 Largeur prélevée, pas ajoutée

`avail` retire `SPLITTER_W * band.Count` avant la répartition. Les 5 px de chaque strip sortent de la largeur partagée par les slots : la bande occupe la même emprise qu'avant le patch, aucune colonne ne déborde du scroll view à cause de l'ajout.

## 4. Fichiers touchés

| Fichier | Nature |
|---|---|
| `Editor/KontrolsWindow.Layout.cs` | **nouveau** — partial `KontrolsWindow` : poids des slots, persistance, métriques de ligne, dessin + drag des splitters (~255 lignes) |
| `Editor/KontrolsWindow.cs` | `DrawUserTab` : poids lus via `GetColumnWeights`, strip après chaque colonne, largeur de colonne exposée aux contrôles, appel à `HandleColumnSplitters` ; `DrawSlider` / `DrawIntSlider` / `DrawToggle` : label adaptatif, minimum de slider explicite |
| `Editor/KontrolsWindow.Midi.cs` | `DrawLearnButton` : largeur adaptative |
| `README.md` | ligne `[Column]` du tableau d'attributs + puces « Resizing a band » et « Narrow columns » dans *Layout* |
| `CHANGELOG.md` | section `[Unreleased]` : `Added` + `Changed` |

Un seul ajout de surface publique (`MinColumnWidth`, `protected virtual`), aucun nouvel attribut, aucune migration. Une fenêtre dont on ne touche jamais les splitters, et dont les colonnes sont assez larges pour les tailles d'origine, s'affiche comme avant.

Le `.meta` du nouveau fichier est généré par Unity (le package est en `file:`, donc mutable) ; sur la machine d'origine il porte le GUID `7b3c41d5e8a24f6b9c0d58e2174af93b`.

## 5. Vérification

- **Compilation** : l'assembly `Kontrols.Editor` a été recompilée hors éditeur avec le Roslyn d'Unity 6000.3.10f1 et la response file de Bee du projet (`Library/Bee/artifacts/1900b0aE.dag/Kontrols.Editor.rsp`, `-analyzer` retirés) : **0 erreur, 0 warning**. Unity a par ailleurs recompilé l'assembly de son côté sans erreur.
- **Patch** : `patch -p1` sur une copie vierge du package reproduit les cinq fichiers **à l'octet près** (`cmp` identique).
- **Test manuel** attendu dans l'éditeur, onglet `TOXXIC` de `kontrolsToxxic01` (3 colonnes `[Column]`, `[Column(1.4f)]`, `[Column]`) : curseur ↔ sur les trois strips, réduction de la colonne 1 (le label et le bouton CC doivent maigrir au lieu de déborder), drag du strip de droite pour ouvrir/fermer la zone grise, double-clic de reset, réouverture de la fenêtre après recompilation.

## 6. Limites connues

1. **Plancher de contenu** — une ligne tenant un slider ne descend pas sous ~150 px (label 48 + slider 40 + CC 36 + poignée 30 + marges), alors que `MinColumnWidth` autorise 120 : sur une colonne à sliders, les derniers 30 px de course peuvent faire déborder la ligne sur sa voisine. Valeur gardée volontairement basse parce que les colonnes sans slider, elles, s'affichent correctement à 120 ; à ajuster par override selon le contenu. Aller plus bas proprement demanderait un vrai mode compact (label au-dessus du contrôle, bouton CC en icône), qui est une autre décision de design.
2. **Fenêtre très étroite** — quand `avail / sumWeights` descend sous `MinColumnWidth` pour une colonne, le plancher du rendu reprend le dessus : la somme des colonnes dépasse `avail` et le ratio n'est plus respecté. C'était déjà le cas avant le patch ; le drag refuse alors de redistribuer (`pair <= minLeft + minRight`) mais ne corrige pas la situation.
3. **Débordement volontaire** — élargir une colonne au-delà de la largeur utile produit une scrollbar horizontale (comportement du `BeginScrollView` de `OnGUI`), pas un reflow.
4. **Portée de l'override** — la clé est `(PrefsNamespace, nom d'onglet)`. Deux onglets homonymes dans la même fenêtre partageraient la disposition ; `KontrolsReflection` ne crée pas d'onglets homonymes, donc le cas ne se présente pas en pratique.
5. **Renommer un onglet** perd sa disposition (nouvelle clé) — même compromis que les clés de valeurs, qui sont indexées par `id`.
6. **Colonne 0** (zone pleine largeur avant le premier `[Column]`) n'est pas concernée : elle n'appartient pas à la bande, donc pas de splitter.
