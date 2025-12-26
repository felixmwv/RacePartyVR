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

        Rigidbody rb = playerInput.GetComponent<Rigidbody>();

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.position = spawnPoint.position;
        rb.rotation = spawnPoint.rotation;

        rb.Sleep();

        nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Length;
    }
}
