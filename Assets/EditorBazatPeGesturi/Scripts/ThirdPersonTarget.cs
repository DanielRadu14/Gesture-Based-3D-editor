using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonTarget : MonoBehaviour
{
    public Transform camera;
    public Vector3 moveDir;
    public float moveFactor = 3.0f;
    private ModelGestureListener gestureListener;

    void Start()
    {
        gestureListener = ModelGestureListener.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        if (!gestureListener)
            return;

        GetMoveDirection();
    }

    private void GetMoveDirection()
    {
        if (gestureListener.IsTurningWheel())
        {
            float turnAngle = Mathf.Clamp(gestureListener.GetWheelAngle(), -30f, 30f);
            float updateAngle = Mathf.Lerp(0, turnAngle, moveFactor * Time.deltaTime);

            moveDir = (updateAngle * camera.forward).normalized;
            this.transform.position += moveDir * moveFactor;
        }

        /*float z = 0;
        if (Input.GetKey(KeyCode.W))
        {
            z += Time.deltaTime * moveFactor;
        }

        if (Input.GetKey(KeyCode.S))
        {
            z -= Time.deltaTime * moveFactor;
        }

        moveDir = (z * camera.forward).normalized;
        this.transform.position += moveDir * moveFactor;*/
    }
}
