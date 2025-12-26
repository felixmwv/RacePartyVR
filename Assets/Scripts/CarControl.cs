using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CarControl : MonoBehaviour
{
    [Header("Car Properties")]
    public float motorTorque = 5000f;
    public float brakeTorque = 5000f;
    public float maxSpeed = 60f;
    public float steeringRange = 36f;
    public float steeringRangeAtMaxSpeed = 12f;
    public float centreOfGravityOffset = -1f;

    [Header("Camera Setup")]
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private Transform cameraPoint1;
    [SerializeField] private Transform cameraPoint2;
    [SerializeField] private float maximumOrbitDistance = 10f;
    [SerializeField] private float minimumOrbitDistance = 2f;
    private float orbitRadius = 5f;

    [Header("UI")]
    public TMP_Text speedometer;
    public TMP_Text gearIndicator;

    [Header("Steering")]
    public float maxSteerSpeed = 2f;
    private float smoothSteerInput;

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

    private float engineRPM;
    private int currentGear = 0;
    public int MaxGear => gearRatios.Length;

    private Rigidbody rb;
    private WheelControl[] wheels;

    private float throttleInput;
    private float brakeInput;
    private float hInput;

    private bool switchCameraPressed;
    private bool usingPoint1 = true;

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
        float speed = rb.linearVelocity.magnitude;
        float speedKph = speed * 3.6f;
        float speed01 = Mathf.InverseLerp(0, maxSpeed, speed);
        
        smoothSteerInput = Mathf.MoveTowards(smoothSteerInput, hInput, maxSteerSpeed * Time.fixedDeltaTime);

        float lowSpeedBoost = Mathf.Lerp(1.4f, 1f, speed01);
        float currentSteerRange = Mathf.Lerp(steeringRange, steeringRangeAtMaxSpeed, speed01) * lowSpeedBoost;

        float steer01 = Mathf.Abs(smoothSteerInput);
        float steerLimiterStrength = Mathf.Lerp(0f, 0.5f, speed01);
        float torqueSteerLimiter = 1f - steer01 * steerLimiterStrength;
        
        float wheelRPM = 0f;
        int driven = 0;

        foreach (var w in wheels)
        {
            if (!w.motorized) continue; wheelRPM += w.WheelCollider.rpm; driven++;
        }

        if (driven > 0) wheelRPM /= driven;

        if (currentGear > 0)
        {
            engineRPM = Mathf.Abs(wheelRPM) * gearRatios[currentGear - 1] * finalDrive;
        }
        else
        {
            engineRPM = Mathf.Lerp(engineRPM, idleRPM, Time.fixedDeltaTime * 5f);
        }

        engineRPM = Mathf.Clamp(engineRPM, idleRPM, redlineRPM);
        float rpm01 = Mathf.InverseLerp(idleRPM, redlineRPM, engineRPM);
        float torqueFromRPM = torqueCurve.Evaluate(rpm01);
        
        float gearLimiter = 1f;

        if (currentGear > 0 && currentGear - 1 < gearMaxSpeeds.Length)
        {
            float limit01 = Mathf.InverseLerp(gearMaxSpeeds[currentGear - 1] - 5f, gearMaxSpeeds[currentGear - 1], speedKph);

            gearLimiter = 1f - Mathf.Clamp01(limit01);
        }

        float finalMotorTorque = 0f;

        if (currentGear > 0)
        {
            finalMotorTorque = engineTorque * torqueFromRPM * gearRatios[currentGear - 1] * finalDrive * torqueSteerLimiter * gearLimiter * 10f;
        }
        else if (currentGear == -1)
        {
            finalMotorTorque = engineTorque * torqueFromRPM * finalDrive * torqueSteerLimiter * gearLimiter * 6f;
        }

        bool applyingThrottle = throttleInput > 0.01f;
        bool applyingBrake = brakeInput > 0.01f;
        bool revLimiter = engineRPM >= redlineRPM - 100f;
        float throttle01 = Mathf.Clamp01(throttleInput);

        float rollingTorque = 0f;
        if (!applyingThrottle && currentGear > 0)
            rollingTorque = engineTorque * 0.05f;
        
        Vector3 localAV = transform.InverseTransformDirection(rb.angularVelocity);
        float yawDamping = Mathf.Lerp(0.15f, 0.5f, speed01);
        localAV.y *= (1f - yawDamping);
        rb.angularVelocity = transform.TransformDirection(localAV);

        foreach (var wheel in wheels)
        {
            if (wheel.steerable)
            {
                wheel.WheelCollider.steerAngle = smoothSteerInput * currentSteerRange;
            }

            wheel.WheelCollider.motorTorque = 0f;
            wheel.WheelCollider.brakeTorque = 0f;
            
            if (applyingThrottle && wheel.motorized && !revLimiter)
            {
                float dir = currentGear == -1 ? -1f : 1f;
                wheel.WheelCollider.motorTorque = (finalMotorTorque * throttle01 + rollingTorque) * dir;
            }
            
            if (applyingBrake)
            {
                float bias = wheel.motorized ? 0.8f : 1.2f;
                wheel.WheelCollider.brakeTorque = brakeInput * brakeTorque * bias;
            }
            
            if (!applyingThrottle && !applyingBrake && currentGear != 0)
            {
                float engineBrake = wheel.motorized ? 150f : 350f;

                wheel.WheelCollider.brakeTorque += engineBrake;
            }
            
            if (!applyingThrottle && wheel.steerable)
            {
                WheelFrictionCurve side = wheel.WheelCollider.sidewaysFriction;
                side.stiffness = 1.15f;
                wheel.WheelCollider.sidewaysFriction = side;
            }
        }
    }

    public void OnThrottle(InputAction.CallbackContext ctx)
    {
        throttleInput = ctx.ReadValue<float>();
    }

    public void OnBrake(InputAction.CallbackContext ctx)
    {
        brakeInput = ctx.ReadValue<float>();
    }

    public void OnSteer(InputAction.CallbackContext ctx)
    {
        hInput = ctx.ReadValue<float>();
    }

    public void OnSwitchCamera(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) switchCameraPressed = true;
    }
    
    public void OnShiftUp(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        currentGear = Mathf.Min(currentGear + 1, MaxGear);
    }

    public void OnShiftDown(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        float kph = rb.linearVelocity.magnitude * 3.6f;
        if (currentGear == 0 && kph > 5f) return;

        currentGear = Mathf.Max(currentGear - 1, -1);
    }

    void SwitchCamera()
    {
        usingPoint1 = !usingPoint1;
        playerCamera.transform.SetParent(usingPoint1 ? cameraPoint1 : cameraPoint2);
        playerCamera.transform.localPosition = Vector3.zero;
        playerCamera.transform.localRotation = Quaternion.identity;
    }
}