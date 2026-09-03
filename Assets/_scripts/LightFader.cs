using UnityEngine;
using System.Collections;
using VLB;

public class LightFader : MonoBehaviour
{
    [SerializeField] private VolumetricLightBeamHD[] beams;
    [SerializeField] private float fadeDuration = 0.5f;

    private Color[] baseColors;
    private Coroutine routine;
    private bool isOn = false;

    public bool IsOn
    {
        get => isOn;
        set
        {
            if (isOn == value) return;
            isOn = value;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Fade(isOn ? 1f : 0f));
        }
    }

    void Awake()
    {
        baseColors = new Color[beams.Length];
        for (int i = 0; i < beams.Length; i++)
            baseColors[i] = beams[i].colorFlat;

        SetAlpha(0f);
    }

    private IEnumerator Fade(float target)
    {
        // ratio actuel lu sur le premier beam
        float start = beams[0].colorFlat.a / baseColors[0].a;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(start, target, t / fadeDuration));
            yield return null;
        }

        SetAlpha(target);
        routine = null;
    }

    private void SetAlpha(float ratio)
    {
        for (int i = 0; i < beams.Length; i++)
        {
            Color c = baseColors[i];
            c.a = baseColors[i].a * ratio;
            beams[i].colorFlat = c;
        }
    }
}