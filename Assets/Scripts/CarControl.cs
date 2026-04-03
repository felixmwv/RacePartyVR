using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CarControl : MonoBehaviour
{
    [Header("Car Properties")]
    public float engineTorque = 450f;
    public float brakeTorque = 4000f;
    public float engineBrakeTorque = 300f;
    public float maxSpeed = 220f;

    [Header("Steering")]
    public float steeringRange = 22f;
    public float steeringRangeAtMaxSpeed = 8f;
    public float maxSteerSpeed = 0.9f;

    [Header("Drivetrain")]
    public float finalDrive = 3.4f;
    public float[] gearRatios = { 3.2f, 2.1f, 1.4f, 1.0f, 0.8f };
    public float idleRPM = 900f;
    public float redlineRPM = 7000f;

    public AnimationCurve torqueCurve;

    [Header("Rumble")]
    public float rumbleMinSpeedKph = 2f;

    [Header("UI")]
    public TMP_Text speedometer;
    public TMP_Text gearIndicator;
    public TMP_Text rpmMeter;

    private Rigidbody rb;
    private WheelControl[] wheels;

    private float throttleInput;
    private float brakeInput;
    private float steerInputRaw;
    private float smoothSteer;

    private float engineRPM;
    private int currentGear = 1;

    private bool rumbling;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        wheels = GetComponentsInChildren<WheelControl>();
    }

    private void Start()
    {
        engineRPM = idleRPM;
    }

    private void Update()
    {
        float speedKph = rb.linearVelocity.magnitude * 3.6f;

        speedometer.text = speedKph.ToString("0");
        rpmMeter.text = engineRPM.ToString("0");

        if (currentGear == -1) gearIndicator.text = "R";
        if (currentGear == 0) gearIndicator.text = "N";
        else gearIndicator.text = currentGear.ToString();
    }

    private void FixedUpdate()
    {
        float speedKph = rb.linearVelocity.magnitude * 3.6f;
        float speed01 = Mathf.InverseLerp(0f, maxSpeed, speedKph);

        // ---------- STEERING ----------
        float steerInput =
            steerInputRaw * Mathf.Lerp(1f, 0.6f, speed01);

        smoothSteer = Mathf.MoveTowards(
            smoothSteer,
            steerInput,
            maxSteerSpeed * Time.fixedDeltaTime
        );

        float steerAngle = Mathf.Lerp(
            steeringRange,
            steeringRangeAtMaxSpeed,
            speed01
        );

        foreach (var w in wheels)
        {
            if (w.steerable)
                w.wheelCollider.steerAngle = smoothSteer * steerAngle;
        }

        // ---------- RPM FROM WHEELS ----------
        float wheelRPM = 0f;
        int driven = 0;

        foreach (var w in wheels)
        {
            if (!w.motorized) continue;
            wheelRPM += Mathf.Abs(w.wheelCollider.rpm);
            driven++;
        }

        if (driven > 0)
            wheelRPM /= driven;

        if (currentGear > 0)
        {
            engineRPM = Mathf.Lerp(
                engineRPM,
                Mathf.Max(
                    idleRPM,
                    wheelRPM * gearRatios[currentGear - 1] * finalDrive
                ),
                Time.fixedDeltaTime * 6f
            );
        }
        else // reverse
        {
            engineRPM = Mathf.Lerp(
                engineRPM,
                Mathf.Max(idleRPM, wheelRPM * finalDrive),
                Time.fixedDeltaTime * 6f
            );
        }

        engineRPM = Mathf.Clamp(engineRPM, idleRPM, redlineRPM);
        float rpm01 = Mathf.InverseLerp(idleRPM, redlineRPM, engineRPM);
        bool revLimiter = engineRPM >= redlineRPM - 150f;

        // ---------- TORQUE ----------
        float baseTorque =
            engineTorque *
            torqueCurve.Evaluate(rpm01) *
            finalDrive *
            throttleInput;

        if (currentGear > 0)
            baseTorque *= gearRatios[currentGear - 1];

        if (revLimiter)
            baseTorque = 0f;

        float dir = currentGear == -1 ? -1f : 1f;

        // ---------- RUMBLE ----------
        float rumbleLow = 0f;
        float rumbleHigh = 0f;
        int rumbleWheels = 0;

        foreach (var w in wheels)
        {
            w.wheelCollider.motorTorque = 0f;
            w.wheelCollider.brakeTorque = 0f;

            if (!w.motorized)
                continue;

            if (throttleInput > 0.01f)
            {
                w.wheelCollider.motorTorque = baseTorque * dir;
            }
            else if (rb.linearVelocity.magnitude > 0.5f)
            {
                w.wheelCollider.brakeTorque = engineBrakeTorque;
            }

            if (brakeInput > 0.01f)
            {
                w.wheelCollider.brakeTorque =
                    brakeInput * brakeTorque;
            }

            if (w.TryGetSurface(out SurfaceProfile surface, out float strength))
            {
                float low = surface.lowFrequency * strength;
                float high = surface.highFrequency * strength;

                rumbleLow += low;
                rumbleHigh += high;
                rumbleWheels++;

                w.UpdateDynamicGrip(rpm01, Mathf.Abs(baseTorque) / 8000f, surface.sidewaysGrip);
            }
            else
            {
                w.UpdateDynamicGrip(rpm01, Mathf.Abs(baseTorque) / 8000f, 1f);
            }
        }

        if (speedKph > rumbleMinSpeedKph && rumbleWheels > 0)
        {
            rumbling = true;

            RumbleManager.instance.RumblePulse(
                Mathf.Clamp01(rumbleLow / rumbleWheels),
                Mathf.Clamp01(rumbleHigh / rumbleWheels),
                Time.fixedDeltaTime
            );
        }
        else if (rumbling)
        {
            rumbling = false;
            RumbleManager.instance.RumblePulse(0f, 0f, 0f);
        }
    }

    // ---------- INPUT ----------
    public void OnThrottle(InputAction.CallbackContext ctx)
    {
        throttleInput = ctx.ReadValue<float>(); 
        Debug.Log(throttleInput);
    }


    public void OnBrake(InputAction.CallbackContext ctx)
    {
        brakeInput = ctx.ReadValue<float>();
    }


    public void OnSteer(InputAction.CallbackContext ctx)
        => steerInputRaw = ctx.ReadValue<float>();

    public void OnShiftUp(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            currentGear = Mathf.Min(currentGear + 1, gearRatios.Length);
        RumbleManager.instance.RumblePulse(0.1f,0.5f, 0.15f);
    }

    public void OnShiftDown(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        currentGear--;

        if (currentGear < -1)
            currentGear = -1;
        RumbleManager.instance.RumblePulse(0.1f,0.5f, 0.15f);
    }
}