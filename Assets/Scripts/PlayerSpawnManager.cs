using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawnManager : MonoBehaviour
{
    [SerializeField]
    private Transform[] spawnPoints;

    private int nextSpawnIndex;

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        Transform spawnPoint = spawnPoints[nextSpawnIndex];

        playerInput.transform.position = spawnPoint.position;
        playerInput.transform.rotation = spawnPoint.rotation;

        nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Length;
    }
}
