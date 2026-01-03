using UnityEngine;

public class WheelControl : MonoBehaviour
{
    public Transform wheelModel;
    public bool IsOnCurb(out float curbStrength)
    {
        curbStrength = 0f;

        if (!WheelCollider.isGrounded)
            return false;

        if (!WheelCollider.GetGroundHit(out WheelHit hit))
            return false;

        if (hit.collider.sharedMaterial == null)
            return false;

        if (hit.collider.sharedMaterial.name != "CurbMaterial")
            return false;
        
        float slip = Mathf.Abs(hit.sidewaysSlip);
        float compression = 1f - (hit.force / WheelCollider.suspensionSpring.spring);

        curbStrength = Mathf.Clamp01(slip + compression);
        return true;
    }
    public bool IsOnLine(out float lineStrength)
    {
        lineStrength = 0f;

        if (!WheelCollider.isGrounded)
            return false;

        if (!WheelCollider.GetGroundHit(out WheelHit hit))
            return false;

        if (hit.collider.sharedMaterial == null)
            return false;

        if (hit.collider.sharedMaterial.name != "RoadLineMaterial")
            return false;
        
        float slip = Mathf.Abs(hit.sidewaysSlip);
        float compression = 1f - (hit.force / WheelCollider.suspensionSpring.spring);

        lineStrength = Mathf.Clamp01(slip + compression);
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

