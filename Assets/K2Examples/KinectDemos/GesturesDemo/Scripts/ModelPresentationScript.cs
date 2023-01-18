using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ModelPresentationScript : UnityEngine.MonoBehaviour 
{
	[Tooltip("Camera used for screen-to-world calculations. This is usually the main camera.")]
	public Camera screenCamera;

	[Tooltip("Speed of rotation, when the presentation model spins.")]
	public float spinSpeed = 10;

	// reference to the gesture listener
	private ModelGestureListener gestureListener;

	// model's initial rotation
	private Quaternion initialRotation;
    private Vector3 initialScale;
    private bool canZoom = true;


	void Start() 
	{
		// hide mouse cursor
		//Cursor.visible = false;
		
		// by default set the main-camera to be screen-camera
		if (screenCamera == null) 
		{
			screenCamera = Camera.main;
		}

		// get model initial rotation
		initialRotation = screenCamera ? Quaternion.Inverse(screenCamera.transform.rotation) * transform.rotation : transform.rotation;
        initialScale = this.gameObject.transform.localScale;

		// get the gestures listener
		gestureListener = ModelGestureListener.Instance;
	}
	
	void Update() 
	{
		if(!gestureListener)
			return;

		if(gestureListener.IsZoomingIn() || gestureListener.IsZoomingOut())
		{
            if (GameModePicker.Instance.gameModeStat == GameModePicker.GameMode.Default)
            {
                // zoom the model
                float zoomFactor = gestureListener.GetZoomFactor();
                Vector3 newLocalScale = new Vector3(zoomFactor, zoomFactor, zoomFactor);
                transform.localScale = Vector3.Lerp(transform.localScale, newLocalScale, spinSpeed * Time.deltaTime);
            }
            else
            {
                CubeGenerator cubeGenerator = this.gameObject.GetComponent<CubeGenerator>();
                if(cubeGenerator != null && canZoom)
                {
                    int resolutionFactor;
                    if (gestureListener.IsZoomingOut())
                        resolutionFactor = 1;
                    else resolutionFactor = -1;

                    cubeGenerator.resolution += resolutionFactor;
                    canZoom = false;
                    StartCoroutine(ResetCanZoom());
                }
            }
		}

		if(gestureListener.IsRaiseHand())
		{
			// reset the model
			transform.localScale = initialScale;
			transform.rotation = screenCamera ? screenCamera.transform.rotation * initialRotation : initialRotation;
		}

	}

    private IEnumerator ResetCanZoom()
    {
        yield return new WaitForSeconds(2.0f);
        canZoom = true;
    }

}
