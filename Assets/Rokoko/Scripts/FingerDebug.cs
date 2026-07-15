using UnityEngine;

public class FingerDebug : MonoBehaviour
{
    Animator anim;
    Transform idx;

    void Start()
    {
        anim = GetComponent<Animator>();
        idx = anim.GetBoneTransform(HumanBodyBones.LeftIndexProximal);
        Debug.Log(idx == null
            ? "BONE NULL -> os non mappé dans l'Avatar"
            : "Bone OK : " + idx.name);
    }

    void LateUpdate()
    {
        if (idx != null)
            Debug.Log($"[LateUpdate] {idx.localRotation.eulerAngles}");
    }
}