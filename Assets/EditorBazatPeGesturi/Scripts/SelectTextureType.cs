using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectTextureType : MonoBehaviour
{
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

    public void SetMaterial(Material draggedObjectMaterial)
    {
        this.draggedObjectMaterial = draggedObjectMaterial;
    }

    public Material GetMaterial()
    {
        return draggedObjectMaterial;
    }
}
