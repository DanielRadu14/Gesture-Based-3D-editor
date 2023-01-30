using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatableTreeTypes : MonoBehaviour
{
    public float deletionAreaDiameter = 10.0f;

    private List<GameObject> createdTrees = new List<GameObject>();
    private GameObject treeContainer;

    protected static CreatableTreeTypes instance = null;
    public static CreatableTreeTypes Instance
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

        treeContainer = new GameObject("TreeContainer");
    }

    private bool IsInDeletionArea(Vector3 deleteAreaOrigin, Vector3 treePosition)
    {
        if (Mathf.Abs(treePosition.x - deleteAreaOrigin.x) <= deletionAreaDiameter &&
            Mathf.Abs(treePosition.y - deleteAreaOrigin.y) <= deletionAreaDiameter &&
            Mathf.Abs(treePosition.z - deleteAreaOrigin.z) <= deletionAreaDiameter)
        {
            return true;
        }
        return false;
    }

    public void DeleteTrees(Vector3 deleteAreaOrigin)
    {
        List<GameObject> deletedTrees = new List<GameObject>();
        foreach(GameObject tree in createdTrees)
        {
            if(IsInDeletionArea(deleteAreaOrigin, tree.transform.position))
            {
                deletedTrees.Add(tree);
            }
        }

        foreach (GameObject tree in deletedTrees)
        {
            createdTrees.Remove(tree);
            Destroy(tree);
        }
    }

    public void CreateTree(Vector3 newObjectPos)
    {
        GameObject tree = SelectableObjects.Instance.creatableTreeTypes[(int)Random.Range(0, SelectableObjects.Instance.creatableTreeTypes.Count - 1)];
        GameObject createdTree = Instantiate(tree, newObjectPos, Quaternion.identity);
        createdTree.transform.parent = treeContainer.transform;
        createdTrees.Add(createdTree);
    }
}
