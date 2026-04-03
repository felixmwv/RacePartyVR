using UnityEngine;

[CreateAssetMenu( fileName = "NewEngineSounds", menuName = "Car/Engine Sound SO" )]
public class CarEngineSoundSO : ScriptableObject
{
    [Tooltip( "RPM difference between each audio clip" )]
    public float rpmStep = 500f;

    [Tooltip( "Array of looping engine sounds from low to high" )]
    public AudioClip[] engineRPMRangeArray;
}