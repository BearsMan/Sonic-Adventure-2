using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform cam;
    public Vector3 xCamVelocity, yCamVelocity;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        xCamVelocity = cam.position;
        yCamVelocity = cam.position;

    }
}
