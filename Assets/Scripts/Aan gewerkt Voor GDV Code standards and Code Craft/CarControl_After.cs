// bomboclaat
// CarControl.cs
// Conventions used:
//   - C# / Unity naming conventions: PascalCase for methods/properties, camelCase for local/member variables
//   - Access modifiers always explicit
//   - Curly brackets on next line
//   - Padding inside () brackets, after commas, and around operators
//   - Member variables before functions; engine hooks before custom functions
//   - public/protected/private ordering
 
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
 
/// <summary>
/// Controls all car behaviour: steering, engine simulation, gear shifting,
/// braking, torque application, and controller rumble feedback.
/// </summary>
public class CarControlAfter : MonoBehaviour
{
	#region Variables
	[Header( "Handling" )]
	public CarHandlingSO handling;
 
	[Header( "UI" )]
	public TMP_Text speedometer;
	public TMP_Text gearIndicator;
	public TMP_Text rpmMeter;
 
	private Rigidbody rb;
	private WheelControl[] wheels;
 
	private float throttleInput;
	private float brakeInput;
	private bool  handbrakeInput;
	private float steerInputRaw;
	private float smoothSteer;
	private float selfAlignAngle;
	private float engineRpm;
 
	private int  currentGear = 1;
	private bool rumbling;
 
	public float EngineRpm => this.engineRpm;
	#endregion
 
	#region Event Functions
 
	private void Awake()
	{
		this.rb = GetComponent<Rigidbody>();
		this.wheels = GetComponentsInChildren<WheelControl>();
	}
 
	private void Start()
	{
		this.engineRpm = this.handling.idleRpm;
	}
 
	/// <summary>
	/// Updates the HUD every frame: speed, RPM, and current gear label.
	/// </summary>
	private void Update()
	{
		float speedKph = this.rb.linearVelocity.magnitude * 3.6f;
 
		this.speedometer.text = speedKph.ToString( "0" );
		this.rpmMeter.text = this.engineRpm.ToString( "0" );
		this.rpmMeter.color = this.engineRpm >= this.handling.redlineRpm ? Color.red : Color.white;
 
		if ( this.currentGear == -1 )
			this.gearIndicator.text = "R";
		else if ( this.currentGear == 0 )
			this.gearIndicator.text = "N";
		else
			this.gearIndicator.text = this.currentGear.ToString();
 
		if ( this.engineRpm >= this.handling.redlineRpm )
		{
			float tremor = ( Mathf.Sin( Time.time * 40f ) + 1f ) * 0.5f;
			RumbleManager.instance.RumblePulse( tremor * 0.15f, tremor * 0.4f, Time.deltaTime );
		}
	}
 
	/// <summary>
	/// Physics step: applies steering, calculates engine RPM, applies torque/braking,
	/// and sends rumble feedback based on surface type.
	/// </summary>
	private void FixedUpdate()
	{
		float speedKph = this.rb.linearVelocity.magnitude * 3.6f;
		float speed01 = Mathf.InverseLerp( 0f, this.handling.maxSpeed, speedKph );
 
		this.ApplySteering( speed01 );
		this.UpdateEngineRpm();
 
		float rpm01 = Mathf.InverseLerp( this.handling.idleRpm, this.handling.redlineRpm, this.engineRpm );
		float overrev01 = Mathf.InverseLerp( this.handling.redlineRpm, this.handling.maxRpm, this.engineRpm );
		bool  revLimiter = this.engineRpm >= this.handling.maxRpm;
 
		float baseTorque = this.CalculateBaseTorque( rpm01, revLimiter );
		this.ApplyWheelForces( speedKph, baseTorque, rpm01, overrev01 );
		this.ApplyStabilisation();
	}
	#endregion
 
