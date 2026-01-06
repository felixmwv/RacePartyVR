using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PlayerSpawnManager : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;

    private int nextSpawnIndex = 0;
    private int nextChannelIndex = 1;

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        Transform spawnPoint = spawnPoints[nextSpawnIndex];
        nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Length;

        Rigidbody rb = playerInput.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = spawnPoint.position;
        rb.rotation = spawnPoint.rotation;
        rb.Sleep();
        
        CinemachineCamera cineCam = playerInput.GetComponentInChildren<CinemachineCamera>();
        if (cineCam == null)
        {
            return;
        }
        
        OutputChannels channelEnum = GetOutputChannel(nextChannelIndex);
        cineCam.OutputChannel = channelEnum;
        
        CinemachineBrain brain = playerInput.GetComponentInChildren<CinemachineBrain>();
        if (brain != null)
        {
            brain.ChannelMask = channelEnum;
        }

        nextChannelIndex++;
    }

    private OutputChannels GetOutputChannel(int index)
    {
        switch (index)
        {
            case 1:  return OutputChannels.Default;
            case 2:  return OutputChannels.Channel01;
            case 3:  return OutputChannels.Channel02;
            case 4:  return OutputChannels.Channel03;
            case 5:  return OutputChannels.Channel04;
            case 6:  return OutputChannels.Channel05;
        }
        return OutputChannels.Default;
    }
}

