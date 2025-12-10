using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public int checkpointIndex;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            RaceManager.Instance.CheckPointReached(checkpointIndex);
        }
    }
}