	#region Driving Functions
	/// <summary>
	/// Steers the front wheels. At high slip angles the wheels are pulled toward
	/// the car's actual travel direction, simulating self-aligning torque.
	/// </summary>
	private void ApplySteering( float speed01 )
	{
		float steerInput = this.steerInputRaw * Mathf.Lerp( 1f, 0.6f, speed01 );
 
		// Dynamic steer speed — faster counter-steer at high slip angles
		Vector3 localVel = this.transform.InverseTransformDirection( this.rb.linearVelocity );
		
		float slipAngle = this.rb.linearVelocity.magnitude > 1f ? Mathf.Abs( Mathf.Atan2( localVel.x, localVel.z ) * Mathf.Rad2Deg ) : 0f;
		
		float slipBoost = Mathf.InverseLerp( 15f, 50f, slipAngle );
		
		float dynamicSteerSpeed = Mathf.Lerp( this.handling.maxSteerSpeed, this.handling.maxSteerSpeed * 2.5f, slipBoost );
 
		this.smoothSteer = Mathf.MoveTowards( this.smoothSteer, steerInput, dynamicSteerSpeed * Time.fixedDeltaTime );
 
		float steerAngle = Mathf.Lerp( this.handling.steeringRange, this.handling.steeringRangeAtMaxSpeed, speed01 );
 
		// Calculate angle between where car is looking and where car is moving
		float alignTorqueBlend = 0f;
 
		if ( this.rb.linearVelocity.magnitude > 3f )
		{
			Vector3 localVelocity = this.transform.InverseTransformDirection( this.rb.linearVelocity );
 
			// Driving backwards inverts steering direction
			float direction = localVelocity.z >= 0f ? 1f : -1f;
 
			float rawAngle = Mathf.Atan2( localVelocity.x, Mathf.Abs( localVelocity.z ) ) * Mathf.Rad2Deg * direction;
 
			// Smoothing so self aligning doesn't jump from one side to the other
			this.selfAlignAngle = Mathf.LerpAngle(this.selfAlignAngle, rawAngle, Time.fixedDeltaTime * 8f);
			this.selfAlignAngle = Mathf.Clamp(this.selfAlignAngle, -this.handling.steeringRange, this.handling.steeringRange);
 
			float slipMagnitude = Mathf.Abs( this.selfAlignAngle ) / this.handling.steeringRange;
			alignTorqueBlend = Mathf.InverseLerp( 0.15f, 0.6f, slipMagnitude );
		}
 
		// Blend between joystick input and self align angle
		float finalAngle = Mathf.Lerp(this.smoothSteer * steerAngle, this.selfAlignAngle, alignTorqueBlend * 0.5f);
 
		// Apply Ackermann steering angle per wheel
		foreach ( WheelControl w in this.wheels )
		{
			if ( !w.steerable ) continue;
 
			if ( Mathf.Abs( finalAngle ) < 0.01f )
			{
				w.wheelCollider.steerAngle = 0f;
				continue;
			}
 
			float radius = this.handling.wheelBase / Mathf.Tan( finalAngle * Mathf.Deg2Rad );
			float innerAngle = Mathf.Rad2Deg * Mathf.Atan(this.handling.wheelBase / ( radius - this.handling.trackWidth * 0.5f ));
			float outerAngle = Mathf.Rad2Deg * Mathf.Atan(this.handling.wheelBase / ( radius + this.handling.trackWidth * 0.5f ));
 
			bool turningLeft = finalAngle > 0f;
 
			if ( w.side == WheelControl.WheelSide.Left )
				w.wheelCollider.steerAngle = turningLeft ? innerAngle : outerAngle;
			else
				w.wheelCollider.steerAngle = turningLeft ? outerAngle : innerAngle;
		}
	}
 
