using UnityEngine;

public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }

    public GameMode CurrentMode { get; private set; }

    public int totalLaps;      // race
    public bool infiniteLaps = false;

    public float hotlapTimeLimit = -1f; // -1 = infinite

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetRaceMode(int laps, bool infinite)
    {
        CurrentMode = GameMode.Race;
        totalLaps = laps;
        infiniteLaps = infinite;
    }

    public void SetHotlapMode(float timeLimit)
    {
        CurrentMode = GameMode.Hotlap;
        hotlapTimeLimit = timeLimit;
    }
}

