using UnityEngine;
using System.Collections;

public class CreateObjectType : MonoBehaviour
{
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