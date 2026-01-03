using UnityEngine;

public class RumbleTest : MonoBehaviour
{
    private void Update()
    {
        if (InputManager.instance.controls.Rumble.RumbleAction.WasPressedThisFrame())
        {
            RumbleManager.instance.RumblePulse(0f,1f, 0.25f);
        }
    }
}
