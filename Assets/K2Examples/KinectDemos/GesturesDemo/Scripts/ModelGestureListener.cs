using UnityEngine;
using System.Collections;
using System;
//using Windows.Kinect;

public class ModelGestureListener : UnityEngine.MonoBehaviour, KinectGestures.GestureListenerInterface
{
	[Tooltip("Index of the player, tracked by this component. 0 means the 1st player, 1 - the 2nd one, 2 - the 3rd one, etc.")]
	public int playerIndex = 0;

	[Tooltip("UI-Text to display gesture-listener messages and gesture information.")]
	public UnityEngine.UI.Text gestureInfo;

	// singleton instance of the class
	private static ModelGestureListener instance = null;
	
	// internal variables to track if progress message has been displayed
	private bool progressDisplayed;
	private float progressGestureTime;

	// whether the needed gesture has been detected or not
	private bool zoomOut;
	private bool zoomIn;
	private float zoomFactor = 1f;

	private bool wheel;
	private float wheelAngle = 0f;

    private float yaw, pitch;
    private bool leanLeft;
    private bool leanRight;
    private bool leanForward;
    private bool leanBack;

    private bool raiseHand = false;
    private bool swipeDown = false;
    private bool swipeUp = false;


	/// <summary>
	/// Gets the singleton ModelGestureListener instance.
	/// </summary>
	/// <value>The ModelGestureListener instance.</value>
	public static ModelGestureListener Instance
	{
		get
		{
			return instance;
		}
	}
	
	/// <summary>
	/// Determines whether the user is zooming out.
	/// </summary>
	/// <returns><c>true</c> if the user is zooming out; otherwise, <c>false</c>.</returns>
	public bool IsZoomingOut()
	{
		return zoomOut;
	}

	/// <summary>
	/// Determines whether the user is zooming in.
	/// </summary>
	/// <returns><c>true</c> if the user is zooming in; otherwise, <c>false</c>.</returns>
	public bool IsZoomingIn()
	{
		return zoomIn;
	}

	/// <summary>
	/// Gets the zoom factor.
	/// </summary>
	/// <returns>The zoom factor.</returns>
	public float GetZoomFactor()
	{
		return zoomFactor;
	}

	/// <summary>
	/// Determines whether the user is turning wheel.
	/// </summary>
	/// <returns><c>true</c> if the user is turning wheel; otherwise, <c>false</c>.</returns>
	public bool IsTurningWheel()
	{
		return wheel;
	}

	/// <summary>
	/// Gets the wheel angle.
	/// </summary>
	/// <returns>The wheel angle.</returns>
	public float GetWheelAngle()
	{
		return wheelAngle;
	}

	/// <summary>
	/// Determines whether the user has raised his left or right hand.
	/// </summary>
	/// <returns><c>true</c> if the user has raised his left or right hand; otherwise, <c>false</c>.</returns>
	public bool IsRaiseHand()
	{
		if(raiseHand)
		{
			raiseHand = false;
			return true;
		}
		
		return false;
	}

    public bool IsSwipingDown()
    {
        if(swipeDown)
        {
            swipeDown = false;
            return true;
        }
        return false;
    }

    public bool IsSwipingUp()
    {
        if (swipeUp)
        {
            swipeUp = false;
            return true;
        }
        return false;
    }

    public bool IsLeaningLeft()
    {
        return leanLeft;
    }

    public bool IsLeaningRight()
    {
        return leanRight;
    }

    public bool IsLeaningForward()
    {
        return leanForward;
    }

    public bool IsLeaningBack()
    {
        return leanBack;
    }

    public float GetPitch()
    {
        return pitch;
    }

    public float GetYaw()
    {
        return yaw;
    }

    public void SetPitch(float pitch)
    {
        this.pitch = pitch;
    }

    public void SetYaw(float yaw)
    {
        this.yaw = yaw;
    }

