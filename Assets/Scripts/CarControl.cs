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
    [SerializeField] private bool Player2;
    [SerializeField] private Camera cam1;
    [SerializeField] private Camera cam2;
    public TMP_Text speedometer;
    public TMP_Text speedometer2;
    private WheelControl[] wheels;
    private Rigidbody rigidBody;
    
    InputAction throttleAction;
    InputAction brakeAction;
    InputAction shiftUpAction;
    InputAction shiftDownAction;
    InputAction driveAction;
    InputAction steerAction;
    InputAction camSwitchAction;

    void Start()
    {
        cam2.enabled = false;
        rigidBody = GetComponent<Rigidbody>();
        
        Vector3 centerOfMass = rigidBody.centerOfMass;
        centerOfMass.y += centreOfGravityOffset;
        rigidBody.centerOfMass = centerOfMass;
        
        wheels = GetComponentsInChildren<WheelControl>();
        throttleAction = InputSystem.actions.FindAction("Throttle");
        brakeAction = InputSystem.actions.FindAction("Brake");
        shiftUpAction = InputSystem.actions.FindAction("ShiftUp");
        shiftDownAction = InputSystem.actions.FindAction("ShiftDown");
        driveAction = InputSystem.actions.FindAction("Drive");
        steerAction = InputSystem.actions.FindAction("Steer");
        camSwitchAction = InputSystem.actions.FindAction("CamSwitch");
    }

    void Update()
    {
        if (Player2)
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                SwitchCameras();
            }
        }
        else
        {
            if (camSwitchAction.triggered)
            {
                SwitchCameras();
            }
        }
    }

    void SwitchCameras()
    {
        cam1.enabled = !cam1.enabled;
        cam2.enabled = !cam2.enabled;
    }
    void FixedUpdate()
    {
        var kph = rigidBody.linearVelocity.magnitude * 3.6;
        speedometer.text = kph.ToString("0");
        speedometer2.text = kph.ToString("0");

        float vInput;
        float hInput;
        if (Player2)
        {
            vInput = Input.GetAxis("Vertical2"); // Forward/backward input
            hInput = Input.GetAxis("Horizontal2"); // Steering input
        }
        else
        {
            vInput = driveAction.ReadValue<float>(); // Forward/backward input
            hInput = steerAction.ReadValue<float>(); // Steering input
        }
        
        float forwardSpeed = Vector3.Dot(transform.forward, rigidBody.linearVelocity);
        float speedFactor = Mathf.InverseLerp(0, maxSpeed, Mathf.Abs(forwardSpeed)); 
        
        float currentMotorTorque = Mathf.Lerp(motorTorque, 0, speedFactor);
        float currentSteerRange = Mathf.Lerp(steeringRange, steeringRangeAtMaxSpeed, speedFactor);
        
        bool isAccelerating = Mathf.Sign(vInput) == Mathf.Sign(forwardSpeed);

        foreach (var wheel in wheels)
        {
            if (wheel.steerable)
            {
                wheel.WheelCollider.steerAngle = hInput * currentSteerRange;
            }

            if (isAccelerating)
            {
                if (wheel.motorized)
                {
                    wheel.WheelCollider.motorTorque = vInput * currentMotorTorque;
                }
                wheel.WheelCollider.brakeTorque = 0f;
            }
            else
            {
                wheel.WheelCollider.motorTorque = 0f;
                wheel.WheelCollider.brakeTorque = Mathf.Abs(vInput) * brakeTorque;
            }
        }
    }
}

