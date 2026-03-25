using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PlayerSpawnManager : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        // registreer speler in lobby
        LobbyManager.Instance.RegisterPlayer(playerInput);

        var lobbyPlayers = LobbyManager.Instance.Players;
        int playerIndex = lobbyPlayers.IndexOf(
            playerInput.GetComponent<PlayerLobbyState>()
        );

        // clamp index voor spawnpoints
        if (playerIndex >= spawnPoints.Length)
            playerIndex = spawnPoints.Length - 1;

        Transform spawnPoint = spawnPoints[playerIndex];

        // reset physics
        Rigidbody rb = playerInput.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = spawnPoint.position;
        rb.rotation = spawnPoint.rotation;
        rb.Sleep();
        
        CinemachineCamera cineCam =
            playerInput.GetComponentInChildren<CinemachineCamera>();

        if (cineCam != null)
        {
            // Output channel (splitscreen routing)
            OutputChannels channelEnum = GetOutputChannel(playerIndex + 1);
            cineCam.OutputChannel = channelEnum;
            
            CinemachineBrain brain =
                playerInput.GetComponentInChildren<CinemachineBrain>();

            if (brain != null)
                brain.ChannelMask = channelEnum;
            
            CinemachineInputAxisController axisInput =
                cineCam.GetComponent<CinemachineInputAxisController>();

            if (axisInput != null)
            {
                axisInput.PlayerIndex = playerInput.playerIndex;
            }
        }
    }

    private OutputChannels GetOutputChannel(int index)
    {
        switch (index)
        {
            case 1: return OutputChannels.Default;
            case 2: return OutputChannels.Channel01;
            case 3: return OutputChannels.Channel02;
            case 4: return OutputChannels.Channel03;
            case 5: return OutputChannels.Channel04;
            case 6: return OutputChannels.Channel05;
        }
        return OutputChannels.Default;
    }
}




