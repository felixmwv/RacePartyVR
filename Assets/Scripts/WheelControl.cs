using UnityEngine;

public class WheelControl : MonoBehaviour
{
    public Transform wheelModel;
    
    [Header("Drift")]
    public float normalSidewaysGrip = 1.0f;
    public float handbrakeSidewaysGrip = 0.3f;

    private bool handbrakeActive;

    public void SetHandbrake(bool active)
    {
        if (handbrakeActive == active)
            return;

        handbrakeActive = active;

        WheelFrictionCurve friction = WheelCollider.sidewaysFriction;
        friction.stiffness = active ? handbrakeSidewaysGrip : normalSidewaysGrip;
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

        if (!SurfaceManager.Instance.TryGetSurface(hit.collider.sharedMaterial, out surface))
            return false;

        float slip = Mathf.Abs(hit.sidewaysSlip);
        float load = hit.force / WheelCollider.suspensionSpring.spring;

        strength = Mathf.Clamp01((slip + load) * surface.intensityMultiplier);
        return true;
    }

    [HideInInspector] public WheelCollider WheelCollider;

    // Create properties for the CarControl script
    // (You should enable/disable these via the 
    // Editor Inspector window)
    public bool steerable;
    public bool motorized;

    Vector3 position;
    Quaternion rotation;

    // Start is called before the first frame update
    private void Start()
    {
        WheelCollider = GetComponent<WheelCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        // Get the Wheel collider's world pose values and
        // use them to set the wheel model's position and rotation
        WheelCollider.GetWorldPose(out position, out rotation);
        wheelModel.transform.position = position;
        wheelModel.transform.rotation = rotation;
    }
}

