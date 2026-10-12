using System.Collections;
using UnityEngine;

/// <summary>
/// Fixe la hauteur des yeux du joueur en decalant le TrackingSpace sur son axe Y LOCAL.
/// A poser sur TrackingSpace (enfant de OVRCameraRig).
///
/// Suppose une origine de tracking au sol : OVRManager > Tracking Origin Type = Stage
/// (ou Floor Level). centerEyeAnchor.localPosition.y vaut alors la hauteur reelle des
/// yeux au-dessus du sol, valeur stable d'un lancement a l'autre -- contrairement a
/// Eye Level, ou elle repart de zero sur la posture du recentrage.
///
/// La mesure est un INSTANTANE pris a la calibration, jamais recalcule en continu :
/// l'offset reste constant, donc le joueur garde toute son amplitude verticale
/// (s'accroupir, se pencher). Recalculer l'offset chaque frame annulerait justement
/// ce mouvement, en remontant le TrackingSpace de ce que la tete descend.
/// </summary>
public class Set_VR_Avatar_height : MonoBehaviour
{
    [Header("Hauteur des yeux visee (m)")]
    [Range(-4.5f, 4.5f)]
    [Tooltip("Hauteur a laquelle les yeux se posent apres calibration, mesuree depuis " +
             "la racine de l'OVRCameraRig, joueur debout.")]
    public float height = 1.9f;

    [Tooltip("0 = instantane. Sinon, lissage en secondes.")]
    public float smoothTime = 0f;

    [Header("Calibration")]
    [Tooltip("Calibre au demarrage des que le casque renvoie une pose valide. Suppose le " +
             "joueur debout a ce moment la -- sinon, passer par le bouton CALIBRATE.")]
    public bool calibrateOnStart = true;

    [Tooltip("Delai max d'attente d'une pose casque valide au demarrage, en secondes.")]
    public float calibrationTimeout = 15f;

    [Tooltip("Hauteur des yeux supposee tant qu'aucune calibration n'a reussi " +
             "(casque eteint, Link non connecte, lancement sans HMD).")]
    public float assumedEyeHeight = 1.7f;

    [Tooltip("En-dessous de cette valeur, la pose est jugee non initialisee ou le casque " +
             "n'est pas porte : la calibration est refusee.")]
    public float minValidEyeHeight = 0.5f;

    /// <summary>
    /// Hauteur des yeux relevee a la derniere calibration. Figee : c'est elle qui
    /// preserve l'accroupissement.
    /// </summary>
    public float MeasuredEyeHeight { get; private set; }

    /// <summary>true des qu'une calibration a reussi. Sinon on tourne sur assumedEyeHeight.</summary>
    public bool IsCalibrated { get; private set; }

    const string MissingAnchorMessage =
        "[Set_VR_Avatar_height] CenterEyeAnchor introuvable : ce composant doit vivre sur le " +
        "TrackingSpace d'un OVRCameraRig.";

    Transform _centerEye;
    float _current;
    float _velocity;

    // Offset applique au TrackingSpace. height reste libre de bouger a la volee
    // (slider, OSC, UnityEvent) : seule MeasuredEyeHeight est figee.
    float OffsetY => height - MeasuredEyeHeight;

    void Awake()
    {
        MeasuredEyeHeight = assumedEyeHeight;
        _current = OffsetY;
        ResolveCenterEye();
    }

    void Start()
    {
        if (calibrateOnStart)
            StartCoroutine(CalibrateWhenPoseIsValid());
    }

    void LateUpdate()
    {
        float target = OffsetY;

        // Lissage optionnel pour un reglage a la volee sans a-coups
        _current = (smoothTime <= 0f)
            ? target
            : Mathf.SmoothDamp(_current, target, ref _velocity, smoothTime);

        // localPosition : on ne touche QUE l'axe Y, en local.
        // x et z restent a 0 pour ne pas decaler le tracking lateralement,
        // et le parent (OVRCameraRig) reste libre de se deplacer.
        transform.localPosition = new Vector3(0f, _current, 0f);
    }

