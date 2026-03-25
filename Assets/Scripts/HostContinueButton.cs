using UnityEngine;

public class HostContinueButton : MonoBehaviour
{
    public GameObject menu;
    public GameObject menuCamera;
    
    public void Continue()
    {
        if (!LobbyManager.Instance.AllPlayersReady())
            return;
        
        LobbyManager.Instance.ResetReadyStates();

        menu.SetActive(false);
        menuCamera.SetActive(false);
        Debug.Log("START GAME");
    }
}

