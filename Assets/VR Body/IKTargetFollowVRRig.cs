using UnityEngine;

[System.Serializable]
public class VRMap
{
    public Transform vrTarget;
    public Transform ikTarget;
    public Vector3 trackingPositionOffset;
    public Vector3 trackingRotationOffset;

    public void Map()
    {
        Map(Vector3.zero, Vector3.one);
    }

    // Amplifie la position du target autour d'un pivot (la tete). Necessaire des que
    // l'avatar n'est pas a la taille du joueur : le tracking arrive en metres reels
    // alors que l'avatar a des bras N fois plus longs, ce qui ecrase l'amplitude.
    public void Map(Vector3 pivot, Vector3 scale)
    {
        Vector3 tracked = vrTarget.TransformPoint(trackingPositionOffset);
        ikTarget.position = pivot + Vector3.Scale(tracked - pivot, scale);
        ikTarget.rotation = vrTarget.rotation * Quaternion.Euler(trackingRotationOffset);
    }
}

public class IKTargetFollowVRRig : MonoBehaviour
{
    [Range(0,1)]
    public float turnSmoothness = 0.1f;
    public VRMap head;
    public VRMap leftHand;
    public VRMap rightHand;

    public Vector3 headBodyPositionOffset;
    public float headBodyYawOffset;

    [Header("Amplitude des mains")]
    [Tooltip("Rapport taille avatar / taille joueur. 1 = avatar a taille humaine, pas d'amplification. " +
             "Point de depart : |headBodyPositionOffset.y| divise par la hauteur de tes yeux en metres.")]
    public float handTrackingScale = 1f;

    [Tooltip("Reglage fin par axe, multiplie handTrackingScale.")]
    public Vector3 handTrackingScaleAxis = Vector3.one;

    // Update is called once per frame
    void LateUpdate()
    {
        // Position reelle du casque : sert a poser le corps et de pivot d'amplification.
        Vector3 headPosition = head.vrTarget.TransformPoint(head.trackingPositionOffset);

        transform.position = headPosition + headBodyPositionOffset;
        float yaw = head.vrTarget.eulerAngles.y;
        transform.rotation = Quaternion.Lerp(transform.rotation,Quaternion.Euler(transform.eulerAngles.x, yaw, transform.eulerAngles.z),turnSmoothness);

        // Le corps vient d'etre deplace : les targets se reposent ensuite en coordonnees monde.
        head.Map();

        Vector3 handScale = HandScale();
        leftHand.Map(headPosition, handScale);
        rightHand.Map(headPosition, handScale);
    }

    Vector3 HandScale()
    {
        float uniform = handTrackingScale > 0f ? handTrackingScale : 1f;
        return new Vector3(
            uniform * (handTrackingScaleAxis.x > 0f ? handTrackingScaleAxis.x : 1f),
            uniform * (handTrackingScaleAxis.y > 0f ? handTrackingScaleAxis.y : 1f),
            uniform * (handTrackingScaleAxis.z > 0f ? handTrackingScaleAxis.z : 1f));
    }
}
