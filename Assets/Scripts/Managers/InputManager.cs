using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager instance;

    [HideInInspector] public PlayerSplitScreen controls;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        
        controls = new PlayerSplitScreen();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }
}
