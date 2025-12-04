using System;
using TMPro;
using UnityEngine;

public class CarControl : MonoBehaviour
{
    [Header("Car Properties")]
    public float motorTorque = 2000f;
    public float brakeTorque = 2000f;
    public float maxSpeed = 20f;
    public float steeringRange = 30f;
    public float steeringRangeAtMaxSpeed = 10f;
    public float centreOfGravityOffset = -1f;
    [SerializeField] private bool Player2;
    public TMP_Text speedometer;
    private WheelControl[] wheels;
    private Rigidbody rigidBody;

    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        
        Vector3 centerOfMass = rigidBody.centerOfMass;
        centerOfMass.y += centreOfGravityOffset;
        rigidBody.centerOfMass = centerOfMass;
        
        wheels = GetComponentsInChildren<WheelControl>();
    }
    
    
    void FixedUpdate()
    {
        var kph = rigidBody.linearVelocity.magnitude * 3.6;
        speedometer.text = kph.ToString("0");

        float vInput;
        float hInput;
        if (Player2)
        {
            vInput = Input.GetAxis("Vertical"); // Forward/backward input
            hInput = Input.GetAxis("Horizontal"); // Steering input
        }
        else
        {
            vInput = Input.GetAxis("Vertical2"); // Forward/backward input
            hInput = Input.GetAxis("Horizontal2"); // Steering input
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

