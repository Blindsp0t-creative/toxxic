using UnityEngine;

public class AlarmToggle : MonoBehaviour
{
    [Header("Référence")]
    [SerializeField] private AlarmLightHDRP alarmLight;

    [Header("Déclenchement")]
    [SerializeField] private bool alarmOn = false;

    private bool previousState = false;

    private void OnValidate()
    {
        // Appelé automatiquement quand tu modifies une valeur dans l'Inspector
        if (alarmOn != previousState)
        {
            previousState = alarmOn;

            if (alarmOn)
                alarmLight?.StartAlarm();
            else
                alarmLight?.StopAlarm();
        }
    }
}