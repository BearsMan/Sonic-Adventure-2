using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Public Variables
    public Transform liveCam;
    public Camera mainCamera;
    public Rigidbody body;
    public Vector2 pInput;
    public Vector3 mov;
    public float speed = 5.0f;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    private void FixedUpdate()
    {
        CharacterMovement(PlayerInput());
    }
    Vector2 PlayerInput()
    {
        // Inputs for Movement
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        pInput = new Vector2(h, v);


        return pInput;
    }
    // Rotates Lead Character
    public void RotateLeader(Vector3 dir)
    {
        if (dir != Vector3.zero)
        {
            dir = new Vector3(dir.x, 0, dir.z);
            transform.rotation = Quaternion.LookRotation(dir);

        }
    }
    public void CharacterMovement(Vector2 pInput)
    {
        // Camera Movements and Rotations
        float rotx = Input.GetAxis("Mouse X");
        float roty = Input.GetAxis("Mouse Y");
        transform.Rotate(transform.up * rotx);
        transform.Rotate(transform.right * roty);

        // Basic Player Movements
        mov = liveCam.forward * pInput.y + liveCam.right * pInput.x;
        mov = liveCam.up * pInput.y + liveCam.up * pInput.y;
        body.AddForce(mov * speed);
    }
}
