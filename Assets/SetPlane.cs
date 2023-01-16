using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetPlane : MonoBehaviour
{
    bool updated = false;
    float smooth = 5.0f;
    float tiltAngle = 90.0f;

    void LateUpdate()
    {
        this.gameObject.transform.position = new Vector3(this.gameObject.transform.position.x, -23, 52);
        Quaternion target = Quaternion.Euler(90.0f, 0, 0);
        this.gameObject.transform.rotation = Quaternion.Slerp(transform.rotation, target, 1);

        CubeGenerator cubeGenerator = this.gameObject.GetComponent<CubeGenerator>();
        cubeGenerator.size = 1000;
        cubeGenerator.resolution = 10;
    }
}
