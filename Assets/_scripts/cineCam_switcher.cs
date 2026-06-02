using UnityEngine;

public class cineCam_switcher : MonoBehaviour
{
    public GameObject[] _cams = new GameObject[10];

    [Range(0, 10)]
    public int _activeCam=0;

    private int currentCam = 0;

    void Start()
    {
        //_cams = 
        disableAllCams();
        _cams[0].SetActive(true);
    }

    void Update()
    {
        if(_activeCam != currentCam)
        {
            disableAllCams();
            
            _cams[_activeCam].SetActive(true);
            currentCam = _activeCam;
        }
    }

    public void disableAllCams()
    {
        for (int i = 0; i < _cams.Length; i++)
        {
            _cams[i].SetActive(false);
        }
    }
}
