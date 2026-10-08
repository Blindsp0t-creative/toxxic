using System;
using UnityEngine;
using UnityEditor;
using Kontrols;

// Migre vers l'API Kontrols 0.4.0+ : tout controle vit sur un champ, les
// boutons sont des System.Action. Les ids sont inchanges, donc les mappings
// MIDI, les adresses OSC et les bindings du tab References sont conserves.
//
// Layout : 3 colonnes. GENERAL | STRIP CLUB | PELLETEUSE + RAINBOW ROAD.
// La section BARDELLAX, commentee de longue date, a ete retiree (voir git).

public class kontrolsToxxic01 : KontrolsWindow
{
    [MenuItem("Tools/ToxxicKontrols")]
    static void Open() => GetWindow<kontrolsToxxic01>("ToxxicKontrols");

    // ── Colonne 1 — GENERAL ──────────────────────────────────────────

    [Tab("TOXXIC")]
    [Column]
    [Section("GENERAL")]
    [Button("CALIBRATE", id: "calbibrate")]
    public Action calibrate;

    [Toggle("BLACK OUT", id: "blackout")]
    public bool blackOut;

    [SameLine]
    [Toggle("FOOT LOCK", id: "footRotation")]
    public bool footRotationClamp;

    [Slider("ElevationAvatarDenis", -0.5f, 4.5f, id: "elevationavatarDenis")]
    public float avatarElevationDenis;

    [Slider("ElevationAvatarJen", 0.0f, 2.0f, id: "elevationavatarJen")]
    public float avatarElevationJen;

    [Slider("ElevationAvatarJenBarde", -1.0f, 2.0f, id: "elevationavatarJenBarde")]
    public float avatarElevationJenBarde;

    [Slider("Dissolve EVG Avatar", 0.0f, 1.0f, id: "dissolveEVGAvatar")]
    public float dissolveAvatarStripClub;

    [Section("PRESETS")]
    [Button("PREV PRESET", id: "presetPrev")]
    public Action presetPrevious;

    [SameLine]
    [Button("NEXT PRESET", id: "presetNext")]
    public Action presetNext;

    // ── Colonne 2 — STRIP CLUB ───────────────────────────────────────

    [Column(1.4f)]
    [Section("STRIP CLUB")]
    [Button("Top Scene StripClub", id: "stripclub")]
    public Action topSceneStripClub;

    [Button("Top Plateforme", id: "toggleAlarm")]
    public Action topPlateforme;

    [Button("Zoom", id: "stripclubZoom")]
    public Action camZoom;

    [Button("Dolly Main", id: "stripclubDM")]
    public Action camDollyMain;

    //[SameLine]
    [Button("Dolly Main 2", id: "stripclubDM2")]
    public Action camDollyMain2;

    [Button("Dolly Public", id: "stripclubDP")]
    public Action camDollyPublic;

    //[SameLine]
    [Button("Dolly Public 2", id: "stripclubDP2")]
    public Action camDollyPublic2;

    [Button("DollySol", id: "dollySol")]
    public Action camDollySol;

    //[SameLine]
    [Button("DollySol2", id: "dollySol2")]
    public Action camDollySol2;

    [Button("Reverse POV", id: "stripclubReversePOV")]
    public Action camReversePOV;

    //[SameLine]
    [Button("Reverse POV2", id: "stripclubReversePOV2")]
    public Action camReversePOV2;

    // ── Colonne 3 — PELLETEUSE + RAINBOW ROAD ────────────────────────

    [Column]
    [Section("PELLETEUSE")]
    [Button("Top Scene Pelleteuse", id: "pelleteuse")]
    public Action topScenePelleteuse;


    [Button("Previous", id: "pelleteuseP")]
    public Action pelleteusePrevious;
    [SameLine]
    [Button("Next", id: "pelleteuseN")]
    public Action pelleteuseNext;


