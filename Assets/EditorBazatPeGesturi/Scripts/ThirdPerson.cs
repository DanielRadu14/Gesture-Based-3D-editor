using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPerson : MonoBehaviour
{
    float yaw, pitch;
    public Transform target;
    public Vector3 cameraOffset;
    private ModelGestureListener gestureListener;

    void Start()
    {
        gestureListener = ModelGestureListener.Instance;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!gestureListener)
            return;

        if (Input.GetKey(KeyCode.Q))
        {
            pitch += Time.deltaTime * ThirdPersonTarget.Instance.rotationSpeed;
        }

        if (Input.GetKey(KeyCode.E))
        {
            pitch -= Time.deltaTime * ThirdPersonTarget.Instance.rotationSpeed;
        }
        

        if (Input.GetKey(KeyCode.R))
        {
            yaw -= Time.deltaTime * ThirdPersonTarget.Instance.rotationSpeed;
        }

        if (Input.GetKey(KeyCode.T))
        {
            yaw += Time.deltaTime * ThirdPersonTarget.Instance.rotationSpeed;
        }

        if (gestureListener.IsLeaningLeft() || gestureListener.IsLeaningRight())
        {
            yaw = gestureListener.GetYaw();
        }
        else if (gestureListener.IsLeaningForward() || gestureListener.IsLeaningBack())
        {
            pitch = gestureListener.GetPitch();
        }

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 newCameraPosition = target.position + transform.TransformDirection(cameraOffset);
        transform.position = Vector3.Lerp(transform.position,
                                        newCameraPosition,
                                        Mathf.Clamp01(Time.deltaTime * ThirdPersonTarget.Instance.rotationSpeed));
    }
}
