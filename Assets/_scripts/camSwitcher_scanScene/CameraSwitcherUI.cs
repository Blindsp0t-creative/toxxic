using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CameraSwitcherUI : MonoBehaviour
{
    [Header("Références")]
    public CinemachineCameraSwitcher switcher;
    public Slider slider;
    public Button btnNext;
    public Button btnPrevious;
    public TextMeshProUGUI labelCameraName;  // optionnel

    void Start()
    {
        // Configurer le slider selon le nombre de cameras
        slider.minValue = 0;
        slider.maxValue = switcher.CameraCount - 1;
        slider.wholeNumbers = true;
        slider.value = switcher.CurrentIndex;

        // Brancher les événements
        slider.onValueChanged.AddListener(OnSliderChanged);
        btnNext.onClick.AddListener(OnNext);
        btnPrevious.onClick.AddListener(OnPrevious);

        UpdateLabel();
    }

    void OnSliderChanged(float value)
    {
        switcher.OnSliderChanged(value);
        UpdateLabel();
    }

    void OnNext()
    {
        switcher.NextCamera();
        slider.value = switcher.CurrentIndex;  // resync slider
        UpdateLabel();
    }

    void OnPrevious()
    {
        switcher.PreviousCamera();
        slider.value = switcher.CurrentIndex;  // resync slider
        UpdateLabel();
    }

    void UpdateLabel()
    {
        if (labelCameraName != null)
            labelCameraName.text = switcher.CurrentCameraName;
    }
}