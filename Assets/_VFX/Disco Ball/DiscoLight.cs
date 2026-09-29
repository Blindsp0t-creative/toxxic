using UnityEngine;

[RequireComponent(typeof(Light))]
public class DiscoLight : MonoBehaviour
{
    public enum FormeFacette { Carre, Rond }

    [Header("Boule à facettes")]
    [Tooltip("Résolution de chaque face du cookie (512 conseillé)")]
    public int resolution = 512;
    [Tooltip("Nombre de rangées de facettes du pôle sud au pôle nord")]
    [Range(8, 80)] public int nombreAnneaux = 36;
    [Tooltip("Portion de la facette qui renvoie de la lumière (espace noir entre les points)")]
    [Range(0.1f, 1f)] public float tailleFacette = 0.45f;
    [Range(0f, 1f)] public float douceur = 0.25f;
    public FormeFacette forme = FormeFacette.Carre;
    [Tooltip("Décale une rangée sur deux, comme les vraies boules")]
    public bool decalerRangees = true;

    [Header("Pôles")]
    [Tooltip("Nombre de rangées supprimées en haut (fixation de la boule)")]
    public int anneauxVidesEnHaut = 2;
    [Tooltip("Nombre de rangées supprimées en bas")]
    public int anneauxVidesEnBas = 1;

    [Header("Réalisme")]
    [Tooltip("Petite variation de luminosité entre les miroirs")]
    [Range(0f, 1f)] public float variationLuminosite = 0.25f;
    [Tooltip("Pourcentage de miroirs manquants ou ternis")]
    [Range(0f, 0.5f)] public float miroirsManquants = 0.03f;
    public int seed = 42;

    [Header("Rotation (degrés / seconde)")]
    public Vector3 vitesseRotation = new Vector3(0f, 15f, 0f);

    void Start()
    {
        Light l = GetComponent<Light>();
        l.type = LightType.Point;
        l.cookie = GenererCookie();
    }

    void Update()
    {
        transform.Rotate(vitesseRotation * Time.deltaTime, Space.Self);
    }

    Cubemap GenererCookie()
    {
        var cube = new Cubemap(resolution, TextureFormat.RGBA32, false);
        cube.wrapMode = TextureWrapMode.Clamp;
        cube.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[resolution * resolution];

        for (int f = 0; f < 6; f++)
        {
            CubemapFace face = (CubemapFace)f;
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float u = (x + 0.5f) / resolution * 2f - 1f;
                    float v = (y + 0.5f) / resolution * 2f - 1f;
                    Vector3 dir = DirectionFace(face, u, v).normalized;

                    float valeur = ValeurFacette(dir);
                    pixels[y * resolution + x] = new Color(valeur, valeur, valeur, valeur);
                }
            }
            cube.SetPixels(pixels, face);
        }

        cube.Apply(false, true); // non lisible ensuite = moins de mémoire
        return cube;
    }

    float ValeurFacette(Vector3 dir)
    {
        float hauteurAnneau = Mathf.PI / nombreAnneaux;

        // --- Latitude -> numéro d'anneau ---
        float lat = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f));      // -PI/2 .. PI/2
        float posLat = (lat + Mathf.PI * 0.5f) / hauteurAnneau;
        int anneau = Mathf.Clamp(Mathf.FloorToInt(posLat), 0, nombreAnneaux - 1);

        if (anneau < anneauxVidesEnBas || anneau >= nombreAnneaux - anneauxVidesEnHaut)
            return 0f;

        float latCentre = -Mathf.PI * 0.5f + (anneau + 0.5f) * hauteurAnneau;

        // --- Nombre de facettes dans cet anneau (proportionnel à la circonférence) ---
        int nbFacettes = Mathf.Max(1, Mathf.RoundToInt(2f * Mathf.PI * Mathf.Cos(latCentre) / hauteurAnneau));
        float largeurFacette = 2f * Mathf.PI / nbFacettes;

        // --- Longitude -> numéro de facette ---
        float lon = Mathf.Atan2(dir.z, dir.x) + Mathf.PI;           // 0 .. 2PI
        if (decalerRangees && (anneau % 2 == 1))
            lon += largeurFacette * 0.5f;
        float posLon = lon / largeurFacette;
        int facette = Mathf.FloorToInt(posLon) % nbFacettes;

        // --- Position locale dans la facette (-0.5 .. 0.5) ---
        float du = (posLon - Mathf.Floor(posLon)) - 0.5f;
        float dv = (posLat - Mathf.Floor(posLat)) - 0.5f;

        float d = (forme == FormeFacette.Carre)
            ? Mathf.Max(Mathf.Abs(du), Mathf.Abs(dv)) * 2f
            : Mathf.Sqrt(du * du + dv * dv) * 2f;

        float bord = tailleFacette;
        float debutFondu = bord * (1f - douceur);
        float masque = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(debutFondu, bord, d));
        if (masque <= 0f) return 0f;

        // --- Hasard stable par facette (même résultat à chaque lancement) ---
        float h = Hash(anneau, facette);
        if (h < miroirsManquants) return 0f;
        float lum = 1f - variationLuminosite * Hash(facette + 17, anneau + 91);

        return masque * lum;
    }

    float Hash(int a, int b)
    {
        unchecked
        {
            int n = a * 73856093 ^ b * 19349663 ^ seed * 83492791;
            n = (n << 13) ^ n;
            n = n * (n * n * 15731 + 789221) + 1376312589;
            return (n & 0x7fffffff) / (float)0x7fffffff;
        }
    }

    static Vector3 DirectionFace(CubemapFace face, float u, float v)
    {
        switch (face)
        {
            case CubemapFace.PositiveX: return new Vector3(1f, -v, -u);
            case CubemapFace.NegativeX: return new Vector3(-1f, -v, u);
            case CubemapFace.PositiveY: return new Vector3(u, 1f, v);
            case CubemapFace.NegativeY: return new Vector3(u, -1f, -v);
            case CubemapFace.PositiveZ: return new Vector3(u, -v, 1f);
            default: return new Vector3(-u, -v, -1f);
        }
    }
}