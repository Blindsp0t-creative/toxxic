using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class OVRLocomotion : MonoBehaviour
{
    [Header("Références")]
    public OVRCameraRig cameraRig;

    [Header("Déplacement (joystick gauche)")]
    public float moveSpeed = 3.0f;
    public bool enableStrafe = true;

    [Header("Rotation continue (joystick droit)")]
    public float rotationSpeed = 90f;      // degrés par seconde
    public float rotationDeadzone = 0.2f;  // seuil d'activation

    [Header("Gravité")]
    public float gravity = -9.81f;

    private CharacterController _cc;
    private float _verticalVelocity = 0f;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();

        if (cameraRig == null)
            cameraRig = GetComponentInChildren<OVRCameraRig>();

        // Garde-fou : le rig doit être sous le CharacterController,
        // sinon la caméra ne suivra pas le déplacement.
        if (cameraRig == null)
            Debug.LogError("[OVRLocomotion] Aucun OVRCameraRig trouvé.");
        else if (!cameraRig.transform.IsChildOf(transform))
            Debug.LogError($"[OVRLocomotion] '{cameraRig.name}' n'est pas enfant de '{name}'.");
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    void HandleMovement()
    {
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        Transform cam = cameraRig.centerEyeAnchor;
        Vector3 forward = cam.forward; forward.y = 0f; forward.Normalize();
        Vector3 right = cam.right; right.y = 0f; right.Normalize();

        Vector3 move = (forward * input.y) + (enableStrafe ? right * input.x : Vector3.zero);
        move *= moveSpeed;   // vitesse appliquée avant la gravité

        if (_cc.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;
        else
            _verticalVelocity += gravity * Time.deltaTime;

        move.y = _verticalVelocity;   // gravité en m/s, non scalée par moveSpeed

        _cc.Move(move * Time.deltaTime);
    }

    void HandleRotation()
    {
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        if (Mathf.Abs(input.x) < rotationDeadzone) return;

        float angle = input.x * rotationSpeed * Time.deltaTime;

        // Pivot autour de la position réelle de la tête
        transform.RotateAround(cameraRig.centerEyeAnchor.position, Vector3.up, angle);
    }
}