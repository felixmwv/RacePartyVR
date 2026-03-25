using UnityEngine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    private List<PlayerLobbyState> players = new();
    private HashSet<InputDevice> joinedDevices = new();
    private int desiredMaxPlayers = 1;

    public List<PlayerLobbyState> Players => players;

    private void Awake()
    {
        Instance = this;
    }

    public void SetDesiredPlayerCount(int count)
    {
        desiredMaxPlayers = Mathf.Clamp(count, 1, 4);
        
        while (players.Count > desiredMaxPlayers)
        {
            var last = players[players.Count - 1];
            joinedDevices.Remove(last.GetComponent<PlayerInput>().devices[0]);
            Destroy(last.gameObject);
            players.RemoveAt(players.Count - 1);
        }

        RecalculatePlayerIndices();
    }

    public bool CanPlayerJoin()
    {
        return players.Count < desiredMaxPlayers;
    }

    public bool HasDeviceJoined(InputDevice device)
    {
        return joinedDevices.Contains(device);
    }

    public void RegisterPlayer(PlayerInput input)
    {
        var device = input.devices[0];
        if (!CanPlayerJoin() || joinedDevices.Contains(device))
        {
            Destroy(input.gameObject);
            return;
        }

        var lobbyState = input.GetComponent<PlayerLobbyState>();
        players.Add(lobbyState);
        joinedDevices.Add(device);

        RecalculatePlayerIndices();
    }

    public void RemovePlayer(PlayerLobbyState player)
    {
        players.Remove(player);
        
        var playerInput = player.GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            foreach (var device in playerInput.devices)
            {
                joinedDevices.Remove(device);
            }
        }

        Destroy(player.gameObject);
        RecalculatePlayerIndices();
    }

    private void RecalculatePlayerIndices()
    {
        for (int i = 0; i < players.Count; i++)
        {
            players[i].Init(i);
        }
    }
    
    public void ResetReadyStates()
    {
        foreach (var player in players)
        {
            player.SetReady(false);
        }
    }

    public bool AllPlayersReady()
    {
        if (players.Count == 0) return false;

        foreach (var p in players)
            if (!p.IsReady) return false;
        return true;
    }

    public bool IsHost(PlayerLobbyState player)
    {
        return player.PlayerIndex == 0;
    }
}
