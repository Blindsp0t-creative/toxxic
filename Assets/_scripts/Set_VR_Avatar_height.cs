using UnityEngine;

/// <summary>
/// Règle la hauteur des yeux en décalant le TrackingSpace sur son axe Y LOCAL.
/// À poser sur TrackingSpace (enfant de OVRCameraRig).
/// </summary>
public class Set_VR_Avatar_height : MonoBehaviour
{
    [Header("Hauteur des yeux (m)")]
    [Range(0.5f, 2.5f)]
    public float height = 1.9f;

    [Tooltip("0 = instantané. Sinon, lissage en secondes.")]
    public float smoothTime = 0f;

    private float _current;
    private float _velocity;

    void Awake()
    {
        _current = height;
    }

    void LateUpdate()
    {
        // Lissage optionnel pour un réglage à la volée sans à-coups
        _current = (smoothTime <= 0f)
            ? height
            : Mathf.SmoothDamp(_current, height, ref _velocity, smoothTime);

        // localPosition : on ne touche QUE l'axe Y, en local.
        // x et z restent à 0 pour ne pas décaler le tracking latéralement,
        // et le parent (OVRCameraRig) reste libre de se déplacer.
        transform.localPosition = new Vector3(0f, _current, 0f);
    }

    public void SetHeight(float value) => height = value;

    // Conservé pour compatibilité avec tes UnityEvents / sliders existants
    public void setHeight(float value) => height = value;

}