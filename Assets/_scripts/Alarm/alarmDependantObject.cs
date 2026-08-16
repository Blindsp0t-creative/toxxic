using UnityEngine;

public class alarmDependantObject : MonoBehaviour
{
    [Header("Objets à désactiver quand l'alarme démarre")]
    public GameObject[] objectsToToggle;

    [Tooltip("Si coché : les objets sont désactivés quand l'alarme démarre, et réactivés quand elle s'arrête. Décoche pour inverser le comportement.")]
    [SerializeField] private bool disableOnAlarmStart = true;

    private void OnEnable()
    {
        AlarmLightHDRP.OnAlarmStateChanged += HandleAlarmStateChanged;
    }

    private void OnDisable()
    {
        AlarmLightHDRP.OnAlarmStateChanged -= HandleAlarmStateChanged;
    }

    private void HandleAlarmStateChanged(bool alarmIsOn)
    {
        bool shouldBeActive = disableOnAlarmStart ? !alarmIsOn : alarmIsOn;

        foreach (var obj in objectsToToggle)
        {
            if (obj == null) continue;
            obj.SetActive(shouldBeActive);
        }
    }
}