using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Main Sonic Adventure DX Camera Controller Script
    
    public Transform player; // Reference to the player transform
    private Vector3 offset; // Offset between camera and player
    public float smoothSpeed = 0.125f; // Smoothing speed for camera movement

    void Start()
    {
        offset = transform.position - player.position; // Calculate initial offset
    }

    void LateUpdate()
    {
        Vector3 desiredPosition = player.position + offset; // Desired camera position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed); // Smoothly interpolate to desired position
        transform.position = smoothedPosition; // Update camera position
        transform.LookAt(player); // Make the camera look at the player
    }
}
