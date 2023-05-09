using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModePicker : UnityEngine.MonoBehaviour
{
    [Tooltip("UI-Text used to display the picked game mode.")]
    public UnityEngine.UI.Text gameModeDebugText;

    public enum GameMode { Default, Vertex, Terrain, Tree };
    public GameMode gameModeStat = GameMode.Default;

    private GameObject planeObject;

    public Transform camera;
    public float hidingOffset = 20.0f;
    public float slideSpeed = 5.0f;
    public float distanceFromCamera = 25;
    private ModelGestureListener gestureListener;
    private InteractionManager interactionManager;

    protected static GameModePicker instance = null;
    public static GameModePicker Instance
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

        planeObject = GameObject.Find("Plane");
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

        Vector3 topMiddleWorldPoint = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth / 2, Camera.main.pixelHeight, distanceFromCamera));
        Vector3 topMiddleScreenPoint = Camera.main.WorldToScreenPoint(topMiddleWorldPoint);

        if (!ShowMenu(topMiddleScreenPoint))
        {
            Vector3 moveFactor = camera.up.normalized * hidingOffset;
            Vector3 hidingPosition = new Vector3(topMiddleWorldPoint.x + moveFactor.x, topMiddleWorldPoint.y + moveFactor.y, topMiddleWorldPoint.z + moveFactor.z);
            this.transform.position = Vector3.Lerp(this.transform.position,
                                        hidingPosition,
                                        Mathf.Clamp01(Time.deltaTime * slideSpeed));
        }
        else
        {
            this.transform.position = Vector3.Lerp(this.transform.position,
                                        topMiddleWorldPoint,
                                        Mathf.Clamp01(Time.deltaTime * slideSpeed));
        }
    }

    private bool ShowMenu(Vector3 pivotScreenPoint)
    {
        Vector3 leftBottomBorder = new Vector3(pivotScreenPoint.x - 512, pivotScreenPoint.y - 56, 0);
        Vector3 rightTopBorder = new Vector3(pivotScreenPoint.x + 512, pivotScreenPoint.y, 0);

        Vector3 cursorNormalPos = interactionManager.IsLeftHandPrimary() ? interactionManager.GetLeftHandScreenPos() : interactionManager.GetRightHandScreenPos();

        Vector3 cursorPixelPos = Vector3.zero;
        cursorPixelPos.x = (int)(cursorNormalPos.x * (Camera.main ? Camera.main.pixelWidth : Screen.width));
        cursorPixelPos.y = (int)(cursorNormalPos.y * (Camera.main ? Camera.main.pixelHeight : Screen.height));

        return cursorPixelPos.x > leftBottomBorder.x && cursorPixelPos.x < rightTopBorder.x && cursorPixelPos.y > leftBottomBorder.y;
    }

    public void SetGameMode(string gameMode)
    {
        switch (gameMode)
        {
            case "Default":
                gameModeStat = GameMode.Default;
                foreach (GameObject gameObj in GrabDropScript.Instance.draggableObjects)
                {
                    if (gameObj.name.Contains("Vertex"))
                    {
                        gameObj.GetComponent<SphereCollider>().enabled = false;
                    }
                    else
                    {
                        if (!SelectableObjects.Instance.selectableObjectTypes.Contains(gameObj))
                        {
                            gameObj.GetComponent<MeshCollider>().enabled = true;
                        }
                    }
                }
                UpdateActiveGameObjects(true);
                break;
            case "Vertex":
                gameModeStat = GameMode.Vertex;
                foreach (GameObject gameObj in GrabDropScript.Instance.draggableObjects)
                {
                    if (gameObj.name.Contains("Vertex"))
                    {
                        gameObj.GetComponent<SphereCollider>().enabled = true;
                    }
                    else
                    {
                        if (!SelectableObjects.Instance.selectableObjectTypes.Contains(gameObj))
                        {
                            gameObj.GetComponent<MeshCollider>().enabled = false;
                        }
                    }
                }
                UpdateActiveGameObjects(false);
                break;
            case "Terrain":
                gameModeStat = GameMode.Terrain;
                foreach (GameObject gameObj in GrabDropScript.Instance.draggableObjects)
                {
                    if (gameObj.name.Contains("Vertex"))
                    {
                        gameObj.GetComponent<SphereCollider>().enabled = true;
                    }
                    else
                    {
                        if (!SelectableObjects.Instance.selectableObjectTypes.Contains(gameObj))
                        {
                            gameObj.GetComponent<MeshCollider>().enabled = false;
                        }
                    }
                }

                UpdateActiveGameObjects(false);
                SetPlane setPlaneScript = planeObject.GetComponent<SetPlane>();
                setPlaneScript.enabled = false;
                break;
            case "Tree":
                gameModeStat = GameMode.Tree;
                foreach (GameObject gameObj in GrabDropScript.Instance.draggableObjects)
                {
                    if (gameObj.name.Contains("Vertex"))
                    {
                        gameObj.GetComponent<SphereCollider>().enabled = false;
                    }
                }
                UpdateActiveGameObjects(false);
                Destroy(planeObject.GetComponent<MeshCollider>());
                planeObject.AddComponent<MeshCollider>();
                break;
            default:
                gameModeStat = GameMode.Default;
                break;
        }
    }

    public GameMode GetGameMode()
    {
        return gameModeStat;
    }

    private void UpdateActiveGameObjects(bool active)
    {
        CreateObjectType.Instance.SetMenuAvailability(active);
        SelectTextureType.Instance.SetMenuAvailability(active);
    }
}
