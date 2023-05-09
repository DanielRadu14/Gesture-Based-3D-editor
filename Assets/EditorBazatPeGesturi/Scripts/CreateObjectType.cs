using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CreateObjectType : MonoBehaviour
{
    [Tooltip("UI-Text used to display the picked game mode.")]
    public UnityEngine.UI.Text gameModeDebugText;

    public Transform camera;
    public float hidingOffset = 20.0f;
    public float slideSpeed = 5.0f;
    public float distanceFromCamera = 25;
    private bool availability = true;
    private ModelGestureListener gestureListener;
    private InteractionManager interactionManager;

    protected static CreateObjectType instance = null;
    public static CreateObjectType Instance
    {
        get
        {
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this);
            return;
        }
    }

    private void Start()
    {
        gestureListener = ModelGestureListener.Instance;
    }

    private void Update()
    {
        if (interactionManager == null)
        {
            interactionManager = GrabDropScript.Instance.interactionManager;
        }

        this.transform.rotation = Quaternion.Euler(gestureListener.GetPitch(), gestureListener.GetYaw(), 0f);
 
        Vector3 topRightWorldPoint = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, Camera.main.pixelHeight, distanceFromCamera));
        Vector3 topRightScreenPoint = Camera.main.WorldToScreenPoint(topRightWorldPoint);

        if (!ShowMenu(topRightScreenPoint))
        {
            Vector3 moveFactor = camera.right.normalized * hidingOffset;
            Vector3 hidingPosition = new Vector3(topRightWorldPoint.x + moveFactor.x, topRightWorldPoint.y + moveFactor.y, topRightWorldPoint.z + moveFactor.z);
            this.transform.position = Vector3.Lerp(this.transform.position,
                                        hidingPosition,
                                        Mathf.Clamp01(Time.deltaTime * slideSpeed));
        }
        else
        {
            if (availability)
            {
                this.transform.position = Vector3.Lerp(this.transform.position,
                                        topRightWorldPoint,
                                        Mathf.Clamp01(Time.deltaTime * slideSpeed));
            }
        }
    }

    private bool ShowMenu(Vector3 pivotScreenPoint)
    {
        Vector3 leftBottomBorder = new Vector3(pivotScreenPoint.x - 56, pivotScreenPoint.y - 512, 0);

        Vector3 cursorNormalPos = interactionManager.IsLeftHandPrimary() ? interactionManager.GetLeftHandScreenPos() : interactionManager.GetRightHandScreenPos();

        Vector3 cursorPixelPos = Vector3.zero;
        cursorPixelPos.x = (int)(cursorNormalPos.x * (Camera.main ? Camera.main.pixelWidth : Screen.width));
        cursorPixelPos.y = (int)(cursorNormalPos.y * (Camera.main ? Camera.main.pixelHeight : Screen.height));

        return cursorPixelPos.x > leftBottomBorder.x && cursorPixelPos.y > leftBottomBorder.y;
    }

    public void SetMenuAvailability(bool availability)
    {
        this.availability = availability;
    }

    public void CreateObject(GameObject objectToCreate, Vector3 newObjectPos)
    {
        foreach (GameObject gameObj in SelectableObjects.Instance.creatableObjectTypes)
        {
            if (gameObj.name.Equals(objectToCreate.name))
            {
                GameObject gameObject = Instantiate(gameObj, newObjectPos, Quaternion.identity);
                GrabDropScript.Instance.draggableObjects.Add(gameObject);
            }
        }
    }
}