using UnityEngine;

public class PressToShow : MonoBehaviour
{
    public GameObject canvas;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            canvas.SetActive(!canvas.activeSelf);
        }
    }
}
