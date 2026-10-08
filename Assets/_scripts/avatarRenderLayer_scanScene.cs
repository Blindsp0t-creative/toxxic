using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene 03_RAINBOW (ex SCAN_V1) : pilote la visibilite de l'avatar JEN
/// (CLARA_RETOPOLOGY_RIG_PAINTING_EXPORT) par layer de rendu.
/// L'avatar reste TOUJOURS visible en VR.
///
/// Les deux toggles de la kontrol surface :
///   avatarJenVisibility ON                        -> visible mainCamera + miroir + VR
///   avatarJenVisibility OFF + jenMirrorVisibility ON  -> cache mainCamera, visible miroir + VR
///   avatarJenVisibility OFF + jenMirrorVisibility OFF -> visible VR uniquement
/// (jenMirrorVisibility n'est lu que quand avatarJenVisibility est OFF)
///
/// Un objet n'a qu'un seul layer, donc chaque etat = un layer :
///   visible partout -> layers d'origine (Default...), vus par les 3 cameras
///   cache main      -> HIDDEN_FROM_MAIN_CAM (hors mask mainCamera, dans mask miroir + VR)
///   VR seulement    -> VR_ONLY              (hors mask mainCamera et miroir, dans mask VR)
///
/// Les culling masks des 3 cameras sont corriges par ce script, pas besoin de les
/// cocher a la main. La camera VR (CenterEyeAnchor) vit dans 00_MASTER_, chargee
/// en additif : si elle n'est pas encore la, le script retente a chaque changement.
///
/// Toutes les methodes publiques sont appelables depuis un UnityEvent, une Timeline,
/// l'OSC, la kontrol surface, ou le menu contextuel du composant.
/// </summary>
public class avatarRenderLayer_scanScene : MonoBehaviour
{
    [Header("Avatar")]
    [Tooltip("Racine de l'avatar. Si vide : recherche par nom dans la scene.")]
    public GameObject avatarRoot;
    public string avatarName = "CLARA_RETOPOLOGY_RIG_PAINTING_EXPORT";

    [Header("Cameras")]
    [Tooltip("Camera de rendu principale. Si vide : objet nomme ci-dessous, sinon Camera.main.")]
    public Camera mainCamera;
    public string mainCameraName = "CinemachineBrain";
    [Tooltip("Camera du miroir. Si vide : objet nomme ci-dessous.")]
    public Camera mirrorCamera;
    public string mirrorCameraName = "MIRROR CAMERA";
    [Tooltip("Cameras VR : elles voient l'avatar dans tous les cas. Si vide : objets nommes ci-dessous (00_MASTER_ charge en additif).")]
    public Camera[] vrCameras;
    public string[] vrCameraNames = { "CenterEyeAnchor" };

    [Header("Layers")]
    [Tooltip("Avatar cache de la mainCamera, toujours visible dans le miroir et en VR.")]
    public string hiddenFromMainLayerName = "HIDDEN_FROM_MAIN_CAM";
    [Tooltip("Avatar visible en VR uniquement.")]
    public string vrOnlyLayerName = "VR_ONLY";
    [Tooltip("Retour en visible partout : remet chaque objet sur son layer d'origine (recommande, preserve les layers de colliders / IK).")]
    public bool restoreOriginalLayers = true;
    [Tooltip("Layer pose sur tout l'avatar en visible partout, si restoreOriginalLayers est decoche.")]
    public string visibleLayerName = "Default";

    [Header("Kontrol surface")]
    [Tooltip("Toggle \"Avatar Jen\" (id avatarJenVisibility). ON = visible mainCamera + miroir + VR.")]
    public bool avatarJenVisibility = true;
    [Tooltip("Toggle \"Mirror Jen Visibility\" (id JenMirrorVisibility). Lu uniquement quand Avatar Jen est OFF.")]
    public bool jenMirrorVisibility = true;

    readonly List<Transform> savedTransforms = new List<Transform>();
    readonly List<int> savedLayers = new List<int>();
    readonly List<Camera> resolvedVrCameras = new List<Camera>();

    int hiddenFromMainLayer = -1;
    int vrOnlyLayer = -1;
    int visibleLayer = -1;

    bool initDone;
    bool ready;
    bool vrMasksDone;
    float nextVrRetry;
    bool lastAvatar = true;
    bool lastMirror = true;

    /// <summary>Etat courant : l'avatar est-il rendu par la camera principale.</summary>
    public bool IsVisibleInMainCamera => avatarJenVisibility;

