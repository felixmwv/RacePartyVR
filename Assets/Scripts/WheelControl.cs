using UnityEngine;

public class WheelControl : MonoBehaviour
{
    public Transform wheelModel;

    [Header("Dynamic Grip")]
    public float minSidewaysGrip = 0.4f;
    public float maxSidewaysGrip = 1f;
    public float powerSlipStrength = 1.0f;
    [Header("Braking Grip")]
    public float brakeSidewaysGrip = 0.75f;
    [Header("Drift")]
    public float normalSidewaysGrip = 1.0f;
    public float handbrakeSidewaysGrip = 0.3f;

    [HideInInspector] public WheelCollider WheelCollider;

    public bool steerable;
    public bool motorized;

    private bool handbrakeActive;

    private Vector3 position;
    private Quaternion rotation;
    public enum WheelSide
    {
        Left,
        Right
    }

    public WheelSide side;
    public bool isFront;

    private void Start()
    {
        WheelCollider = GetComponent<WheelCollider>();
    }

    public void UpdateDynamicGrip(
        float rpm01,
        float torque01,
        float surfaceGrip
    )
    {
        if (!motorized)
            return;

        if (!WheelCollider.GetGroundHit(out WheelHit hit))
            return;

        float slip = Mathf.Abs(hit.sidewaysSlip);

        float slip01 = Mathf.InverseLerp(0.2f, 1.0f, slip);

        float grip = Mathf.Lerp(
            maxSidewaysGrip,
            minSidewaysGrip,
            slip01
        );

        grip *= surfaceGrip;

        WheelFrictionCurve friction = WheelCollider.sidewaysFriction;
        friction.stiffness = grip;
        WheelCollider.sidewaysFriction = friction;
    }
    public void SetBrakeGrip(bool braking)
    {
        if (handbrakeActive)
            return;

        WheelFrictionCurve friction = WheelCollider.sidewaysFriction;

        friction.stiffness = braking
            ? brakeSidewaysGrip
            : normalSidewaysGrip;

        WheelCollider.sidewaysFriction = friction;
    }


    public void SetHandbrake(bool active)
    {
        if (handbrakeActive == active)
            return;

        handbrakeActive = active;

        WheelFrictionCurve friction = WheelCollider.sidewaysFriction;
        friction.stiffness =
            active ? handbrakeSidewaysGrip : normalSidewaysGrip;

        WheelCollider.sidewaysFriction = friction;
    }

    public bool TryGetSurface(out SurfaceProfile surface, out float strength)
    {
        surface = null;
        strength = 0f;

        if (!WheelCollider.isGrounded)
            return false;

        if (!WheelCollider.GetGroundHit(out WheelHit hit))
            return false;

        if (!SurfaceManager.Instance.TryGetSurface(
                hit.collider.sharedMaterial,
                out surface))
            return false;

        float slip = Mathf.Abs(hit.sidewaysSlip);
        float load =
            hit.force / WheelCollider.suspensionSpring.spring;

        strength =
            Mathf.Clamp01((slip + load) * surface.intensityMultiplier);

        return true;
    }

    private void Update()
    {
        WheelCollider.GetWorldPose(out position, out rotation);
        wheelModel.transform.position = position;
        wheelModel.transform.rotation = rotation;
    }
}


