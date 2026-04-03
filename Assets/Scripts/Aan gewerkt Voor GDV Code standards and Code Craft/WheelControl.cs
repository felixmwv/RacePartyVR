using UnityEngine;
 
/// <summary>
/// Controls wheel behaviour, grip and braking.
/// Grip values are loaded from CarHandlingSO if assigned.
/// </summary>
public class WheelControl : MonoBehaviour
{
    #region Variables
    [Header( "Wheel Settings" )]
    public WheelSide side;
    public bool isFront;
    public bool steerable;
    public bool motorized;
 
    [Header( "Handling" )]
    public CarHandlingSO handling;
 
    [Header( "Wheel Model" )]
    public Transform wheelModel;
    public Vector3 modelOffset = Vector3.zero;
 
    [Header( "Grip" )]
    public float minSidewaysGrip = 0.4f;
    public float maxSidewaysGrip = 1f;
    public float normalSidewaysGrip = 1.0f;
 
    [Header( "Braking" )]
    public float brakeSidewaysGrip = 0.75f;
 
    [Header( "Drift" )]
    public float handbrakeSidewaysGrip = 0.3f;
 
    [Header( "Skid FX" )]
    [SerializeField] private ParticleSystem skidSmoke;
    [SerializeField] private float skidThreshold = 0.35f;
 
    [HideInInspector] public WheelCollider wheelCollider;
 
    private bool handbrakeActive;
    private Vector3 position;
    private Quaternion rotation;
 
    public enum WheelSide { Left, Right }
    #endregion
 
    #region Event Functions
    private void Start()
    {
        this.wheelCollider = GetComponent<WheelCollider>();
        this.ApplyHandlingProfile();
 
        if ( this.skidSmoke != null )
            this.skidSmoke.Stop();
    }
 
    private void Update()
    {
        this.wheelCollider.GetWorldPose( out this.position, out this.rotation );
        this.wheelModel.transform.position = this.position + this.transform.TransformDirection( this.modelOffset );
        this.wheelModel.transform.rotation = this.rotation;
    }
 
    private void FixedUpdate()
    {
        this.SkidCheck();
    }
    #endregion
 
    #region Handling Profile
    /// <summary>
    /// Loads grip values from the handling SO based on front or rear position.
    /// Call this from Start or whenever the SO changes.
    /// </summary>
    public void ApplyHandlingProfile()
    {
        if ( this.handling == null ) return;
 
        if ( this.isFront )
        {
            this.minSidewaysGrip = this.handling.frontMinSidewaysGrip;
            this.maxSidewaysGrip = this.handling.frontMaxSidewaysGrip;
            this.normalSidewaysGrip = this.handling.frontNormalSidewaysGrip;
            this.brakeSidewaysGrip = this.handling.frontBrakeSidewaysGrip;
            this.handbrakeSidewaysGrip = this.handling.frontHandbrakeSidewaysGrip;
        }
        else
        {
            this.minSidewaysGrip = this.handling.rearMinSidewaysGrip;
            this.maxSidewaysGrip = this.handling.rearMaxSidewaysGrip;
            this.normalSidewaysGrip = this.handling.rearNormalSidewaysGrip;
            this.brakeSidewaysGrip = this.handling.rearBrakeSidewaysGrip;
            this.handbrakeSidewaysGrip = this.handling.rearHandbrakeSidewaysGrip;
        }
    }
    #endregion
 
    #region Grip
    /// <summary>
    /// Changes sideways grip dynamically based on slip, torque pressure and surface.
    /// </summary>
    public void UpdateDynamicGrip(float rpm01, float torquePressure, float surfaceGrip, bool handbrake = false, bool braking = false)
    {
        if ( !this.motorized ) return;
        if ( !this.wheelCollider.GetGroundHit( out WheelHit hit ) ) return;
 
        float slip = Mathf.Abs( hit.sidewaysSlip );
        float slipLoss = Mathf.InverseLerp( 0.4f, 1.2f, slip );
        float torqueLoss = Mathf.Clamp01( torquePressure * 0.6f );
        float gripLoss = Mathf.Clamp01( Mathf.Lerp( slipLoss, torqueLoss, 0.4f ) );
 
        float sidewaysGrip = Mathf.Lerp( this.maxSidewaysGrip, this.minSidewaysGrip, gripLoss );
        sidewaysGrip *= surfaceGrip;
 
        if ( handbrake && !this.isFront )
            sidewaysGrip = Mathf.Min( sidewaysGrip, this.handbrakeSidewaysGrip );
        else if ( braking )
            sidewaysGrip = Mathf.Min( sidewaysGrip, this.brakeSidewaysGrip );
 
        WheelFrictionCurve sideways = this.wheelCollider.sidewaysFriction;
        sideways.stiffness = sidewaysGrip;
        this.wheelCollider.sidewaysFriction = sideways;
    }
 
    /// <summary>
    /// Checks which surface the wheel is on via the SurfaceManager.
    /// </summary>
    public bool TryGetSurface( out SurfaceProfile surface, out float strength )
    {
        surface  = null;
        strength = 0f;
 
        if ( !this.wheelCollider.isGrounded )
            return false;
 
        if ( !this.wheelCollider.GetGroundHit( out WheelHit hit ) )
            return false;
 
        if ( !SurfaceManager.Instance.TryGetSurface( hit.collider.sharedMaterial, out surface ) )
            return false;
 
        float slip = Mathf.Abs( hit.sidewaysSlip );
        float load = hit.force / this.wheelCollider.suspensionSpring.spring;
 
        strength = Mathf.Clamp01( ( slip + load ) * surface.intensityMultiplier );
 
        return true;
    }
 
    /// <summary>
    /// Checks if the car is skidding and plays the smoke effect.
    /// </summary>
    private void SkidCheck()
    {
        if ( !this.wheelCollider.GetGroundHit( out WheelHit hit ) )
        {
            if ( this.skidSmoke.isPlaying )
                this.skidSmoke.Stop();
            return;
        }

        float combinedSlip = new Vector2( hit.forwardSlip, hit.sidewaysSlip ).magnitude;

        if ( combinedSlip >= this.skidThreshold )
        {
            if ( !this.skidSmoke.isPlaying )
                this.skidSmoke.Play();

            this.skidSmoke.transform.position = hit.point + hit.normal * 0.02f;
        }
        else
        {
            if ( this.skidSmoke.isPlaying )
                this.skidSmoke.Stop();
        }
    }
    #endregion
 
    #region Braking
    /// <summary>
    /// Adjusts sideways grip when braking.
    /// </summary>
    public void SetBrakeGrip( bool braking )
    {
        if ( this.handbrakeActive )
            return;
 
        WheelFrictionCurve friction = this.wheelCollider.sidewaysFriction;
        friction.stiffness = braking ? this.brakeSidewaysGrip : this.normalSidewaysGrip;
        this.wheelCollider.sidewaysFriction = friction;
    }
 
    /// <summary>
    /// Activates or deactivates handbrake grip reduction on this wheel.
    /// </summary>
    public void SetHandbrake( bool active )
    {
        if ( this.handbrakeActive == active )
            return;
 
        this.handbrakeActive = active;
 
        WheelFrictionCurve friction = this.wheelCollider.sidewaysFriction;
        friction.stiffness = active ? this.handbrakeSidewaysGrip : this.normalSidewaysGrip;
        this.wheelCollider.sidewaysFriction = friction;
    }
    #endregion
}