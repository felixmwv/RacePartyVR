using System;
using TMPro;
using UnityEngine;

public class PlayerRaceController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI currentLapTimeText;
    [SerializeField] private TextMeshProUGUI bestLapTimeText;
    [SerializeField] private TextMeshProUGUI lapText;

    [Header("Race Settings")]
    [SerializeField] private bool isCircuit = true;

    private Checkpoint[] checkpoints;
    private int totalLaps;
    private int currentLap = 1;

    private float currentLapTime;
    private float bestLapTime = Mathf.Infinity;

    private bool raceStarted;
    private bool raceFinished;

    public int CurrentLap => currentLap;
    public int LastCheckpointIndex { get; private set; } = -1;
    public float DistanceToNextCheckpoint { get; private set; }
    public bool HasFinished { get; private set; }

    private Transform nextCheckpoint;


    private void Awake()
    {
        FindCheckpoints();
    }

    private void Start()
    {
        RaceManager.Instance.RegisterPlayer(this);

        var gm = GameModeManager.Instance;
        totalLaps = gm.infiniteLaps ? int.MaxValue : gm.totalLaps;
    }

    private void OnDestroy()
    {
        if (RaceManager.Instance != null)
            RaceManager.Instance.UnregisterPlayer(this);
    }

    private void Update()
    {
        if (raceStarted && !raceFinished)
        {
            currentLapTime += Time.deltaTime;
        }
        if (nextCheckpoint != null)
        {
            DistanceToNextCheckpoint =
                Vector3.Distance(transform.position, nextCheckpoint.position);
        }

        UpdateUI();
    }

    private void FindCheckpoints()
    {
        var parent = GameObject.Find("Checkpoints");
        checkpoints = parent.GetComponentsInChildren<Checkpoint>();
        Array.Sort(checkpoints, (a, b) => a.checkpointIndex.CompareTo(b.checkpointIndex));
        if (checkpoints.Length > 0)
            nextCheckpoint = checkpoints[0].transform;
    }

    public void CheckPointReached(int checkpointIndex)
    {
        if (!raceStarted && checkpointIndex != 0) return;
        if (raceFinished) return;

        if (checkpointIndex == LastCheckpointIndex + 1)
        {
            UpdateCheckpoint(checkpointIndex);
        }
    }

    private void UpdateCheckpoint(int checkpointIndex)
    {
        if (checkpointIndex == 0)
        {
            if (!raceStarted)
            {
                raceStarted = true;
            }
            else if (LastCheckpointIndex == checkpoints.Length - 1)
            {
                FinishLap();
            }
        }

        LastCheckpointIndex = checkpointIndex;

        int nextIndex = checkpointIndex + 1;
        if (nextIndex >= checkpoints.Length)
            nextIndex = 0;

        nextCheckpoint = checkpoints[nextIndex].transform;
    }

    private void FinishLap()
    {
        if (currentLapTime < bestLapTime)
            bestLapTime = currentLapTime;

        currentLap++;
        currentLapTime = 0f;
        LastCheckpointIndex = isCircuit ? 0 : -1;

        if (currentLap > totalLaps)
        {
            raceFinished = true;
            HasFinished = true;
            RaceManager.Instance.NotifyPlayerFinished(this);
        }

    }

    private void UpdateUI()
    {
        currentLapTimeText.text = FormatTime(currentLapTime);
        bestLapTimeText.text = FormatTime(bestLapTime);

        lapText.text = totalLaps == int.MaxValue
            ? $"Lap {currentLap}/∞"
            : $"Lap {currentLap}/{totalLaps}";
    }

    private string FormatTime(float time)
    {
        if (float.IsInfinity(time)) return "--:--";
        int minutes = (int)time / 60;
        float seconds = time % 60;
        return $"{minutes:00}:{seconds:00}";
    }
}

