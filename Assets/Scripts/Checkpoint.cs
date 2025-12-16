using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [HideInInspector] public int checkpointIndex;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        RaceManager raceManager = other.GetComponent<RaceManager>();
        if (raceManager == null)
        {
            return;
        }

        raceManager.CheckPointReached(checkpointIndex);
    }
}
