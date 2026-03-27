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
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls all car behaviour: steering, engine simulation, gear shifting,
/// braking, torque application, and controller rumble feedback.
/// </summary>
public class CarControlAfter : MonoBehaviour
{
	[Header( "Car Properties" )]
	public float engineTorque = 450f;
	public float brakeTorque = 4000f;
	public float engineBrakeTorque = 300f;
	public float maxSpeed = 220f;

	[Header( "Steering" )]
	public float steeringRange = 22f;
	public float steeringRangeAtMaxSpeed = 8f;
	public float maxSteerSpeed = 0.9f;
	public float wheelBase  = 2.6f;
	public float trackWidth = 1.8f;
	
	[Header( "Drivetrain" )]
	public float finalDrive = 3.4f;
	public float[] gearRatios = { 3.2f, 2.1f, 1.4f, 1.0f, 0.8f };
	public float idleRPM = 900f;
	public float redlineRPM = 7000f;
	public float maxRPM = 9000f;
	public float overrevGripLoss = 1.5f;
	public AnimationCurve torqueCurve;

	[Header( "Drift" )]
	public float wheelspinGripLoss = 1.8f;
	
	[Header( "Rumble" )]
	public float rumbleMinSpeedKph = 2f;

	[Header( "UI" )]
	public TMP_Text speedometer;
	public TMP_Text gearIndicator;
	public TMP_Text rpmMeter;

	private Rigidbody rb;
	private WheelControl[] wheels;

	private float throttleInput;
	private float brakeInput;
	private bool handbrakeInput;
	private float steerInputRaw;
	private float smoothSteer;
	private float selfAlignAngle;
	private float engineRPM;
	
	private int currentGear = 1;

	private bool rumbling;
	public float EngineRPM => this.engineRPM;
	// -------------------------------------------------------------------------

	private void Awake()
	{
		this.rb = GetComponent<Rigidbody>();
		this.wheels = GetComponentsInChildren<WheelControl>();
	}

	private void Start()
	{
		this.engineRPM = this.idleRPM;
	}

	/// <summary>
	/// Updates the HUD every frame: speed, RPM, and current gear label.
	/// </summary>
	private void Update()
	{
		float speedKph = this.rb.linearVelocity.magnitude * 3.6f;

		this.speedometer.text = speedKph.ToString( "0" );
		this.rpmMeter.text = this.engineRPM.ToString( "0" );
		this.rpmMeter.color = this.engineRPM >= this.redlineRPM
			? Color.red
			: Color.white;

		if ( this.currentGear == -1 )
			this.gearIndicator.text = "R";
		else if ( this.currentGear == 0 )
			this.gearIndicator.text = "N";
		else
			this.gearIndicator.text = this.currentGear.ToString();
		if ( this.engineRPM >= this.redlineRPM )
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
		float speed01 = Mathf.InverseLerp( 0f, this.maxSpeed, speedKph );

		this.ApplySteering( speed01 );
		this.UpdateEngineRPM();

		float rpm01        = Mathf.InverseLerp( this.idleRPM, this.redlineRPM, this.engineRPM );
		float overrev01    = Mathf.InverseLerp( this.redlineRPM, this.maxRPM, this.engineRPM );
		bool  revLimiter   = this.engineRPM >= this.maxRPM;

		float baseTorque = this.CalculateBaseTorque( rpm01, revLimiter );
		this.ApplyWheelForces( speedKph, baseTorque, rpm01, overrev01 );
		this.ApplyStabilisation();
	}

	// -------------------------------------------------------------------------
	
	/// <summary>
	/// Steers the front wheels. At high slip angles the wheels are pulled toward
	/// the car's actual travel direction, simulating self-aligning torque.
	/// </summary>
	
