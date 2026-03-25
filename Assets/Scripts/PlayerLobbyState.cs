using UnityEngine;

public class PlayerLobbyState : MonoBehaviour
{
    public bool IsReady { get; private set; }
    public int PlayerIndex { get; private set; }
    
    [SerializeField] private GameObject readyCheckmark;
    
    public void Init(int index)
    {
        PlayerIndex = index;
        SetReady(false);
    }

    public void ToggleReady()
    {
        SetReady(!IsReady);
    }
    
    public void SetReady(bool value)
    {
        IsReady = value;

        if (readyCheckmark != null)
            readyCheckmark.SetActive(IsReady);
    }
}