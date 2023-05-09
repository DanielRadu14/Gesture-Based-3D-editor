using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectTextureType : MonoBehaviour
{
    public Transform camera;
    public float hidingOffset = -20.0f;
    public float slideSpeed = 5.0f;
    public float distanceFromCamera = 25;
    private bool availability = true;
    private ModelGestureListener gestureListener;
    private InteractionManager interactionManager;

    private Material draggedObjectMaterial = null;

    protected static SelectTextureType instance = null;
    public static SelectTextureType Instance
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

        Vector3 topLeftWorldPoint = Camera.main.ScreenToWorldPoint(new Vector3(0, Camera.main.pixelHeight, distanceFromCamera));
        Vector3 topLeftScreenPoint = Camera.main.WorldToScreenPoint(topLeftWorldPoint);

        if (!ShowMenu(topLeftScreenPoint))
        {
            Vector3 moveFactor = camera.right.normalized * hidingOffset;
            Vector3 hidingPosition = new Vector3(topLeftWorldPoint.x + moveFactor.x, topLeftWorldPoint.y + moveFactor.y, topLeftWorldPoint.z + moveFactor.z);
            this.transform.position = Vector3.Lerp(this.transform.position,
                                        hidingPosition,
                                        Mathf.Clamp01(Time.deltaTime * slideSpeed));
        }
        else
        {
            if (availability)
            {
                this.transform.position = Vector3.Lerp(this.transform.position,
                                        topLeftWorldPoint,
                                        Mathf.Clamp01(Time.deltaTime * slideSpeed));
            }
        }
    }

    private bool ShowMenu(Vector3 pivotScreenPoint)
    {
        Vector3 rightBottomBorder = new Vector3(pivotScreenPoint.x + 128, pivotScreenPoint.y - Camera.main.pixelHeight, 0);

        Vector3 cursorNormalPos = interactionManager.IsLeftHandPrimary() ? interactionManager.GetLeftHandScreenPos() : interactionManager.GetRightHandScreenPos();

        Vector3 cursorPixelPos = Vector3.zero;
        cursorPixelPos.x = (int)(cursorNormalPos.x * (Camera.main ? Camera.main.pixelWidth : Screen.width));
        cursorPixelPos.y = (int)(cursorNormalPos.y * (Camera.main ? Camera.main.pixelHeight : Screen.height));

        return cursorPixelPos.x < rightBottomBorder.x;
    }

    public void SetMenuAvailability(bool availability)
    {
        this.availability = availability;
    }

    public void SetMaterial(Material draggedObjectMaterial)
    {
        this.draggedObjectMaterial = draggedObjectMaterial;
    }

    public Material GetMaterial()
    {
        return draggedObjectMaterial;
    }
}