    /// <summary>Etat courant : l'avatar est-il rendu par le miroir.</summary>
    public bool IsVisibleInMirrorCamera => avatarJenVisibility || jenMirrorVisibility;

    void Awake()
    {
        Init();
    }

    void Start()
    {
        // Applique l'etat initial choisi dans l'inspector / la kontrol surface
        ApplyLayers();
    }

    void Update()
    {
        // Permet de piloter les bools depuis l'inspector / l'OSC / la kontrol surface
        if (avatarJenVisibility != lastAvatar || jenMirrorVisibility != lastMirror)
            ApplyLayers();

        // La camera VR peut arriver plus tard (00_MASTER_ chargee en additif)
        if (!vrMasksDone && Time.unscaledTime >= nextVrRetry)
        {
            nextVrRetry = Time.unscaledTime + 1f;
            SetupVrCameras();
        }
    }

    void Init()
    {
        if (initDone) return;
        initDone = true;

        if (avatarRoot == null)
            avatarRoot = GameObject.Find(avatarName);

        if (mainCamera == null)
        {
            mainCamera = FindCamera(mainCameraName);
            if (mainCamera == null) mainCamera = Camera.main;
        }
        if (mirrorCamera == null)
            mirrorCamera = FindCamera(mirrorCameraName);

        hiddenFromMainLayer = LayerMask.NameToLayer(hiddenFromMainLayerName);
        vrOnlyLayer = LayerMask.NameToLayer(vrOnlyLayerName);
        visibleLayer = LayerMask.NameToLayer(visibleLayerName);

        if (avatarRoot == null)
            Debug.LogError($"[avatarRenderLayer] Avatar \"{avatarName}\" introuvable dans la scene.", this);
        if (mainCamera == null)
            Debug.LogError("[avatarRenderLayer] Camera principale introuvable.", this);
        if (mirrorCamera == null)
            Debug.LogError($"[avatarRenderLayer] Camera \"{mirrorCameraName}\" introuvable.", this);
        CheckLayer(hiddenFromMainLayer, hiddenFromMainLayerName);
        CheckLayer(vrOnlyLayer, vrOnlyLayerName);
        if (!restoreOriginalLayers) CheckLayer(visibleLayer, visibleLayerName);

        // mainCamera : ne rend ni le layer "cache main" ni le layer "VR only"
        if (mainCamera != null)
            mainCamera.cullingMask &= ~(Bit(hiddenFromMainLayer) | Bit(vrOnlyLayer));

        // miroir : rend le layer "cache main", mais pas le layer "VR only"
        if (mirrorCamera != null)
        {
            mirrorCamera.cullingMask |= Bit(hiddenFromMainLayer);
            mirrorCamera.cullingMask &= ~Bit(vrOnlyLayer);
        }

        ready = avatarRoot != null && hiddenFromMainLayer >= 0 && vrOnlyLayer >= 0;

        SetupVrCameras();
    }

    /// <summary>
    /// Les cameras VR doivent voir les deux layers "caches". Retente tant qu'elles
    /// n'ont pas ete trouvees : 00_MASTER_ peut etre chargee apres cette scene.
    /// </summary>
    void SetupVrCameras()
    {
        if (vrMasksDone)
        {
            // Si la scene VR a ete rechargee, les cameras memorisees sont mortes : on refait
            bool allAlive = resolvedVrCameras.Count > 0;
            for (int i = 0; i < resolvedVrCameras.Count; i++)
                if (resolvedVrCameras[i] == null) allAlive = false;

            if (allAlive) return;
            vrMasksDone = false;
        }

        resolvedVrCameras.Clear();

        if (vrCameras != null)
        {
            for (int i = 0; i < vrCameras.Length; i++)
                if (vrCameras[i] != null) resolvedVrCameras.Add(vrCameras[i]);
        }

        if (resolvedVrCameras.Count == 0 && vrCameraNames != null)
        {
            for (int i = 0; i < vrCameraNames.Length; i++)
            {
                Camera cam = FindCamera(vrCameraNames[i]);
                if (cam != null) resolvedVrCameras.Add(cam);
            }
        }

        if (resolvedVrCameras.Count == 0) return;

        int bits = Bit(hiddenFromMainLayer) | Bit(vrOnlyLayer);
        for (int i = 0; i < resolvedVrCameras.Count; i++)
            resolvedVrCameras[i].cullingMask |= bits;

        vrMasksDone = true;
    }

    // ---------- Toggle "Avatar Jen" ----------

    /// <summary>Avatar Jen ON : visible mainCamera + miroir + VR.</summary>
    [ContextMenu("Show avatar in main camera")]
    public void showAvatarInMainCamera()
    {
        avatarJenVisibility = true;
        ApplyLayers();
    }

