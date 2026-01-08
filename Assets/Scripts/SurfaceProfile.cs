using UnityEngine;

[System.Serializable]
public class SurfaceProfile
{
    public string name;
    public PhysicsMaterial[] materials;

    [Header("Rumble")]
    [Range(0f, 1f)] public float lowFrequency;
    [Range(0f, 1f)] public float highFrequency;
    public float intensityMultiplier = 1f;

    [Header("Behaviour")]
    public bool speedScaled = true;
    public float sidewaysGrip = 1f;
}
