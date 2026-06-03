using UnityEngine;

public class Set_VR_Avatar_height : MonoBehaviour
{
    public float height = 1.9f;

    void Update()
    {
        this.transform.position = new Vector3(this.transform.localPosition.x, /*this.transform.localPosition.y +*/ height, this.transform.localPosition.z);        
    }

    public void setHeight(float _height)
    {
        this.height = _height;
    }
}
