using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPosition : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if(this.transform.position != this.gameObject.GetComponent<CubeGenerator>().origin)
        {
            this.gameObject.GetComponent<CubeGenerator>().origin = this.transform.position;
        }
    }
}