	/// <summary>
	/// Drives engine RPM from car velocity rather than wheel RPM,
	/// preventing spinning wheels from spiking the RPM.
	/// </summary>
	private void UpdateEngineRpm()
	{
		float speedMs = this.rb.linearVelocity.magnitude;
		float wheelRpm = ( speedMs / ( 2f * Mathf.PI * 0.335f ) ) * 60f;
 
		float drivetrainRpm = this.currentGear > 0 ? wheelRpm 
		* this.handling.gearRatios[ this.currentGear - 1 ] * this.handling.finalDrive : wheelRpm * this.handling.finalDrive;
 
		float targetRpm;
 
		if ( this.throttleInput > 0.01f )
			targetRpm = Mathf.Max( this.handling.idleRpm, drivetrainRpm );
		else
			targetRpm = this.handling.idleRpm;
 
		// Idle blip when moving slowly — purely for audio feel
		float speedKph    = speedMs * 3.6f;
		float movingBlend = Mathf.InverseLerp( 3f, 20f, speedKph );
 
		if ( movingBlend < 1f && this.throttleInput > 0.01f )
		{
			float idleBlip = this.handling.idleRpm * ( 1f + this.throttleInput * 0.2f );
			targetRpm = Mathf.Lerp( idleBlip, targetRpm, movingBlend );
		}
 
		// RPM drops slowly without throttle (engine braking feel)
		float blendSpeed = this.throttleInput > 0.01f ? 8f : 0.8f;
 
		this.engineRpm = Mathf.Lerp( this.engineRpm, targetRpm, Time.fixedDeltaTime * blendSpeed );
		this.engineRpm = Mathf.Clamp( this.engineRpm, this.handling.idleRpm, this.handling.maxRpm );
	}
 
	/// <summary>
	/// Returns the drive torque for this physics step; returns 0 if the rev limiter is active.
	/// </summary>
	private float CalculateBaseTorque( float rpm01, bool revLimiter )
	{
		if ( revLimiter ) return 0f;
		if ( this.currentGear == 0 ) return 0f;
 
		// Slight quadratic throttle response — small inputs give less power
		float throttleResponse = Mathf.Lerp(this.throttleInput, this.throttleInput * this.throttleInput, 0.25f);
 
		float torque = this.handling.engineTorque
		               * this.handling.torqueCurve.Evaluate( rpm01 )
		               * this.handling.finalDrive
		               * throttleResponse;
 
		if ( this.currentGear > 0 )
			torque *= this.handling.gearRatios[ this.currentGear - 1 ];
		else
			torque *= this.handling.gearRatios[ 0 ] * 0.6f;
 
		return torque;
	}
 
	/// <summary>
	/// Applies motor torque or braking to each motorised wheel and triggers rumble feedback.
	/// </summary>
	private void ApplyWheelForces( float speedKph, float baseTorque, float rpm01, float overrev01 )
	{
		float rumbleLow   = 0f;
		float rumbleHigh  = 0f;
		int   rumbleWheels = 0;
 
		float dir = this.currentGear == -1 ? -1f : 1f;
 
		foreach ( WheelControl w in this.wheels )
		{
			w.wheelCollider.motorTorque = 0f;
			w.wheelCollider.brakeTorque = 0f;
 
			// Handbrake only affects rear wheels
			if ( !w.isFront )
				w.SetHandbrake( this.handbrakeInput );
 
			// Braking applies to all wheels
			if ( this.brakeInput > 0.01f )
			{
				w.wheelCollider.brakeTorque = this.brakeInput * this.handling.brakeTorque;
				w.SetBrakeGrip( true );
			}
			else
			{
				w.SetBrakeGrip( false );
			}
 
			// Handbrake on front wheels releases brake torque
			if ( this.handbrakeInput && w.isFront )
				w.wheelCollider.brakeTorque = 0f;
 
			if ( !w.motorized )
				continue;
 
			if ( this.throttleInput > 0.01f )
			{
				w.wheelCollider.motorTorque = baseTorque * dir;
			}
			else if ( this.rb.linearVelocity.magnitude > 0.5f )
			{
				w.wheelCollider.brakeTorque = this.handling.engineBrakeTorque;
			}
 
			if ( this.handbrakeInput && !w.isFront )
				w.wheelCollider.brakeTorque = this.handling.brakeTorque;
 
			float rpmOverThreshold = Mathf.InverseLerp( 0.85f, 1f, rpm01 );
			float aggressiveThrottle = Mathf.InverseLerp( 0.85f, 1.0f, this.throttleInput );
			float wheelspinPressure = rpmOverThreshold * aggressiveThrottle * this.handling.wheelspinGripLoss;
			float overrevPressure = overrev01 * this.handling.overrevGripLoss;
			float torquePressure = Mathf.Max( wheelspinPressure, overrevPressure );
 
			if ( w.TryGetSurface( out SurfaceProfile surface, out float strength ) )
			{
				rumbleLow    += surface.lowFrequency  * strength;
				rumbleHigh   += surface.highFrequency * strength;
				rumbleWheels++;
 
				w.UpdateDynamicGrip( rpm01, torquePressure, surface.sidewaysGrip, this.handbrakeInput, this.brakeInput > 0.01f );
			}
			else
			{
				w.UpdateDynamicGrip( rpm01, torquePressure, 1f, this.handbrakeInput, this.brakeInput > 0.01f );
			}
		}
 
		this.UpdateRumble( speedKph, rumbleLow, rumbleHigh, rumbleWheels );
	}
 
