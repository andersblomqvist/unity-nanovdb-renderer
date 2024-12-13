using UnityEngine;

// Whole file written by ChatGPT
public class ArcballCamera : MonoBehaviour
{
    public Transform target; // The target object or position (world origin by default)
    public float distance = 10.0f; // Initial distance from the target
    public float scrollSpeed = 2.0f; // Speed of zooming in/out
    public float rotationSpeed = 100.0f; // Speed of rotation
    public float panSpeed = 0.5f; // Speed of panning

    private float currentYaw = -90.0f; // Current yaw (horizontal rotation)
    private float currentPitch = 0.0f; // Current pitch (vertical rotation)

    void Start()
    {
        // If no target is set, default to the world origin
        if (target == null)
        {
            GameObject origin = new GameObject("WorldOrigin");
            target = origin.transform;
            target.position = new Vector3(-10, 92, -54);
        }
    }

    void Update()
    {
        HandleRotation();
        HandleZoom();
        HandlePan();
        UpdateCameraPosition();
    }

    void HandleRotation()
    {
        // make sure were not interacting with UI
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButton(0)) // Left mouse button
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            currentYaw += mouseX * rotationSpeed * Time.deltaTime;
            currentPitch -= mouseY * rotationSpeed * Time.deltaTime;

            // Clamp pitch to avoid flipping (e.g., -89 to 89 degrees)
            currentPitch = Mathf.Clamp(currentPitch, -89f, 89f);
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * scrollSpeed;
    }

    void HandlePan()
    {
        if (Input.GetMouseButton(1)) // Right mouse button
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            Vector3 right = transform.right; // Camera's right direction
            Vector3 up = transform.up; // Camera's up direction

            // Move the target position based on mouse movement in screen space
            target.position += -right * mouseX * panSpeed + -up * mouseY * panSpeed;
        }
    }

    void UpdateCameraPosition()
    {
        // Convert spherical coordinates (yaw, pitch, distance) to Cartesian coordinates
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        Vector3 offset = rotation * new Vector3(0, 0, -distance);

        // Update camera position and look at the target
        transform.position = target.position + offset;
        transform.LookAt(target);
    }

    public void ResetCameraPosition()
    {
        target.position = new Vector3(-10, 92, -54);
        currentYaw = -90.0f;
        currentPitch = 0.0f;
        distance = 1000.0f;
    }
}