    /// <summary>Avatar Jen OFF : cache de la mainCamera (le miroir suit jenMirrorVisibility, la VR garde l'avatar).</summary>
    [ContextMenu("Hide avatar in main camera")]
    public void hideAvatarInMainCamera()
    {
        avatarJenVisibility = false;
        ApplyLayers();
    }

    public void toggleAvatarInMainCamera()
    {
        avatarJenVisibility = !avatarJenVisibility;
        ApplyLayers();
    }

    /// <summary>Version parametree, pratique pour un UnityEvent / un toggle (bool).</summary>
    public void setAvatarVisibleInMainCamera(bool visible)
    {
        avatarJenVisibility = visible;
        ApplyLayers();
    }

    // ---------- Toggle "Mirror Jen Visibility" ----------

    /// <summary>Mirror Jen ON : quand Avatar Jen est OFF, l'avatar reste visible dans le miroir.</summary>
    [ContextMenu("Show avatar in mirror camera")]
    public void showAvatarInMirrorCamera()
    {
        jenMirrorVisibility = true;
        ApplyLayers();
    }

    /// <summary>Mirror Jen OFF : quand Avatar Jen est OFF, l'avatar n'est plus visible que en VR.</summary>
    [ContextMenu("Hide avatar in mirror camera")]
    public void hideAvatarInMirrorCamera()
    {
        jenMirrorVisibility = false;
        ApplyLayers();
    }

    public void toggleAvatarInMirrorCamera()
    {
        jenMirrorVisibility = !jenMirrorVisibility;
        ApplyLayers();
    }

    /// <summary>Version parametree, pratique pour un UnityEvent / un toggle (bool).</summary>
    public void setAvatarVisibleInMirrorCamera(bool visible)
    {
        jenMirrorVisibility = visible;
        ApplyLayers();
    }

    // ---------- Raccourcis ----------

    /// <summary>Visible partout (mainCamera + miroir + VR).</summary>
    [ContextMenu("Show avatar everywhere")]
    public void showAvatarEverywhere()
    {
        avatarJenVisibility = true;
        jenMirrorVisibility = true;
        ApplyLayers();
    }

    /// <summary>Visible en VR uniquement.</summary>
    [ContextMenu("Show avatar in VR only")]
    public void showAvatarInVrOnly()
    {
        avatarJenVisibility = false;
        jenMirrorVisibility = false;
        ApplyLayers();
    }

    // ---------- Application ----------

    void ApplyLayers()
    {
        Init();
        SetupVrCameras();

        lastAvatar = avatarJenVisibility;
        lastMirror = jenMirrorVisibility;
        if (!ready) return;

        if (avatarJenVisibility)
        {
            // Visible partout : retour aux layers d'origine
            if (restoreOriginalLayers)
            {
                // Liste vide = l'avatar n'a jamais ete cache, il est deja sur ses layers d'origine
                for (int i = 0; i < savedTransforms.Count; i++)
                {
                    if (savedTransforms[i] != null)
                        savedTransforms[i].gameObject.layer = savedLayers[i];
                }
                savedTransforms.Clear();
                savedLayers.Clear();
            }
            else if (visibleLayer >= 0)
            {
                SetLayerRecursive(avatarRoot.transform, visibleLayer);
            }
        }
        else
        {
            // Memorise les layers d'origine la premiere fois qu'on quitte l'etat "visible partout"
            if (savedTransforms.Count == 0)
                SaveLayersRecursive(avatarRoot.transform);

            SetLayerRecursive(avatarRoot.transform,
                              jenMirrorVisibility ? hiddenFromMainLayer : vrOnlyLayer);
        }

        Debug.Log($"[avatarRenderLayer] {avatarRoot.name} : " +
                  $"main={(IsVisibleInMainCamera ? "ON" : "OFF")} " +
                  $"miroir={(IsVisibleInMirrorCamera ? "ON" : "OFF")} " +
                  $"VR=ON ({resolvedVrCameras.Count} cam VR)");
    }

    // ---------- Utilitaires ----------

    static int Bit(int layer) => layer >= 0 ? 1 << layer : 0;

    static Camera FindCamera(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        GameObject go = GameObject.Find(name);
        return go != null ? go.GetComponent<Camera>() : null;
    }

    void CheckLayer(int layer, string name)
    {
        if (layer < 0)
            Debug.LogError($"[avatarRenderLayer] Layer \"{name}\" inexistant (Project Settings > Tags and Layers).", this);
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
}
