using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor;
public class SelectableObjects : MonoBehaviour
//CreateObjectType.Instance.CreateObject(draggedObject, newObjectPos);
{
    [Tooltip("List of the objects that may be dragged and dropped.")]
    public List<GameObject> selectableObjectTypes;

    [Tooltip("List of the objects that may be dragged and dropped.")]
    public List<GameObject> creatableObjectTypes;

    [Tooltip("List of the objects that may be dragged and dropped.")]
    public List<GameObject> selectableTextureTypes;

    [Tooltip("List of the objects that may be dragged and dropped.")]
    public List<GameObject> selectableGameModeTypes;

    protected static SelectableObjects instance = null;
    public static SelectableObjects Instance
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
        if(GrabDropScript.Instance != null)
        {
            foreach(GameObject gameObj in selectableObjectTypes)
            {
                GrabDropScript.Instance.draggableObjects.Add(gameObj);
            }
        }
    }
}