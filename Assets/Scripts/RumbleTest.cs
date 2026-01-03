using UnityEngine;

public class RumbleTest : MonoBehaviour
{
    private void Update()
    {
        if (InputManager.instance.controls.Rumble.RumbleAction.WasPressedThisFrame())
        {
            RumbleManager.instance.RumblePulse(.25f,1f, 0.25f);
        }
    }
}