	private void ApplySteering( float speed01 )
	{
		float steerInput = this.steerInputRaw * Mathf.Lerp( 1f, 0.6f, speed01 );

		Vector3 localVel      = this.transform.InverseTransformDirection( this.rb.linearVelocity );
		float   slipAngle     = this.rb.linearVelocity.magnitude > 1f
			? Mathf.Abs( Mathf.Atan2( localVel.x, localVel.z ) * Mathf.Rad2Deg )
			: 0f;
		float   slipBoost     = Mathf.InverseLerp( 15f, 50f, slipAngle );
		float   dynamicSteerSpeed = Mathf.Lerp( this.maxSteerSpeed, this.maxSteerSpeed * 2.5f, slipBoost );

		this.smoothSteer = Mathf.MoveTowards(
			this.smoothSteer,
			steerInput,
			dynamicSteerSpeed * Time.fixedDeltaTime
		);

		float steerAngle = Mathf.Lerp( this.steeringRange, this.steeringRangeAtMaxSpeed, speed01 );
		
		// calculate angle between where car is looking and where car is moving
		float alignTorqueBlend = 0f;
		
		if ( this.rb.linearVelocity.magnitude > 3f )
		{
			Vector3 localVelocity = this.transform.InverseTransformDirection( this.rb.linearVelocity );

			// driving backwards inverts steering direction
			float direction = localVelocity.z >= 0f ? 1f : -1f;

			float rawAngle = Mathf.Atan2( localVelocity.x, Mathf.Abs( localVelocity.z ) )
			                 * Mathf.Rad2Deg
			                 * direction;

			// smoothing so self aligning doesn't jump from one side to the other
			selfAlignAngle = Mathf.LerpAngle( selfAlignAngle, rawAngle, Time.fixedDeltaTime * 8f );
			selfAlignAngle = Mathf.Clamp( selfAlignAngle, -this.steeringRange, this.steeringRange );

			float slipMagnitude    = Mathf.Abs( selfAlignAngle ) / this.steeringRange;
			alignTorqueBlend = Mathf.InverseLerp( 0.15f, 0.6f, slipMagnitude );
		}

		// blend between joystick input and self align angle
		float finalAngle = Mathf.Lerp(
			this.smoothSteer * steerAngle,
			selfAlignAngle,
			alignTorqueBlend * 0.5f
		);

		// calculate ackermann steering angle 
		foreach ( WheelControl w in this.wheels )
		{
			if ( !w.steerable ) continue;

			if ( Mathf.Abs( finalAngle ) < 0.01f )
			{
				w.WheelCollider.steerAngle = 0f;
				continue;
			}
			
			float radius = this.wheelBase / Mathf.Tan( finalAngle * Mathf.Deg2Rad );
			
			float innerAngle = Mathf.Rad2Deg * Mathf.Atan( this.wheelBase
			                                               / ( radius - this.trackWidth * 0.5f ) );
			float outerAngle = Mathf.Rad2Deg * Mathf.Atan( this.wheelBase
			                                               / ( radius + this.trackWidth * 0.5f ) );
			
			bool turningLeft = finalAngle > 0f;

			if ( w.side == WheelControl.WheelSide.Left )
				w.WheelCollider.steerAngle = turningLeft ? innerAngle : outerAngle;
			else
				w.WheelCollider.steerAngle = turningLeft ? outerAngle : innerAngle;
		}
	}
	/// <summary>
	/// Drives engine RPM: throttle input raises target RPM toward redline,
	/// releasing throttle lets it fall back toward idle (engine braking).
	/// </summary>
	
	private void UpdateEngineRPM()
	{
		float speedMs     = this.rb.linearVelocity.magnitude;
		float wheelRadius = 0.335f;
		float wheelRPM    = ( speedMs / ( 2f * Mathf.PI * wheelRadius ) ) * 60f;

		float drivetrainRPM = this.currentGear > 0
			? wheelRPM * this.gearRatios[ this.currentGear - 1 ] * this.finalDrive
			: wheelRPM * this.finalDrive;

		float targetRPM;

		if ( this.throttleInput > 0.01f )
		{
			targetRPM = Mathf.Max( this.idleRPM, drivetrainRPM );
		}
		else
		{
			targetRPM = this.idleRPM;
		}
		
		float speedKph    = speedMs * 3.6f;
		float movingBlend = Mathf.InverseLerp( 3f, 20f, speedKph );

		if ( movingBlend < 1f && this.throttleInput > 0.01f )
		{
			float idleBlip = this.idleRPM * ( 1f + this.throttleInput * 0.2f );
			targetRPM = Mathf.Lerp( idleBlip, targetRPM, movingBlend );
		}
		
		float blendSpeed  = this.throttleInput > 0.01f ? 8f : 0.8f;

		this.engineRPM = Mathf.Lerp( this.engineRPM, targetRPM, Time.fixedDeltaTime * blendSpeed );
		this.engineRPM = Mathf.Clamp( this.engineRPM, this.idleRPM, this.maxRPM );
	}
	
	/// <summary>
	/// Returns the drive torque for this physics step; returns 0 if the rev limiter is active.
	/// </summary>
	private float CalculateBaseTorque( float rpm01, bool revLimiter )
	{
		if ( revLimiter ) return 0f;
		if ( this.currentGear == 0 ) return 0f;
		
		float throttleResponse = Mathf.Lerp( this.throttleInput, this.throttleInput * this.throttleInput, 0.25f );
		
		float torque = this.engineTorque
		               * this.torqueCurve.Evaluate( rpm01 )
		               * this.finalDrive
		               * throttleResponse;

		if ( this.currentGear > 0 )
			torque *= this.gearRatios[ this.currentGear - 1 ];
		else
			torque *= this.gearRatios[ 0 ] * 0.6f;

		return torque;
	}

