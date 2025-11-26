using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    // Public Variables
    public Transform liveCam;
    public Camera mainCamera;
    public Rigidbody body;
    public Vector2 pInput;
    public Vector3 mov;
    public float speed = 5.0f;
    public bool isGrounded = false; // Is true when the player jumps from the ground
    public bool jumping = false;
    public float homingAttackSpeed = 20f; // Sonic/Shadow's Attack
    public float flySpeed = 20f; // Tails only
    public float punchSpeed = 15f; // Knuckles
    public float fishingRodPowerSpeed = 10f; // Big The Cat's Pole for catching Froggy
    public void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

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

    // Moved RotateLeader and CharacterMovement inside the class as instance methods
    public void RotateLeader(Vector3 dir)
    {
        if (dir != Vector3.zero)
        {
            dir = new Vector3(dir.x, 0, dir.z);
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}