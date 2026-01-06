using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CarControl : MonoBehaviour
{
    // PUBLIC VARIABLES

    [Header("Car Properties")]
    public float motorTorque = 5000f;
    public float brakeTorque = 5000f;
    public float maxSpeed = 60f;
    public float minSteeringAngle = 0f;
    public float steeringRange = 30f;
    public float steeringRangeAtMaxSpeed = 12f;
    public float centreOfGravityOffset = -0.6f;

    [Header("Steering")]
    public float maxSteerSpeed = 2f;

    [Header("Drivetrain")]
    public float engineTorque = 400f;
    public float finalDrive = 3.4f;
    public float[] gearRatios = { 3.2f, 2.1f, 1.4f, 1.0f, 0.8f };
    public float[] gearMaxSpeeds = { 20f, 35f, 55f, 75f, 95f };
    public float idleRPM = 900f;
    public float redlineRPM = 7000f;

    public AnimationCurve torqueCurve = new AnimationCurve(
        new Keyframe(0f, 0.4f),
        new Keyframe(0.3f, 1.0f),
        new Keyframe(0.6f, 0.85f),
        new Keyframe(0.85f, 0.4f),
        new Keyframe(1f, 0.1f)
    );

    [Header("Camera Setup")]
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private Transform cameraPoint1;
    [SerializeField] private Transform cameraPoint2;
    [SerializeField] private float maximumOrbitDistance = 10f;
    [SerializeField] private float minimumOrbitDistance = 2f;
    [SerializeField] private float rumbleMinSpeedKph = 2f;

    [Header("UI")]
    public TMP_Text speedometer;
    public TMP_Text gearIndicator;
    
    // PRIVATE VARIABLES

    private Rigidbody rb;
    private WheelControl[] wheels;

    private float smoothSteerInput;
    private float throttleInput;
    private float brakeInput;
    private float hInput;

    private float engineRPM;
    private int currentGear = 0;
    public int MaxGear => gearRatios.Length;

    private bool switchCameraPressed;
    private bool usingPoint1 = true;
    private bool rumbling;
    private bool handbrake;

    // UNITY LIFECYCLE

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        wheels = GetComponentsInChildren<WheelControl>();
    }

    void Start()
    {
        Vector3 com = rb.centerOfMass;
        com.y += centreOfGravityOffset;
        rb.centerOfMass = com;

        engineRPM = idleRPM;
    }

    void Update()
    {
        if (switchCameraPressed)
        {
            SwitchCamera();
            switchCameraPressed = false;
        }

        float kph = rb.linearVelocity.magnitude * 3.6f;
        speedometer.text = kph.ToString("0");

        if (currentGear == -1) gearIndicator.text = "R";
        else if (currentGear == 0) gearIndicator.text = "N";
        else gearIndicator.text = currentGear.ToString();
    }

    void FixedUpdate()
    {
        float rumbleLow = 0f;
        float rumbleHigh = 0f;
        int contributingWheels = 0;
        
        float speedKph = rb.linearVelocity.magnitude * 3.6f;
        float speed = Mathf.InverseLerp(0f, maxSpeed, speedKph);
        float speed01 = Mathf.InverseLerp(40f, 160f, speedKph);
        float rumbleSpeed = Mathf.InverseLerp(5f, maxSpeed * 0.8f, speedKph);
        // Steering

        float steerInput = Mathf.Sign(hInput) * hInput * hInput;

        smoothSteerInput = Mathf.MoveTowards(
            smoothSteerInput,
            steerInput,
            maxSteerSpeed * Time.fixedDeltaTime
        );

        float targetSteerRange = Mathf.Lerp(steeringRange, steeringRangeAtMaxSpeed, speed);
        float currentSteerRange = Mathf.Clamp(targetSteerRange, minSteeringAngle, steeringRange);

        // Engine RPM

        float wheelRPM = 0f;
        int driven = 0;

        foreach (var w in wheels)
        {
            if (!w.motorized) continue;
            wheelRPM += w.WheelCollider.rpm;
            driven++;
        }

        if (driven > 0) wheelRPM /= driven;

        if (currentGear > 0)
            engineRPM = Mathf.Abs(wheelRPM) * gearRatios[currentGear - 1] * finalDrive;
        else
            engineRPM = Mathf.Lerp(engineRPM, idleRPM, Time.fixedDeltaTime * 5f);

        engineRPM = Mathf.Clamp(engineRPM, idleRPM, redlineRPM);

        float rpm01 = Mathf.InverseLerp(idleRPM, redlineRPM, engineRPM);
        float torqueFromRPM = torqueCurve.Evaluate(rpm01);

        // Torque Output
        
        float gearLimiter = 1f;

        if (currentGear > 0 && currentGear - 1 < gearMaxSpeeds.Length)
        {
            float limit01 = Mathf.InverseLerp(
                gearMaxSpeeds[currentGear - 1] - 5f,
                gearMaxSpeeds[currentGear - 1],
                speedKph
            );

            gearLimiter = 1f - Mathf.Clamp01(limit01);
        }

        float finalMotorTorque = 0f;

        if (currentGear > 0)
            finalMotorTorque = engineTorque * torqueFromRPM *
                                 gearRatios[currentGear - 1] *
                                 finalDrive * gearLimiter * 10f;
        else if (currentGear == -1)
            finalMotorTorque = engineTorque * torqueFromRPM *
                                 finalDrive * gearLimiter * 6f;

        // Vehicle Dynamics

        Vector3 localAV = transform.InverseTransformDirection(rb.angularVelocity);
        float yawDamping = Mathf.Lerp(0.08f, 0.18f, speed);
        localAV.y *= (1f - yawDamping);
        rb.angularVelocity = transform.TransformDirection(localAV);

        // Wheels

        bool applyingThrottle = throttleInput > 0.01f;
        bool applyingBrake = brakeInput > 0.01f;
        bool revLimiter = engineRPM >= redlineRPM - 100f;
        float throttle01 = Mathf.Clamp01(throttleInput);

        foreach (var wheel in wheels)
        {
            if (wheel.steerable)
                wheel.WheelCollider.steerAngle = smoothSteerInput * currentSteerRange;

            wheel.WheelCollider.motorTorque = 0f;
            wheel.WheelCollider.brakeTorque = 0f;
            
            if (wheel.motorized && handbrake)
            {
                wheel.SetHandbrake(true);
                wheel.WheelCollider.motorTorque = 0f; 
                wheel.WheelCollider.brakeTorque = brakeTorque * 2f;
                continue;
            }
            else
            {
                wheel.SetHandbrake(false);
            }
            if (applyingThrottle && wheel.motorized && !revLimiter)
            {
                float dir = currentGear == -1 ? -1f : 1f;
                wheel.WheelCollider.motorTorque = finalMotorTorque * throttle01 * dir;
            }
            
            if (applyingBrake)
            {
                float bias = wheel.motorized ? 0.8f : 1.2f;
                wheel.WheelCollider.brakeTorque = brakeInput * brakeTorque * bias;
            }
            if (wheel.TryGetSurface(out SurfaceProfile surface, out float strength))
            {
                float low = surface.lowFrequency * strength;
                float high = surface.highFrequency * strength;

                if (surface.speedScaled)
                    high *= speed01;

                rumbleLow += low;
                rumbleHigh += high;
                contributingWheels++;
            }
        }
        if (speedKph > rumbleMinSpeedKph && contributingWheels > 0)
        {
            rumbling = true;

            float finalLow = (rumbleLow / contributingWheels) * rumbleSpeed;
            float finalHigh = (rumbleHigh / contributingWheels) * rumbleSpeed;

            RumbleManager.instance.RumblePulse(
                Mathf.Clamp01(finalLow),
                Mathf.Clamp01(finalHigh),
                Time.fixedDeltaTime * 1.1f
            );
        }
        else if (rumbling)
        {
            rumbling = false;
            RumbleManager.instance.RumblePulse(0f, 0f, 0f);
        }
    }
    
    // INPUT

    public void OnThrottle(InputAction.CallbackContext ctx) => throttleInput = ctx.ReadValue<float>();
    public void OnBrake(InputAction.CallbackContext ctx) => brakeInput = ctx.ReadValue<float>();
    public void OnSteer(InputAction.CallbackContext ctx) => hInput = ctx.ReadValue<float>();
    public void OnHandbrake(InputAction.CallbackContext ctx)
    {
        handbrake = ctx.ReadValue<bool>();
    }


    public void OnShiftUp(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            currentGear = Mathf.Min(currentGear + 1, MaxGear);
    }

    public void OnShiftDown(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        float kph = rb.linearVelocity.magnitude * 3.6f;
        if (currentGear == 0 && kph > 5f) return;

        currentGear = Mathf.Max(currentGear - 1, -1);
    }

    public void OnSwitchCamera(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) switchCameraPressed = true;
    }
    
    // CAMERA

    void SwitchCamera()
    {
        usingPoint1 = !usingPoint1;
        playerCamera.transform.SetParent(usingPoint1 ? cameraPoint1 : cameraPoint2);
        playerCamera.transform.localPosition = Vector3.zero;
        playerCamera.transform.localRotation = Quaternion.identity;
    }
}
