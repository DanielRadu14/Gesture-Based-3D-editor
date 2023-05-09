using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateLight : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;
    public float rotateOY = -30f;
    public float rotateOX = 50f;
    private ModelGestureListener gestureListener;
    
    void Start()
    {
        gestureListener = ModelGestureListener.Instance;
    }
    
    void Update()
    {
        this.transform.rotation = Quaternion.Euler(gestureListener.GetPitch() + rotateOX, gestureListener.GetYaw() + rotateOY, 0f);

        Vector3 newCameraPosition = target.position + transform.TransformDirection(offset);
        transform.position = Vector3.Lerp(transform.position,
                                        newCameraPosition,
                                        Mathf.Clamp01(Time.deltaTime * ThirdPersonTarget.Instance.rotationSpeed));
    }
}
