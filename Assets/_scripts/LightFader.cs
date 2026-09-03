using UnityEngine;
using System.Collections;
using VLB;

public class LightFader : MonoBehaviour
{
    [SerializeField] private VolumetricLightBeamHD beam;
    [SerializeField] private float fadeDuration = 0.5f;

    private Color baseColor;
    private Coroutine routine;

    void Awake()
    {
        if (beam == null) beam = GetComponent<VolumetricLightBeamHD>();
        baseColor = beam.colorFlat;
        SetAlpha(0f);
    }

    public void SetLight(bool on)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Fade(on ? 1f : 0f));
    }

    private IEnumerator Fade(float target)
    {
        float start = beam.colorFlat.a / baseColor.a;
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
        Color c = baseColor;
        c.a = baseColor.a * ratio;
        beam.colorFlat = c;
       // beam.SetPropertyChanged();
    }
}