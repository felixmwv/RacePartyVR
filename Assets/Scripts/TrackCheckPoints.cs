using System.Collections.Generic;
using UnityEngine;

public class TrackCheckPoints : MonoBehaviour
{
    private List<CheckpointSingle> checkpointSingleList;
    private int nextCheckpointSingleIndex;
    private void Awake()
    {
        Transform checkpointsTransform = transform.Find("CheckPoints");
        
        checkpointSingleList = new List<CheckpointSingle>();
        
        foreach (Transform checkpointSingleTransform in checkpointsTransform)
        {
            CheckpointSingle checkpointSingle = checkpointSingleTransform.GetComponent<CheckpointSingle>();
            checkpointSingle.SetTrackCheckPoints(this);
            checkpointSingleList.Add(checkpointSingle);
        }
        nextCheckpointSingleIndex = 0;
    }

    public void PlayerThroughCheckPoint(CheckpointSingle checkpointSingle)
    {
        if (checkpointSingleList.IndexOf(checkpointSingle) == nextCheckpointSingleIndex)
        {
            nextCheckpointSingleIndex = (nextCheckpointSingleIndex + 1) % checkpointSingleList.Count;
            Debug.Log("Correct");
        }
        else
        {
            Debug.Log("Wrong");
        }
    }
}
