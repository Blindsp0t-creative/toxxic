using UnityEngine;

public class MachineState : MonoBehaviour
{
    [SerializeField] private LightFader beamFader;

    private bool isActive;

    public bool IsActive
    {
        get => isActive;
        set
        {
            if (isActive == value) return;
            isActive = value;
            beamFader.SetLight(isActive);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            IsActive = !IsActive;
    }
}