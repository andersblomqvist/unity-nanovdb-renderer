using UnityEngine;

public class ToggleFullscreen : MonoBehaviour
{
    void Update()
    {
        // leave if in editor
        if (Application.isEditor)
        {
            return;
        }

        // toggle fullscreen on F11
        if (Input.GetKeyDown(KeyCode.F11))
        {
            if (Screen.fullScreenMode == FullScreenMode.Windowed)
            {
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
            }
            else
            {
                Screen.fullScreenMode = FullScreenMode.Windowed;
            }
        }
    }
}