	/// <summary>
	/// Applies motor torque or braking to each motorised wheel and triggers rumble feedback.
	/// </summary>
	private void ApplyWheelForces( float speedKph, float baseTorque, float rpm01, float overrev01 )
	{
		float rumbleLow = 0f;
		float rumbleHigh = 0f;
		int rumbleWheels = 0;

		float dir = this.currentGear == -1 ? -1f : 1f;

		foreach ( WheelControl w in this.wheels )
		{
			w.WheelCollider.motorTorque = 0f;
			w.WheelCollider.brakeTorque = 0f;

			if ( !w.isFront )
				w.SetHandbrake( this.handbrakeInput );
			
			if ( !w.motorized )
				continue;

			if ( this.throttleInput > 0.01f )
			{
				w.WheelCollider.motorTorque = baseTorque * dir;
			}
			else if ( this.rb.linearVelocity.magnitude > 0.5f )
			{
				w.WheelCollider.brakeTorque = this.engineBrakeTorque;
			}

			if ( this.brakeInput > 0.01f )
				w.WheelCollider.brakeTorque = this.brakeInput * this.brakeTorque;
			
			if ( this.handbrakeInput && !w.isFront )
				w.WheelCollider.brakeTorque = this.brakeTorque;
			else
			{
				w.SetHandbrake( false );
			}
			
			float rpmOverThreshold = Mathf.InverseLerp( 0.85f, 1f, rpm01 );
			
			float aggressiveThrottle = Mathf.InverseLerp( 0.85f, 1.0f, this.throttleInput );

			float wheelspinPressure = rpmOverThreshold * aggressiveThrottle * this.wheelspinGripLoss;
			float overrevPressure   = overrev01 * this.overrevGripLoss;
			
			float torquePressure = Mathf.Max( wheelspinPressure, overrevPressure );

			if ( w.TryGetSurface( out SurfaceProfile surface, out float strength ) )
			{
				rumbleLow  += surface.lowFrequency  * strength;
				rumbleHigh += surface.highFrequency * strength;
				rumbleWheels++;

				w.UpdateDynamicGrip( rpm01, torquePressure, surface.sidewaysGrip );
			}
			else
			{
				w.UpdateDynamicGrip( rpm01, torquePressure, 1f );
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
		float yawRate = this.rb.angularVelocity.y;
		
		float speedKph = this.rb.linearVelocity.magnitude * 3.6f;
		if ( speedKph < 20f ) return;
		
		Vector3 localVelocity = this.transform.InverseTransformDirection( this.rb.linearVelocity );
		float slipAngle = Mathf.Atan2( localVelocity.x, localVelocity.z ) * Mathf.Rad2Deg;
		
		float excessSlip = Mathf.Abs( slipAngle ) - 35f;
		if ( excessSlip <= 0f ) return;
		
		float correctionStrength = Mathf.InverseLerp( 0f, 45f, excessSlip );
		float correctionTorque   = -Mathf.Sign( yawRate ) * correctionStrength * 2500f;

		this.rb.AddTorque( 0f, correctionTorque, 0f, ForceMode.Force );
	}
	
	/// <summary>
	/// Sends a per-frame rumble pulse when the car is moving on a surface, or stops it otherwise.
	/// </summary>
	private void UpdateRumble( float speedKph, float rumbleLow, float rumbleHigh, int rumbleWheels )
	{
		if ( speedKph > this.rumbleMinSpeedKph && rumbleWheels > 0 )
		{
			this.rumbling = true;

			RumbleManager.instance.RumblePulse(
				Mathf.Clamp01( rumbleLow / rumbleWheels ),
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
	
	// -------------------------------------------------------------------------
	
	// Input callbacks (bound via Unity Input System)
	public void OnThrottle( InputAction.CallbackContext ctx )
	{
		this.throttleInput = ctx.ReadValue<float>();
		Debug.Log( this.throttleInput );
	}

	public void OnBrake( InputAction.CallbackContext ctx )
	{
		this.brakeInput = ctx.ReadValue<float>();
	}

	public void OnHandBrake(InputAction.CallbackContext ctx)
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

		this.currentGear = Mathf.Min( this.currentGear + 1, this.gearRatios.Length );
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
}
