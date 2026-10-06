using UnityEngine;

public class declencheurAlarm : MonoBehaviour
{
    [SerializeField] private AlarmLightHDRP[] alarmLights;

    [SerializeField] private bool alarmOn = false;

    private bool previousState = false;

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
}