	/// <summary>
	/// Applies a subtle corrective torque when the car spins beyond a recoverable angle,
	/// trying to give an arcade like forgiving drift feeling.
	/// </summary>
	private void ApplyStabilisation()
	{
		float yawRate  = this.rb.angularVelocity.y;
		float speedKph = this.rb.linearVelocity.magnitude * 3.6f;
 
		if ( speedKph < 20f ) return;
 
		Vector3 localVelocity = this.transform.InverseTransformDirection( this.rb.linearVelocity );
		float slipAngle = Mathf.Atan2( localVelocity.x, localVelocity.z ) * Mathf.Rad2Deg;
 
		float excessSlip = Mathf.Abs( slipAngle ) - 30f;
		if ( excessSlip <= 0f ) return;
 
		float correctionStrength = Mathf.InverseLerp( 0f, 45f, excessSlip );
		float correctionTorque = -Mathf.Sign( yawRate ) * correctionStrength * 6000f;
 
		this.rb.AddTorque( 0f, correctionTorque, 0f, ForceMode.Force );
	}
	#endregion
 
	#region Rumble
	/// <summary>
	/// Sends a per-frame rumble pulse when the car is moving on a surface, or stops it otherwise.
	/// </summary>
	private void UpdateRumble( float speedKph, float rumbleLow, float rumbleHigh, int rumbleWheels )
	{
		if ( speedKph > this.handling.rumbleMinSpeedKph && rumbleWheels > 0 )
		{
			this.rumbling = true;
 
			RumbleManager.instance.RumblePulse(
				Mathf.Clamp01( rumbleLow  / rumbleWheels ),
				Mathf.Clamp01( rumbleHigh / rumbleWheels ),
				Time.fixedDeltaTime
			);
		}
		else if ( this.rumbling )
		{
			this.rumbling = false;
			RumbleManager.instance.RumblePulse( 0f, 0f, 0f );
		}
	}
	#endregion
 
	#region Input Callbacks
	public void OnThrottle( InputAction.CallbackContext ctx )
	{
		this.throttleInput = ctx.ReadValue<float>();
	}
 
	public void OnBrake( InputAction.CallbackContext ctx )
	{
		this.brakeInput = ctx.ReadValue<float>();
	}
 
	public void OnHandBrake( InputAction.CallbackContext ctx )
	{
		this.handbrakeInput = ctx.ReadValue<float>() > 0.5f;
	}
 
	public void OnSteer( InputAction.CallbackContext ctx )
	{
		this.steerInputRaw = ctx.ReadValue<float>();
	}
 
	/// <summary>
	/// Shifts up one gear (capped at the highest gear) and triggers a short rumble.
	/// </summary>
	public void OnShiftUp( InputAction.CallbackContext ctx )
	{
		if ( !ctx.performed )
			return;
 
		this.currentGear = Mathf.Min( this.currentGear + 1, this.handling.gearRatios.Length );
		RumbleManager.instance.RumblePulse( 0.1f, 0.5f, 0.15f );
	}
 
	/// <summary>
	/// Shifts down one gear (minimum reverse) and triggers a short rumble.
	/// </summary>
	public void OnShiftDown( InputAction.CallbackContext ctx )
	{
		if ( !ctx.performed )
			return;
 
		this.currentGear--;
 
		if ( this.currentGear < -1 )
			this.currentGear = -1;
 
		RumbleManager.instance.RumblePulse( 0.1f, 0.5f, 0.15f );
	}
	#endregion
}
