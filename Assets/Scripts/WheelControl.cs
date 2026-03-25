using System;
using UnityEngine;

public class WheelControl : MonoBehaviour
{
    public Transform wheelModel;
    [Header("Skid FX")]
    [SerializeField] private ParticleSystem skidSmoke;
    [SerializeField] private float skidThreshold = 0.35f;
    [Header("Dynamic Grip")]
    public float minSidewaysGrip = 0.4f;
    public float maxSidewaysGrip = 1f;
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
        if (skidSmoke != null)
            skidSmoke.Stop();
    }

    public void UpdateDynamicGrip(float rpm01, float torquePressure, float surfaceGrip)
    {
        if ( !motorized ) return;
        if ( !WheelCollider.GetGroundHit( out WheelHit hit ) ) return;

        float slip = Mathf.Abs( hit.sidewaysSlip );
        float slipLoss = Mathf.InverseLerp( 0.4f, 1.2f, slip );
        float torqueLoss = Mathf.Clamp01(torquePressure * 0.6f);
        float gripLoss = Mathf.Lerp( slipLoss, torqueLoss, 0.4f );
        gripLoss = Mathf.Clamp01( gripLoss );

        float sidewaysGrip = Mathf.Lerp( maxSidewaysGrip, minSidewaysGrip, gripLoss );
        sidewaysGrip *= surfaceGrip;

        WheelFrictionCurve sideways = WheelCollider.sidewaysFriction;
        sideways.stiffness = sidewaysGrip;
        WheelCollider.sidewaysFriction = sideways;
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
    private void FixedUpdate()
    {
        SkidCheck();
    }
    private void Update()
    {
        WheelCollider.GetWorldPose(out position, out rotation);
        wheelModel.transform.position = position;
        wheelModel.transform.rotation = rotation;
    }
    private void SkidCheck()
    {
        if (!WheelCollider.isGrounded)
            return;

        if (!WheelCollider.GetGroundHit(out WheelHit hit))
            return;

        float slip = Mathf.Max(
            Mathf.Abs(hit.forwardSlip),
            Mathf.Abs(hit.sidewaysSlip)
        );

        if (slip >= skidThreshold)
        {
            if (!skidSmoke.isPlaying)
                skidSmoke.Play();

            skidSmoke.transform.position =
                hit.point + hit.normal * 0.02f;
        }
        else
        {
            if (skidSmoke.isPlaying)
                skidSmoke.Stop();
        }
    }
}