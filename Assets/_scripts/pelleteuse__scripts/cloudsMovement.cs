using Oculus.Interaction.Editor;
using UnityEngine;

public class cloudsMovement : MonoBehaviour
{
    public float speed = 0.01f;

    void Update()
    {
        float oldPos = transform.position.x;
        float newPos = oldPos += speed;
        transform.position = new Vector3( newPos, transform.position.y, transform.position.z);
    }
}
