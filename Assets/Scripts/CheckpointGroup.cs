using UnityEngine;

public class CheckpointGroup : MonoBehaviour
{
    private void Awake()
    {
        Checkpoint[] checkpoints = GetComponentsInChildren<Checkpoint>();

        for (int i = 0; i < checkpoints.Length; i++)
        {
            checkpoints[i].checkpointIndex = i;
        }
    }
}