    [Button("Top Photo", id: "togglePhoto")]
    public Action topPhoto;

    [Toggle("accrochePelle", id: "accrochePelleteuse")]
    public bool accPelleteuse;

    [Section("RAINBOW ROAD")]
    [Button("Top Scene RainbowRoad", id: "rainbowroad")]
    public Action topSceneRainbowRoad;


    [Button("Previous", id: "RainbowRoadP")]
    public Action rainbowRoadPrevious;
    [SameLine]
    [Button("Next", id: "RainbowRoadN")]
    public Action rainbowRoadNext;


    [Button("Top Video TV", id: "toggleVideoTV")]
    public Action topVideoTV;

    [Toggle("Disco Ball", id: "dicoBall")]
    public bool showDiscoBall;

    [Toggle("Avatar Jen", id: "avatarJenVisibility")]
    public bool avatarJenVisibility;

    [Toggle("Mirror Jen Visibility", id: "JenMirrorVisibility")]
    public bool jenMirrorVisibility;

    // ── Handlers ─────────────────────────────────────────────────────
    //
    // Unity ne serialise pas les delegates : ils sont reassignes a chaque
    // domain reload, d'ou OnEnable plutot qu'un initialiseur de champ.
    // Le vrai travail passe par le tab References — ces handlers ne sont
    // que la trace console, et les bindings tirent meme si le champ est null.

    protected override void OnEnable()
    {
        // Ces deux-la font le vrai travail, contrairement aux autres handlers :
        // ils appellent directement l'API du Preset Bank, sans binding dans
        // l'onglet References — un preset est un objet de l'editeur, pas de la
        // scene. La fenetre Presets doit etre ouverte, comme pour un rappel MIDI.
        //
        // 'global::' est indispensable : 'using UnityEditor' met UnityEditor.Presets
        // (le systeme de Presets d'Unity) dans la portee, et 'Presets.PresetsWindow'
        // s'y resoudrait en premier.
        presetPrevious        = global::Presets.PresetsWindow.RecallPrevious;
        presetNext            = global::Presets.PresetsWindow.RecallNext;

        calibrate             = () => Debug.Log("CALIB");

        topSceneStripClub     = () => Debug.Log("load Strip Club Scene");
        topPlateforme         = () => Debug.Log("Top Plateforme - Strip Club Scene");
        camZoom               = () => Debug.Log("Camera : Zoom");
        camDollyMain          = () => Debug.Log("Camera : Dolly Main");
        camDollyMain2         = () => Debug.Log("Camera : Dolly Main 2");
        camDollyPublic        = () => Debug.Log("Camera : Dolly Public");
        camDollyPublic2       = () => Debug.Log("Camera : Dolly Public 2");
        camDollySol           = () => Debug.Log("Camera : DollySol");
        camDollySol2          = () => Debug.Log("Camera : DollySol2");
        camReversePOV         = () => Debug.Log("Camera : Reverse POV");
        camReversePOV2        = () => Debug.Log("Camera : Reverse POV2");

        topScenePelleteuse    = () => Debug.Log("load Pelleteuse Scene");
        pelleteuseNext        = () => Debug.Log("Next - Pelleteuse Scene");
        pelleteusePrevious    = () => Debug.Log("Previous - Pelleteuse Scene");
        topPhoto              = () => Debug.Log("Top Photo - Pelleteuse Scene");

        topSceneRainbowRoad   = () => Debug.Log("load Rainbow Road Scene");
        rainbowRoadNext       = () => Debug.Log("Next - RainbowRoad Scene");
        rainbowRoadPrevious   = () => Debug.Log("Previous - RainbowRoad Scene");
        topVideoTV            = () => Debug.Log("Top TV - RainbowRoad Scene");

        // Indispensable : la base construit le modele, charge les valeurs,
        // les mappings MIDI/OSC et les references, puis s'abonne aux hubs.
        base.OnEnable();
    }
}
