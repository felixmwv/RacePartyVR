using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarControl : MonoBehaviour
{
    [Header("Car Properties")]
    public float motorTorque = 5000f;
    public float brakeTorque = 5000f;
    public float maxSpeed = 60f;
    public float steeringRange = 30f;
    public float steeringRangeAtMaxSpeed = 10f;
    public float centreOfGravityOffset = -1f;

    [Header("Camera Setup")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraPoint1;
    [SerializeField] private Transform cameraPoint2;

    [Header("UI")]
    public TMP_Text speedometer;

    private PlayerInput playerInput;
    private Rigidbody rb;
    private WheelControl[] wheels;

    private float vInput;              // drive
    private float hInput;              // steering
    private bool switchCameraPressed;  // camera toggle trigger
    private bool usingPoint1 = true;   // current camera point

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody>();
        wheels = GetComponentsInChildren<WheelControl>();
    }

    void Start()
    {
        // Offset center of mass
        Vector3 com = rb.centerOfMass;
        com.y += centreOfGravityOffset;
        rb.centerOfMass = com;
    }

    void Update()
    {
        if (switchCameraPressed)
        {
            SwitchCamera();
            switchCameraPressed = false;
        }

        // Update UI speedometer
        float kph = rb.linearVelocity.magnitude * 3.6f;
        speedometer.text = kph.ToString("0");
    }

    void FixedUpdate()
    {
        float forwardSpeed = Vector3.Dot(transform.forward, rb.linearVelocity);
        float speedFactor = Mathf.InverseLerp(0, maxSpeed, Mathf.Abs(forwardSpeed));

        float currentMotorTorque = Mathf.Lerp(motorTorque, 0, speedFactor);
        float currentSteerRange = Mathf.Lerp(steeringRange, steeringRangeAtMaxSpeed, speedFactor);

        bool isAccelerating = Mathf.Sign(vInput) == Mathf.Sign(forwardSpeed);

        foreach (var wheel in wheels)
        {
            if (wheel.steerable)
                wheel.WheelCollider.steerAngle = hInput * currentSteerRange;

            if (isAccelerating)
            {
                if (wheel.motorized)
                    wheel.WheelCollider.motorTorque = vInput * currentMotorTorque;

                wheel.WheelCollider.brakeTorque = 0f;
            }
            else
            {
                wheel.WheelCollider.motorTorque = 0f;
                wheel.WheelCollider.brakeTorque = Mathf.Abs(vInput) * brakeTorque;
            }
        }
    }

    // ----------- INPUT CALLBACKS (PlayerInput Unity Events) ------------

    public void OnDrive(InputAction.CallbackContext ctx)
    {
        vInput = ctx.ReadValue<float>();
    }

    public void OnSteer(InputAction.CallbackContext ctx)
    {
        hInput = ctx.ReadValue<float>();
    }

    public void OnBrake(InputAction.CallbackContext ctx)
    {
        // optional if you want
    }

    public void OnSwitchCamera(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            switchCameraPressed = true;
    }

    // --------------------- CAMERA FUNCTIONS ----------------------------

    void SwitchCamera()
    {
        usingPoint1 = !usingPoint1;

        if (usingPoint1)
        {
            playerCamera.transform.SetParent(cameraPoint1);
        }
        else
        {
            playerCamera.transform.SetParent(cameraPoint2);
        }

        playerCamera.transform.localPosition = Vector3.zero;
        playerCamera.transform.localRotation = Quaternion.identity;
    }
}

