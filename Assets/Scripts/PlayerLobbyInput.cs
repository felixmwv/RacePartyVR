using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLobbyInput : MonoBehaviour
{
    private PlayerLobbyState lobbyState;

    private void Awake()
    {
        lobbyState = GetComponent<PlayerLobbyState>();
    }

    public void OnReady(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        lobbyState.ToggleReady();
    }
}


