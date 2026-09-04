using System.Collections.Generic;
using UnityEngine;

public class CharacterDissolveController : MonoBehaviour
{
    static readonly int DissolveID = Shader.PropertyToID("_Dissolve");

    [Range(0f, 1f)] public float dissolve;

    List<Material> dissolveMaterials = new List<Material>();
    float lastValue = -1f;

    void Awake()
    {
        // Récupère tous les renderers du perso (SkinnedMeshRenderer inclus)
        var renderers = GetComponentsInChildren<Renderer>(true);

        foreach (var r in renderers)
        {
            // .materials instancie une seule fois, ici c'est voulu
            var mats = r.materials;
            foreach (var m in mats)
            {
                if (m != null && m.HasFloat(DissolveID))
                    dissolveMaterials.Add(m);
            }
        }

        Debug.Log($"[Dissolve] {dissolveMaterials.Count} matériaux trouvés.");
    }

    void Update()
    {
        if (Mathf.Approximately(dissolve, lastValue)) return;
        lastValue = dissolve;

        for (int i = 0; i < dissolveMaterials.Count; i++)
            dissolveMaterials[i].SetFloat(DissolveID, dissolve);
    }

    // Pratique pour déclencher depuis un Timeline / Animator / UnityEvent
    public void SetDissolve(float value) => dissolve = Mathf.Clamp01(value);
}