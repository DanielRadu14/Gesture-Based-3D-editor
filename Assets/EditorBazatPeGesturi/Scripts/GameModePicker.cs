using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModePicker : UnityEngine.MonoBehaviour
{
    public enum GameMode { Default, Vertex, Terrain };
    public GameMode gameModeStat = GameMode.Default;
    
    [Tooltip("UI-Text used to display the picked game mode.")]
    public UnityEngine.UI.Text gameModeDebugText;

    private GameObject planeObject;

    private List<GameObject> deactivatedGameObjects = new List<GameObject>();
    private List<string> gameObjectsToDeactivateFromInterface = new List<string> { "Canvas/Parallelepiped", "Canvas/Sphere", "Canvas/Cube",
                                                                                   "SelectableObjects/CreatableObject", "SelectableObjects/SelectableTextures"};

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

        foreach(string gameObjectName in gameObjectsToDeactivateFromInterface)
        {
            deactivatedGameObjects.Add(GameObject.Find(gameObjectName));
        }

        planeObject = GameObject.Find("Plane");
    }

    private void Update()
    {
        gameModeDebugText.text = gameModeStat.ToString();
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
        foreach(GameObject gameObject in deactivatedGameObjects)
        {
            gameObject.SetActive(active);
        }
    }
}
