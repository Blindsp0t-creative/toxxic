using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[RequireComponent(typeof(Light))]
public class AlarmLightHDRP : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private Light lightSource;
    [SerializeField] private Renderer emissiveRenderer;

    [Header("Émission (matériau)")]
    [SerializeField] private Color emissionColor = Color.red;
    [Tooltip("Multiplicateur appliqué à la couleur. 10-100 pour un bon bloom en HDRP.")]
    [SerializeField] private float emissionIntensity = 30f;

    [Header("Lumière")]
    [Tooltip("En lumens. Gyrophare : 2000-5000.")]
    [SerializeField] private float lightLumens = 3000f;

    [Header("Clignotement")]
    [SerializeField] private float blinkInterval = 0.4f;
    [SerializeField] private bool useSmoothPulse = false;
    [SerializeField] private float pulseSpeed = 2f;

    private static readonly int EmissiveColorID = Shader.PropertyToID("_EmissiveColor");

    private HDAdditionalLightData hdLight;
    private Material mat;
    private Coroutine routine;

    private void Awake()
    {
        if (lightSource == null) lightSource = GetComponent<Light>();
        if (emissiveRenderer == null) emissiveRenderer = GetComponent<Renderer>();

        hdLight = lightSource.GetComponent<HDAdditionalLightData>();
        if (hdLight == null)
            hdLight = lightSource.gameObject.AddComponent<HDAdditionalLightData>();

        mat = emissiveRenderer.material; // instance
        mat.EnableKeyword("_EMISSIVE_COLOR_MAP"); // si tu utilises une texture d'émission

        SetIntensity(0f);
    }

    public void StartAlarm()
    {
        if (routine != null) return;
        routine = StartCoroutine(useSmoothPulse ? Pulse() : Blink());
    }

    public void StopAlarm()
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
        SetIntensity(0f);
    }

    private IEnumerator Blink()
    {
        var wait = new WaitForSeconds(blinkInterval);
        while (true)
        {
            SetIntensity(1f);
            yield return wait;
            SetIntensity(0f);
            yield return wait;
        }
    }

    private IEnumerator Pulse()
    {
        while (true)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) + 1f) * 0.5f;
            SetIntensity(t);
            yield return null;
        }
    }

    /// <param name="t">0 = éteint, 1 = pleine puissance</param>
    private void SetIntensity(float t)
    {
        // Lumière : on module l'intensité plutôt que enabled/disabled
        hdLight.SetIntensity(lightLumens * t, LightUnit.Lumen);
        lightSource.enabled = t > 0.001f;

        // Matériau : _EmissiveColor attend une valeur HDR linéaire
        Color hdr = emissionColor.linear * (emissionIntensity * t);
        mat.SetColor(EmissiveColorID, hdr);
    }
}