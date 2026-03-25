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

        PlayerRaceController playerRaceController = other.GetComponent<PlayerRaceController>();
        if (playerRaceController == null)
        {
            return;
        }

        playerRaceController.CheckPointReached(checkpointIndex);
    }
}
