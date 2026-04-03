using UnityEngine;

/// <summary>
/// Crossfades between looped engine audio clips based on current engine RPM.
/// Attach to EngineAudioHolder. Requires one AudioSource per clip in the Scriptable Object.
/// </summary>
public class EngineAudioController : MonoBehaviour
{
    [Header( "References" )]
    public CarEngineSoundSO engineSoundData;
    public CarHandlingSO handling;
    public CarControlAfter carControl;

    [Header( "Fade" )]
    public float fadeSpeed = 8f;

    private AudioSource[] audioSources;

    private void Awake()
    {
        this.audioSources = GetComponents<AudioSource>();
    }

    private void Start()
    {
        for ( int i = 0; i < this.audioSources.Length; i++ )
        {
            if ( i >= this.engineSoundData.engineRPMRangeArray.Length )
            {
                this.audioSources[ i ].volume = 0f;
                continue;
            }

            this.audioSources[ i ].clip = this.engineSoundData.engineRPMRangeArray[ i ];
            this.audioSources[ i ].loop = true;
            this.audioSources[ i ].volume = 0f;
            this.audioSources[ i ].Play();
        }
    }

    private void Update()
    {
        float currentRpm = this.carControl.EngineRpm;
        float idleRpm = this.handling.idleRpm;
        float rpmStep = this.engineSoundData.rpmStep;

        int clipCount = this.engineSoundData.engineRPMRangeArray.Length;

        for ( int i = 0; i < this.audioSources.Length; i++ )
        {
            if ( i >= clipCount )
            {
                this.audioSources[ i ].volume = 0f;
                continue;
            }
            
            float preferredRpm = ( i * rpmStep ) + idleRpm;
            float rpmDiff = preferredRpm - currentRpm;

            float targetVolume;
            float targetPitch;

            if ( rpmDiff < rpmStep * 2f && rpmDiff > -rpmStep * 2f )
            {
                targetVolume = rpmStep / ( Mathf.Abs( rpmDiff ) + rpmStep );
                targetPitch  = currentRpm / preferredRpm;
            }
            else
            {
                targetVolume = 0f;
                targetPitch  = this.audioSources[ i ].pitch;
            }

            this.audioSources[ i ].volume = Mathf.Lerp(this.audioSources[ i ].volume, targetVolume, Time.deltaTime * this.fadeSpeed);

            this.audioSources[ i ].pitch = Mathf.Lerp(this.audioSources[ i ].pitch, targetPitch, Time.deltaTime * this.fadeSpeed);
        }
    }
}
