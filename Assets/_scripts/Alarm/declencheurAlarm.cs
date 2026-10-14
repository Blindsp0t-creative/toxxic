using System.Collections;
using UnityEngine;

public class declencheurAlarm : MonoBehaviour
{
    [SerializeField] private AlarmLightHDRP[] alarmLights;

    [SerializeField] private bool alarmOn = false;

    [Header("--------- DIRECTIONAL LIGHT (montee de plateforme) ---------")]
    [Tooltip("La Directional Light de la scene. Laisser vide pour ne pas y toucher : " +
             "le script se comporte alors comme avant.")]
    [SerializeField] private Light directionalLight;

    [Tooltip("Duree de la remontee en secondes. podiumAnim dure 17.3 s : mettre moins " +
             "pour que le jour soit revenu avant l'arrivee de la plateforme, plus pour " +
             "que la lumiere finisse de monter apres.")]
    [SerializeField] private float fadeDuration = 17.3f;

    [Tooltip("Forme de la courbe. 1 = lineaire. Au-dela : doucement au debut puis " +
             "rapidement sur la fin (3 est un bon point de depart, 5 pour un effet " +
             "plus brutal).")]
    [SerializeField, Min(0.01f)] private float fadeExponent = 3f;

    private bool previousState = false;

    // Intensite a retrouver en fin de fondu. Relue au depart de chaque montee
    // plutot que mise en cache une fois pour toutes : un rappel de preset peut
    // avoir change la lumiere entre deux tops, et c'est la valeur du moment
    // qu'il faut restituer. 'fadeRoutine != null' sert de garde : pendant le
    // fondu l'intensite courante est la notre, pas celle de l'inspecteur.
    private float baseIntensity;
    private Coroutine fadeRoutine;

    private void OnValidate()
    {
        if (alarmOn != previousState)
        {
            previousState = alarmOn;
            SetAllAlarms(alarmOn);
        }
    }

    private void SetAllAlarms(bool state)
    {
        // La lumiere suit l'alarme : un top plateforme passe par RestartAll(),
        // donc par ici. Fait avant les gyrophares pour que le noir tombe sur la
        // meme frame que le premier flash.
        if (state) StartDirectionalFade();
        else RestoreDirectional();

        if (alarmLights == null) return;

        foreach (var alarm in alarmLights)
        {
            if (alarm == null) continue;

            if (state)
                alarm.StartAlarm();
            else
                alarm.StopAlarm();
        }
    }

    public void ToggleAll()
    {
        alarmOn = !alarmOn;
        previousState = alarmOn;
        SetAllAlarms(alarmOn);
    }

    // Force l'etat, sans dependre de l'etat precedent (contrairement a ToggleAll).
    public void SetAll(bool state)
    {
        alarmOn = state;
        previousState = state;
        SetAllAlarms(state);
    }

    public void StartAll() => SetAll(true);

    public void StopAll() => SetAll(false);

    // Relance l'alarme depuis zero : AlarmLightHDRP.StartAlarm() sort
    // immediatement si sa coroutine tourne deja, donc il faut couper avant
    // pour que le clignotement reparte en phase et que l'evenement
    // OnAlarmStateChanged soit renvoye aux alarmDependantObject.
    public void RestartAll()
    {
        SetAll(false);
        SetAll(true);
    }

    // ── Directional light ────────────────────────────────────────────
    //
    // Light.intensity est en unite native, et pour une directionnelle c'est le
    // lux : la valeur lue et ecrite ici est exactement celle affichee dans
    // l'inspecteur. On ne touche donc ni a lightUnit ni a HDAdditionalLightData
    // (dont SetIntensity() est obsolete depuis HDRP 2023.3).

    // Coupe la lumiere puis la fait remonter a son niveau d'origine.
    public void StartDirectionalFade()
    {
        if (directionalLight == null) return;

        // Coroutines impossibles hors play mode : le toggle de l'inspecteur ne
        // doit pas laisser la scene dans le noir en edition.
        if (!Application.isPlaying) return;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        else baseIntensity = directionalLight.intensity;

        fadeRoutine = StartCoroutine(DirectionalFade());
    }

    // Rend la main a l'inspecteur : intensite d'origine, fondu annule.
    public void RestoreDirectional()
    {
        if (directionalLight == null) return;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
            directionalLight.intensity = baseIntensity;
        }
    }

    private IEnumerator DirectionalFade()
    {
        directionalLight.intensity = 0f;

        // Duree nulle ou negative : on ne fait que le noir d'une frame.
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            directionalLight.intensity = baseIntensity * Mathf.Pow(t, fadeExponent);
            yield return null;
        }

        directionalLight.intensity = baseIntensity;
        fadeRoutine = null;
    }

    // Si l'objet est desactive en pleine montee, la coroutine meurt avec lui et
    // la scene resterait dans le noir.
    private void OnDisable() => RestoreDirectional();
}