    /// <summary>
    /// Invoked when a new user is detected. Here you can start gesture tracking by invoking KinectManager.DetectGesture()-function.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="userIndex">User index</param>
    public void UserDetected(long userId, int userIndex)
	{
		// the gestures are allowed for the primary user only
		KinectManager manager = KinectManager.Instance;
		if(!manager || (userIndex != playerIndex))
			return;
		
		// detect these user specific gestures
		manager.DetectGesture(userId, KinectGestures.Gestures.ZoomOut);
		manager.DetectGesture(userId, KinectGestures.Gestures.ZoomIn);
		manager.DetectGesture(userId, KinectGestures.Gestures.Wheel);

		manager.DetectGesture(userId, KinectGestures.Gestures.RaiseLeftHand);
		manager.DetectGesture(userId, KinectGestures.Gestures.RaiseRightHand);

        manager.DetectGesture(userId, KinectGestures.Gestures.SwipeDown);
        manager.DetectGesture(userId, KinectGestures.Gestures.SwipeUp);

        manager.DetectGesture(userId, KinectGestures.Gestures.LeanLeft);
        manager.DetectGesture(userId, KinectGestures.Gestures.LeanRight);
        manager.DetectGesture(userId, KinectGestures.Gestures.LeanForward);
        manager.DetectGesture(userId, KinectGestures.Gestures.LeanBack);

        if (gestureInfo != null)
		{
			gestureInfo.text = "Zoom-in or wheel to rotate the model.\nRaise hand to reset it.";
		}
	}

	/// <summary>
	/// Invoked when a user gets lost. All tracked gestures for this user are cleared automatically.
	/// </summary>
	/// <param name="userId">User ID</param>
	/// <param name="userIndex">User index</param>
	public void UserLost(long userId, int userIndex)
	{
		// the gestures are allowed for the primary user only
		if(userIndex != playerIndex)
			return;
		
		if(gestureInfo != null)
		{
			gestureInfo.text = string.Empty;
		}
	}

    /// <summary>
    /// Invoked when a gesture is in progress.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="userIndex">User index</param>
    /// <param name="gesture">Gesture type</param>
    /// <param name="progress">Gesture progress [0..1]</param>
    /// <param name="joint">Joint type</param>
    /// <param name="screenPos">Normalized viewport position</param>
    public void GestureInProgress(long userId, int userIndex, KinectGestures.Gestures gesture,
                                  float progress, KinectInterop.JointType joint, Vector3 screenPos)
    {
        // the gestures are allowed for the primary user only
        if (userIndex != playerIndex)
            return;

        if (gesture == KinectGestures.Gestures.ZoomOut)
        {
            if (progress > 0.5f)
            {
                zoomOut = true;
                zoomFactor = screenPos.z;

                if (gestureInfo != null)
                {
                    string sGestureText = string.Format("{0} factor: {1:F0}%", gesture, screenPos.z * 100f);
                    gestureInfo.text = sGestureText;

                    progressDisplayed = true;
                    progressGestureTime = Time.realtimeSinceStartup;
                }
            }
            else
            {
                zoomOut = false;
            }
        }
        else if (gesture == KinectGestures.Gestures.ZoomIn)
        {
            if (progress > 0.5f)
            {
                zoomIn = true;
                zoomFactor = screenPos.z;

                if (gestureInfo != null)
                {
                    string sGestureText = string.Format("{0} factor: {1:F0}%", gesture, screenPos.z * 100f);
                    gestureInfo.text = sGestureText;

                    progressDisplayed = true;
                    progressGestureTime = Time.realtimeSinceStartup;
                }
            }
            else
            {
                zoomIn = false;
            }
        }
        else if (gesture == KinectGestures.Gestures.Wheel)
        {
            if (progress > 0.5f)
            {
                wheel = true;
                wheelAngle = screenPos.z;

                if (gestureInfo != null)
                {
                    string sGestureText = string.Format("Wheel angle: {0:F0} deg.", screenPos.z);
                    gestureInfo.text = sGestureText;

                    progressDisplayed = true;
                    progressGestureTime = Time.realtimeSinceStartup;
                }
            }
            else
            {
                wheel = false;
            }
        }
        else if (gesture == KinectGestures.Gestures.LeanLeft)
        {
            if (progress > 0.5f)
            {
                leanLeft = true;
                yaw += Time.deltaTime * ThirdPersonTarget.Instance.rotationSpeed;
            }
            else
            {
                leanLeft = false;
            }
        }
        else if (gesture == KinectGestures.Gestures.LeanRight)
        {
            if (progress > 0.5f)
            {
                leanRight = true;
                yaw -= Time.deltaTime * ThirdPersonTarget.Instance.rotationSpeed;
            }
            else
            {
                leanRight = false;
            }
        }
        else if (gesture == KinectGestures.Gestures.LeanForward)
        {
            if (progress > 0.5f)
            {
                leanForward = true;
                pitch += Time.deltaTime * ThirdPersonTarget.Instance.rotationSpeed;
            }
            else
            {
                leanForward = false;
            }
        }
        else if (gesture == KinectGestures.Gestures.LeanBack)
        {
            if (progress > 0.5f)
            {
                leanBack = true;
                pitch -= Time.deltaTime * ThirdPersonTarget.Instance.rotationSpeed;
            }
            else
            {
                leanBack = false;
            }
        }
    }

