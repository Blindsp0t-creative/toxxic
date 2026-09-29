using UnityEngine;

[RequireComponent(typeof(Light))]
public class DiscoColor : MonoBehaviour
{
    public enum Mode { SautsSurLeTempo, FonduPalette }

    [Header("Puissance (à régler ICI, pas dans la Light)")]
    [Tooltip("Intensité de base de la lumière. Si 0 au lancement, reprend la valeur de la Light.")]
    public float puissance = 0f;

    [Header("Couleurs kitsch")]
    public Color[] palette = new Color[]
    {
        new Color(1f, 0.1f, 0.6f),  // rose
        new Color(0.1f, 0.9f, 1f),  // cyan
        new Color(1f, 0.9f, 0.1f),  // jaune
        new Color(0.6f, 0.2f, 1f),  // violet
        new Color(0.2f, 1f, 0.3f),  // vert
    };
    public Mode mode = Mode.SautsSurLeTempo;

    [Header("Rythme")]
    public float bpm = 124f;
    [Tooltip("Nombre de temps entre deux changements de couleur")]
    public int tempsParCouleur = 2;

    [Header("Pulsation")]
    public bool pulsation = true;
    [Range(0f, 1f)] public float forcePulsation = 0.4f;

    Light lumiere;

    void Start()
    {
        lumiere = GetComponent<Light>();

        // Si tu n'as rien mis, on reprend l'intensité actuelle de la Light
        if (puissance <= 0f)
            puissance = lumiere.intensity;
    }

    void Update()
    {
        if (palette == null || palette.Length == 0) return;

        float dureeTemps = 60f / bpm;
        float temps = Time.time / dureeTemps;           // nombre de temps écoulés
        float cycle = temps / Mathf.Max(1, tempsParCouleur);

        // --- Couleur ---
        int index = Mathf.FloorToInt(cycle) % palette.Length;
        if (mode == Mode.SautsSurLeTempo)
        {
            lumiere.color = palette[index];
        }
        else
        {
            int suivant = (index + 1) % palette.Length;
            float t = cycle - Mathf.Floor(cycle);
            lumiere.color = Color.Lerp(palette[index], palette[suivant], t);
        }

        // --- Intensité = puissance × pulsation ---
        float multiplicateur = 1f;
        if (pulsation)
        {
            float phase = temps - Mathf.Floor(temps);    // 0 → 1 sur chaque temps
            float flash = 1f - phase;                     // pic au début du temps
            multiplicateur = 1f - forcePulsation + forcePulsation * flash * flash;
        }

        lumiere.intensity = puissance * multiplicateur;
    }
}