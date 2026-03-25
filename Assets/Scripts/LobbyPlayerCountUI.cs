using UnityEngine;

public class LobbyPlayerCountUI : MonoBehaviour
{
    [SerializeField] private LobbyManager lobbyManager;
    public void SetMaxPlayers(int count)
    {
        lobbyManager.SetDesiredPlayerCount(count);
    }
}


