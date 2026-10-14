using UnityEngine;

public class discoBallController : MonoBehaviour
{
    public GameObject discoBallGO;
    public GameObject directionnalLight;

    void Start()
    {
        if (discoBallGO != null)
            discoBallGO.SetActive(false);
        else
            Debug.Log("discoBall not assigned");
    }

    public void toggleDiscoBall(bool _active)
    {
        if (discoBallGO != null)
            discoBallGO.SetActive(_active);
        if(directionnalLight != null)
            directionnalLight.SetActive(!_active);
    }
}
