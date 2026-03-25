using TMPro;
using UnityEngine;

public class PlayerPositionIndicator : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI positionText;
    private PlayerRaceController raceController;

    private void Awake()
    {
        raceController = GetComponent<PlayerRaceController>();
    }

    private void Update()
    {
        int position = RaceManager.Instance.GetPlayerPosition(raceController);
        int total = RaceManager.Instance.PlayerCount;

        positionText.text = $"{FormatPosition(position)} / {total}";
    }

    private string FormatPosition(int pos)
    {
        return pos switch
        {
            1 => "1st",
            2 => "2nd",
            3 => "3rd",
            _ => $"{pos}th"
        };
    }
}

