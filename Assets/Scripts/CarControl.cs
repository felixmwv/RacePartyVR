using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CarControl : MonoBehaviour
{
    // =======================
    // PUBLIC VARIABLES
    // =======================

    [Header("Car Properties")]
    public float brakeTorque = 5000f;
    public float maxSpeed = 200f;
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
    public float speedResponse = 10f;

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
    public TMP_Text rpmMeter;

    // =======================
    // PRIVATE VARIABLES
    // =======================

    private Rigidbody rb;
    private WheelControl[] wheels;

    private float smoothSteerInput;
    private float throttleInput;
    private float brakeInput;
    private float steerInputRaw;

    private float engineRPM;
    private int currentGear = 0;
    public int MaxGear => gearRatios.Length;

    private bool switchCameraPressed;
    private bool usingPoint1 = true;
    private bool rumbling;
    private bool handbrake;

    // =======================
    // UNITY LIFECYCLE
    // =======================

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        wheels = GetComponentsInChildren<WheelControl>();
    }

    private void Start()
    {
        Vector3 com = rb.centerOfMass;
        com.y += centreOfGravityOffset;
        rb.centerOfMass = com;

        engineRPM = idleRPM;
    }

    private void Update()
    {
        if (switchCameraPressed)
        {
            SwitchCamera();
            switchCameraPressed = false;
        }

        float speedKph = rb.linearVelocity.magnitude * 3.6f;

        speedometer.text = speedKph.ToString("0");
        rpmMeter.text = engineRPM.ToString("0");

        if (currentGear == -1) gearIndicator.text = "R";
        else if (currentGear == 0) gearIndicator.text = "N";
        else gearIndicator.text = currentGear.ToString();
    }

    private void FixedUpdate()
    {
        // =======================
        // SPEED VALUES
        // =======================

        float speedKph = rb.linearVelocity.magnitude * 3.6f;
        float speed01 = Mathf.InverseLerp(0f, maxSpeed, speedKph);
        float steerSpeed01 = Mathf.InverseLerp(80f, 200f, speedKph);
        float rumbleSpeed01 = Mathf.InverseLerp(5f, maxSpeed * 0.8f, speedKph);

        // =======================
        // INPUT PROCESSING
        // =======================

        float throttle01 = Mathf.Clamp01(throttleInput);
        float brake01 = Mathf.Clamp01(brakeInput);
        bool applyingThrottle = throttle01 > 0.01f;
        bool applyingBrake = brake01 > 0.01f;

        float steerInput = Mathf.Sign(steerInputRaw) * steerInputRaw * steerInputRaw;

        float targetSteerRange =
            Mathf.Lerp(steeringRange, steeringRangeAtMaxSpeed, speed01);

        float currentSteerRange =
            Mathf.Clamp(targetSteerRange, minSteeringAngle, steeringRange);
        float steer = smoothSteerInput * currentSteerRange;

        float inner = steer * 1.15f;
        float outer = steer * 0.85f;

        foreach (var wheel in wheels)
        {
            if (!wheel.steerable)
                continue;

            if (steer > 0f) // rechtsom
            {
                wheel.WheelCollider.steerAngle =
                    wheel.side == WheelControl.WheelSide.Left ? inner : outer;
            }
            else // linksom
            {
                wheel.WheelCollider.steerAngle =
                    wheel.side == WheelControl.WheelSide.Left ? outer : inner;
            }
        }
        
        smoothSteerInput = Mathf.MoveTowards(
            smoothSteerInput,
            steerInput,
            maxSteerSpeed * Time.fixedDeltaTime
        );

        if (Mathf.Abs(steerInputRaw) < 0.01f)
        {
            float recenterStrength = Mathf.Lerp(2f, 8f, speed01);
            smoothSteerInput = Mathf.MoveTowards(
                smoothSteerInput,
                0f,
                recenterStrength * Time.fixedDeltaTime
            );
        }

        // =======================
        // ENGINE RPM FROM WHEELS
        // =======================

        float wheelRPM = 0f;
        int drivenWheels = 0;

        foreach (var w in wheels)
        {
            if (!w.motorized) continue;
            wheelRPM += w.WheelCollider.rpm;
            drivenWheels++;
        }

        if (drivenWheels > 0)
            wheelRPM /= drivenWheels;

        if (currentGear > 0)
            engineRPM = Mathf.Abs(wheelRPM) * gearRatios[currentGear - 1] * finalDrive;
        else
            engineRPM = Mathf.Lerp(engineRPM, idleRPM, Time.fixedDeltaTime * 5f);

        engineRPM = Mathf.Clamp(engineRPM, idleRPM, redlineRPM);

        float rpm01 = Mathf.InverseLerp(idleRPM, redlineRPM, engineRPM);
        bool revLimiter = engineRPM >= redlineRPM - 100f;

        // =======================
        // GEAR SPEED TARGET
        // =======================

        float gearMinSpeed;
        float gearMaxSpeed;

        if (currentGear <= 0)
        {
            gearMinSpeed = 0f;
            gearMaxSpeed = maxSpeed;
        }
        else
        {
            gearMaxSpeed = gearMaxSpeeds[currentGear - 1];
            gearMinSpeed = currentGear > 1 ? gearMaxSpeeds[currentGear - 2] : 0f;
        }

        float targetSpeedKph =
            Mathf.Lerp(gearMinSpeed, gearMaxSpeed, throttle01);

        float speedError = targetSpeedKph - speedKph;
        float speedError01 = Mathf.Clamp01(speedError / speedResponse);

        // =======================
        // TORQUE CALC
        // =======================

        float torqueFromRPM = torqueCurve.Evaluate(rpm01);
        float finalMotorTorque = 0f;

        if (currentGear > 0 && speedError > 0f && !revLimiter)
        {
            finalMotorTorque =
                engineTorque *
                torqueFromRPM *
                gearRatios[currentGear - 1] *
                finalDrive *
                speedError01 *
                10f;
        }
        else if (currentGear == -1 && speedError > 0f)
        {
            finalMotorTorque =
                engineTorque *
                torqueFromRPM *
                finalDrive *
                speedError01 *
                6f;
        }

        float torque01 = Mathf.Clamp01(finalMotorTorque / 8000f);

        // =======================
        // YAW DAMPING
        // =======================

        float yawDamping = Mathf.Lerp(0.05f, 0.25f, steerSpeed01);
        Vector3 localAV = transform.InverseTransformDirection(rb.angularVelocity);
        localAV.y *= (1f - yawDamping);
        rb.angularVelocity = transform.TransformDirection(localAV);

        // =======================
        // WHEELS + RUMBLE
        // =======================

        float rumbleLow = 0f;
        float rumbleHigh = 0f;
        int contributingWheels = 0;

        foreach (var wheel in wheels)
        {
            if (wheel.steerable)
                wheel.WheelCollider.steerAngle =
                    smoothSteerInput * currentSteerRange;

            wheel.WheelCollider.motorTorque = 0f;
            wheel.WheelCollider.brakeTorque = 0f;

            if (wheel.motorized && handbrake)
            {
                wheel.SetHandbrake(true);
                wheel.WheelCollider.brakeTorque = brakeTorque * 2f;
                continue;
            }

            wheel.SetHandbrake(false);

            if (applyingThrottle && wheel.motorized && !revLimiter)
            {
                float dir = currentGear == -1 ? -1f : 1f;
                wheel.WheelCollider.motorTorque =
                    finalMotorTorque * throttle01 * dir;
            }
            bool isFront = wheel.transform.localPosition.z > 0f;

            float steer01 = Mathf.Abs(smoothSteerInput);

            float brakeStrength = brake01;
            float bias = isFront ? 1.2f : 0.6f;
            if (isFront)
            {
                // minder remkracht op voorwielen tijdens sturen
                brakeStrength *= Mathf.Lerp(1f, 0.4f, steer01);
            }

            

            if (applyingBrake)
            {
                wheel.WheelCollider.brakeTorque = brake01 * brakeTorque * bias;
                wheel.SetBrakeGrip(true);
            }
            else
            {
                wheel.SetBrakeGrip(false);
            }

            if (wheel.TryGetSurface(out SurfaceProfile surface, out float strength))
            {
                float low = surface.lowFrequency * strength;
                float high = surface.highFrequency * strength;

                if (surface.speedScaled)
                    high *= steerSpeed01;

                rumbleLow += low;
                rumbleHigh += high;
                contributingWheels++;

                wheel.UpdateDynamicGrip(rpm01, torque01, surface.sidewaysGrip);
            }
            else
            {
                wheel.UpdateDynamicGrip(rpm01, torque01, 1f);
            }
        }

        if (speedKph > rumbleMinSpeedKph && contributingWheels > 0)
        {
            rumbling = true;

            float finalLow =
                (rumbleLow / contributingWheels) * rumbleSpeed01;

            float finalHigh =
                (rumbleHigh / contributingWheels) * rumbleSpeed01;

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

    // =======================
    // INPUT
    // =======================

    public void OnThrottle(InputAction.CallbackContext ctx)
        => throttleInput = ctx.ReadValue<float>();

    public void OnBrake(InputAction.CallbackContext ctx)
        => brakeInput = ctx.ReadValue<float>();

    public void OnSteer(InputAction.CallbackContext ctx)
        => steerInputRaw = ctx.ReadValue<float>();

    public void OnHandbrake(InputAction.CallbackContext ctx)
        => handbrake = ctx.ReadValueAsButton();

    public void OnShiftUp(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            currentGear = Mathf.Min(currentGear + 1, MaxGear);
    }

    public void OnShiftDown(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        float speedKph = rb.linearVelocity.magnitude * 3.6f;
        if (currentGear == 0 && speedKph > 5f) return;

        currentGear = Mathf.Max(currentGear - 1, -1);
    }

    public void OnSwitchCamera(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            switchCameraPressed = true;
    }

    // =======================
    // CAMERA
    // =======================

    private void SwitchCamera()
    {
        usingPoint1 = !usingPoint1;
        playerCamera.transform.SetParent(usingPoint1 ? cameraPoint1 : cameraPoint2);
        playerCamera.transform.localPosition = Vector3.zero;
        playerCamera.transform.localRotation = Quaternion.identity;
    }
}

