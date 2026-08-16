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
}