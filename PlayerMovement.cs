using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 1f;
    public float runSpeed = 5f;
    public float jumpForce = 7f;
    public float gravity = 10f;
    public float jumpSpeed = 3f;
    public float jumpHeight = 4f;
    public float verticalVelocityMovement;
    public float horizontalVelocityMovement;    
    public CharacterController controller;
    private bool isGrounded;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float horizontalVelocity = controller.velocity.x;
        float verticalVelocity = controller.velocity.y;
        Vector3 moveDir = new Vector3(verticalVelocityMovement, 0, horizontalVelocityMovement).normalized;
    }
}
