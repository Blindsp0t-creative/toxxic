using UnityEngine;

public class discoBallController : MonoBehaviour
{
    public GameObject discoBallGO;
    
    void Start()
    {
        if (discoBallGO != null)
            discoBallGO.SetActive(false);
        else
            Debug.Log("discoBall not assigned");
    }

    public void toggleDiscoBall(bool _active)
    {
        discoBallGO.SetActive(_active);
    }
}
