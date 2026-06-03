using UnityEngine;

public class LayerSwitcher : MonoBehaviour
{
    [Header("GameObjects")]
    public GameObject object1;
    public GameObject object2;
    public GameObject object3;

    [Header("Layers")]
    public string layerAvatar = "AVATAR";
    public string layerDefault = "Default";

    private bool _isOnAvatar = true;

    public void SwitchToAvatar()
    {
        SetLayer(LayerMask.NameToLayer(layerAvatar));
        _isOnAvatar = true;
    }

    public void SwitchToDefault()
    {
        SetLayer(LayerMask.NameToLayer(layerDefault));
        _isOnAvatar = false;
    }

    public void Toggle()
    {
        if (_isOnAvatar) SwitchToDefault();
        else SwitchToAvatar();
    }

    void SetLayer(int layer)
    {
        if (layer == -1)
        {
            Debug.LogWarning("LayerSwitcher : layer introuvable. Vérifie le nom dans Project Settings > Tags and Layers.");
            return;
        }
        if (object1 != null)
        {
            object1.layer = layer;

            foreach (Transform child in object1.transform)
            {
                child.gameObject.layer = layer;
            }
        }
        if (object2 != null) object2.layer = layer;
        if (object3 != null) object3.layer = layer;
    }
}