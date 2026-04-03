using UnityEngine;

[CreateAssetMenu( fileName = "NewHandling", menuName = "Car/Handling SO" )]
public class CarHandlingSO : ScriptableObject
{
    [Header( "Car Properties" )]
    public float engineTorque = 450f;
    public float brakeTorque = 4000f;
    public float engineBrakeTorque = 300f;
    public float maxSpeed = 200f;

    [Header( "Steering" )]
    public float steeringRange = 30f;
    public float steeringRangeAtMaxSpeed = 20f;
    public float maxSteerSpeed = 1.7f;
    public float wheelBase = 2.7f;
    public float trackWidth = 1.86f;

    [Header( "Drivetrain" )]
    public float finalDrive = 3.4f;
    public float[] gearRatios = { 3.2f, 2.1f, 1.4f, 1.0f, 0.8f };
    public float idleRpm = 900f;
    public float redlineRpm = 5000f;
    public float maxRpm = 6000f;
    public AnimationCurve torqueCurve;

    [Header( "Drift" )]
    public float wheelspinGripLoss = 0.5f;
    public float overrevGripLoss = 0.9f;

    [Header( "Wheel Grip — Front" )]
    public float frontMinSidewaysGrip = 0.55f;
    public float frontMaxSidewaysGrip = 1.0f;
    public float frontNormalSidewaysGrip = 1.0f;
    public float frontBrakeSidewaysGrip = 1.0f;
    public float frontHandbrakeSidewaysGrip = 1.0f;

    [Header( "Wheel Grip — Rear" )]
    public float rearMinSidewaysGrip = 0.3f;
    public float rearMaxSidewaysGrip = 1.2f;
    public float rearNormalSidewaysGrip = 1.0f;
    public float rearBrakeSidewaysGrip = 0.8f;
    public float rearHandbrakeSidewaysGrip = 0.4f;

    [Header( "Rumble" )]
    public float rumbleMinSpeedKph = 2f;
}