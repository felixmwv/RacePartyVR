using UnityEngine;

public class CheckpointSingle : MonoBehaviour
{
    private TrackCheckPoints trackCheckPoints;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            trackCheckPoints.PlayerThroughCheckPoint(this);
        }
    }

    public void SetTrackCheckPoints(TrackCheckPoints trackCheckPoints)
    {
        this.trackCheckPoints = trackCheckPoints;
    }
}
