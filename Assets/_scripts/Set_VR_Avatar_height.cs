using UnityEngine;
using System.Collections;

public class Set_VR_Avatar_height : MonoBehaviour
{
    public float height = 1.9f;

    private void Start()
    {
        StartCoroutine(setHeightAfterSeconds(1));
        StartCoroutine(setHeightAfterSeconds(3));

    }


    IEnumerator setHeightAfterSeconds(float _seconds)
    {
        yield return new WaitForSeconds(_seconds);
        this.transform.position = new Vector3(0,height,0);
    }
}
