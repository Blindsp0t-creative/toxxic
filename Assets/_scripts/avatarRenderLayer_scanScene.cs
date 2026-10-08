using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene SCAN_V1 : montre / cache un avatar (CLARA_RETOPOLOGY_RIG_PAINTING_EXPORT)
/// dans la camera principale (le CinemachineBrain) en changeant son layer de rendu.
///
/// Le layer "cache" est retire du culling mask de la camera principale au demarrage,
/// donc les autres cameras (MIRROR CAMERA, POV_JEN_Camera...) continuent de voir
/// l'avatar si leur propre culling mask inclut ce layer.
///
/// showAvatarInMainCamera() / hideAvatarInMainCamera() sont appelables depuis un
/// UnityEvent, une Timeline, l'OSC, ou le menu contextuel du composant.
/// </summary>
public class avatarRenderLayer_scanScene : MonoBehaviour
{
    [Header("Avatar")]
    [Tooltip("Racine de l'avatar. Si vide : recherche par nom dans la scene.")]
    public GameObject avatarRoot;
    public string avatarName = "CLARA_RETOPOLOGY_RIG_PAINTING_EXPORT";

    [Header("Camera principale")]
    [Tooltip("Si vide : objet nomme ci-dessous, sinon Camera.main.")]
    public Camera mainCamera;
    public string mainCameraName = "CinemachineBrain";

    [Header("Layers")]
    [Tooltip("Layer pose sur tout l'avatar quand il est cache (hors culling mask de la cam principale).")]
    public string hiddenLayerName = "HIDDEN_FROM_MAIN_CAM";
    [Tooltip("Au show : remet chaque objet sur son layer d'origine (recommande, preserve les layers de colliders / IK).")]
    public bool restoreOriginalLayers = true;
    [Tooltip("Layer pose sur tout l'avatar au show si restoreOriginalLayers est decoche.")]
    public string visibleLayerName = "Default";

    [Header("Etat")]
    [Tooltip("Pilotable en direct (inspector / OSC). Coche = visible dans la camera principale.")]
    public bool visibleInMainCamera = true;

    readonly List<Transform> savedTransforms = new List<Transform>();
    readonly List<int> savedLayers = new List<int>();

    int hiddenLayer = -1;
    int visibleLayer = -1;
    bool initDone;
    bool ready;
    bool lastApplied = true;

    public bool IsVisibleInMainCamera => visibleInMainCamera;

    void Awake()
    {
        Init();
    }

    void Start()
    {
        // Applique l'etat initial choisi dans l'inspector
        if (!visibleInMainCamera) hideAvatarInMainCamera();
        lastApplied = visibleInMainCamera;
    }

    void Update()
    {
        // Permet de piloter le bool depuis l'inspector / l'OSC
        if (visibleInMainCamera != lastApplied)
            ApplyState(visibleInMainCamera);
    }

    void Init()
    {
        if (initDone) return;
        initDone = true;

        if (avatarRoot == null)
            avatarRoot = GameObject.Find(avatarName);

        if (mainCamera == null)
        {
            GameObject camGo = string.IsNullOrEmpty(mainCameraName) ? null : GameObject.Find(mainCameraName);
            mainCamera = camGo != null ? camGo.GetComponent<Camera>() : null;
            if (mainCamera == null) mainCamera = Camera.main;
        }

        hiddenLayer = LayerMask.NameToLayer(hiddenLayerName);
        visibleLayer = LayerMask.NameToLayer(visibleLayerName);

        if (avatarRoot == null)
            Debug.LogError($"[avatarRenderLayer] Avatar \"{avatarName}\" introuvable dans la scene.", this);
        if (mainCamera == null)
            Debug.LogError("[avatarRenderLayer] Camera principale introuvable.", this);
        if (hiddenLayer < 0)
            Debug.LogError($"[avatarRenderLayer] Layer \"{hiddenLayerName}\" inexistant (Project Settings > Tags and Layers).", this);
        if (!restoreOriginalLayers && visibleLayer < 0)
            Debug.LogError($"[avatarRenderLayer] Layer \"{visibleLayerName}\" inexistant (Project Settings > Tags and Layers).", this);

        // La camera principale ne doit jamais rendre le layer "cache"
        if (mainCamera != null && hiddenLayer >= 0)
            mainCamera.cullingMask &= ~(1 << hiddenLayer);

        ready = avatarRoot != null && hiddenLayer >= 0;
    }

    /// <summary>Rend l'avatar visible dans la camera principale (layers d'origine restaures).</summary>
    [ContextMenu("Show avatar in main camera")]
    public void showAvatarInMainCamera()
    {
        Init();
        visibleInMainCamera = true;
        lastApplied = true;
        if (!ready) return;

        if (restoreOriginalLayers)
        {
            // Rien en cache = l'avatar n'a jamais ete cache, il est deja sur ses layers d'origine
            for (int i = 0; i < savedTransforms.Count; i++)
            {
                if (savedTransforms[i] != null)
                    savedTransforms[i].gameObject.layer = savedLayers[i];
            }
        }
        else if (visibleLayer >= 0)
        {
            SetLayerRecursive(avatarRoot.transform, visibleLayer);
        }

        savedTransforms.Clear();
        savedLayers.Clear();

        // La camera principale doit voir les layers de l'avatar
        if (mainCamera != null)
            mainCamera.cullingMask |= AvatarLayerMask();

        Log("visible");
    }

    /// <summary>Passe tout l'avatar sur le layer cache : plus rendu par la camera principale.</summary>
    [ContextMenu("Hide avatar in main camera")]
    public void hideAvatarInMainCamera()
    {
        Init();
        visibleInMainCamera = false;
        lastApplied = false;
        if (!ready) return;

        // Memorise les layers actuels pour pouvoir les restaurer au show
        savedTransforms.Clear();
        savedLayers.Clear();
        SaveLayersRecursive(avatarRoot.transform);

        SetLayerRecursive(avatarRoot.transform, hiddenLayer);

        Log("cache");
    }

    /// <summary>Inverse l'etat courant.</summary>
    public void toggleAvatarInMainCamera()
    {
        ApplyState(!visibleInMainCamera);
    }

    /// <summary>Version parametree, pratique pour un UnityEvent (bool).</summary>
    public void setAvatarVisibleInMainCamera(bool visible)
    {
        ApplyState(visible);
    }

    void ApplyState(bool visible)
    {
        if (visible) showAvatarInMainCamera();
        else hideAvatarInMainCamera();
    }

    int AvatarLayerMask()
    {
        int mask = 0;
        var transforms = avatarRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
            mask |= 1 << transforms[i].gameObject.layer;

        // ne jamais reactiver le layer cache sur la camera principale
        if (hiddenLayer >= 0) mask &= ~(1 << hiddenLayer);
        return mask;
    }

    void SaveLayersRecursive(Transform t)
    {
        savedTransforms.Add(t);
        savedLayers.Add(t.gameObject.layer);
        for (int i = 0; i < t.childCount; i++)
            SaveLayersRecursive(t.GetChild(i));
    }

    static void SetLayerRecursive(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursive(t.GetChild(i), layer);
    }

    void Log(string etat)
    {
        Debug.Log($"[avatarRenderLayer] {avatarRoot.name} : {etat} dans {(mainCamera != null ? mainCamera.name : "?")}");
    }
}