    /// <summary>
    /// Releve la hauteur des yeux courante et en fait la nouvelle reference.
    /// A appeler joueur debout, en position neutre. Sans parametre, donc bindable
    /// depuis l'onglet References de Kontrols (bouton CALIBRATE) ou un UnityEvent.
    /// </summary>
    [ContextMenu("Calibrer maintenant")]
    public void Calibrate()
    {
        if (TryCalibrate())
            return;

        // Deux causes possibles, et elles ne se corrigent pas au meme endroit.
        if (_centerEye == null)
            Debug.LogError(MissingAnchorMessage, this);
        else
            Debug.LogWarning("[Set_VR_Avatar_height] Calibration refusee : hauteur des yeux lue a " +
                             _centerEye.localPosition.y.ToString("F3") + " m, sous minValidEyeHeight (" +
                             minValidEyeHeight + " m). Casque porte ?", this);
    }

    /// <summary>Variante silencieuse : false si la pose n'est pas exploitable.</summary>
    public bool TryCalibrate()
    {
        if (_centerEye == null && !ResolveCenterEye())
            return false;

        // localPosition est relatif au TrackingSpace, donc a l'origine de tracking :
        // notre propre offset n'entre pas dans la mesure, et l'ordre d'execution
        // vis-a-vis d'OVRCameraRig n'a pas d'importance.
        float eye = _centerEye.localPosition.y;
        if (eye < minValidEyeHeight)
            return false;

        MeasuredEyeHeight = eye;
        IsCalibrated = true;

        WarnIfOriginIsNotFloorBased();
        Debug.Log($"[Set_VR_Avatar_height] Calibre : yeux mesures a {eye:F3} m, " +
                  $"cible {height:F3} m, offset {OffsetY:F3} m.", this);
        return true;
    }

    IEnumerator CalibrateWhenPoseIsValid()
    {
        float deadline = Time.unscaledTime + calibrationTimeout;

        while (Time.unscaledTime < deadline)
        {
            if (TryCalibrate())
                yield break;
            yield return null;
        }

        if (_centerEye == null)
        {
            Debug.LogError(MissingAnchorMessage, this);
            yield break;
        }

        Debug.LogWarning($"[Set_VR_Avatar_height] Aucune pose casque valide apres " +
                         $"{calibrationTimeout} s : offset calcule sur assumedEyeHeight " +
                         $"({assumedEyeHeight} m). Utiliser le bouton CALIBRATE une fois le " +
                         "casque porte.", this);
    }

    bool ResolveCenterEye()
    {
        OVRCameraRig rig = GetComponentInParent<OVRCameraRig>();
        if (rig != null && rig.centerEyeAnchor != null)
        {
            _centerEye = rig.centerEyeAnchor;
        }
        else
        {
            // centerEyeAnchor est rempli par OVRCameraRig a l'init, donc null hors Play.
            // Ce script vit sur TrackingSpace : CenterEyeAnchor est un enfant direct.
            _centerEye = transform.Find("CenterEyeAnchor");
        }

        // Pas de log ici : cette methode est reappelee a chaque frame tant que la
        // calibration n'a pas abouti. Les appelants rapportent l'echec une seule fois.
        return _centerEye != null;
    }

    void WarnIfOriginIsNotFloorBased()
    {
        OVRManager ovr = OVRManager.instance;
        if (ovr == null)
            return;

        OVRManager.TrackingOrigin origin = ovr.trackingOriginType;
        if (origin == OVRManager.TrackingOrigin.Stage || origin == OVRManager.TrackingOrigin.FloorLevel)
            return;

        Debug.LogWarning($"[Set_VR_Avatar_height] Tracking Origin Type = {origin}. La calibration " +
                         "suppose une origine au sol (Stage ou Floor Level) ; en Eye Level la hauteur " +
                         "mesuree repart de la posture du recentrage et variera d'un lancement a " +
                         "l'autre.", this);
    }

    public void SetHeight(float value) => height = value;

    // Conserve pour compatibilite avec tes UnityEvents / sliders existants
    public void setHeight(float value) => height = value;
}