	/// <summary>
	/// Invoked if a gesture is completed.
	/// </summary>
	/// <returns>true</returns>
	/// <c>false</c>
	/// <param name="userId">User ID</param>
	/// <param name="userIndex">User index</param>
	/// <param name="gesture">Gesture type</param>
	/// <param name="joint">Joint type</param>
	/// <param name="screenPos">Normalized viewport position</param>
	public bool GestureCompleted (long userId, int userIndex, KinectGestures.Gestures gesture, 
	                              KinectInterop.JointType joint, Vector3 screenPos)
	{
		// the gestures are allowed for the primary user only
		if(userIndex != playerIndex)
			return false;

		if(gesture == KinectGestures.Gestures.RaiseLeftHand)
			raiseHand = true;
		//else if(gesture == KinectGestures.Gestures.RaiseRightHand)
			//raiseHand = true;
        else if (gesture == KinectGestures.Gestures.SwipeDown)
            swipeDown = true;
        else if (gesture == KinectGestures.Gestures.SwipeUp)
            swipeUp = true;

        return true;
	}

	/// <summary>
	/// Invoked if a gesture is cancelled.
	/// </summary>
	/// <returns>true</returns>
	/// <c>false</c>
	/// <param name="userId">User ID</param>
	/// <param name="userIndex">User index</param>
	/// <param name="gesture">Gesture type</param>
	/// <param name="joint">Joint type</param>
	public bool GestureCancelled (long userId, int userIndex, KinectGestures.Gestures gesture, 
	                              KinectInterop.JointType joint)
	{
		// the gestures are allowed for the primary user only
		if(userIndex != playerIndex)
			return false;
		
		if(gesture == KinectGestures.Gestures.ZoomOut)
		{
			zoomOut = false;
		}
		else if(gesture == KinectGestures.Gestures.ZoomIn)
		{
			zoomIn = false;
		}
		else if(gesture == KinectGestures.Gestures.Wheel)
		{
			wheel = false;
		}
        else if (gesture == KinectGestures.Gestures.LeanLeft)
        {
            leanLeft = false;
        }
        else if (gesture == KinectGestures.Gestures.LeanRight)
        {
            leanRight = false;
        }
        else if (gesture == KinectGestures.Gestures.LeanForward)
        {
            leanForward = false;
        }
        else if (gesture == KinectGestures.Gestures.LeanBack)
        {
            leanBack = false;
        }

        if (gestureInfo != null && progressDisplayed)
		{
			progressDisplayed = false;
			gestureInfo.text = "Zoom-in or wheel to rotate the model.\nRaise hand to reset it.";;
		}

		return true;
	}

	
	void Awake()
	{
		instance = this;
	}

	void Update()
	{
		if(progressDisplayed && ((Time.realtimeSinceStartup - progressGestureTime) > 2f))
		{
			progressDisplayed = false;
			gestureInfo.text = string.Empty;

			Debug.Log("Forced progress to end.");
		}
	}

}
