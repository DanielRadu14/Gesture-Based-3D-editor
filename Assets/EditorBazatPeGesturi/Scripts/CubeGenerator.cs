using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class CubeGenerator : MonoBehaviour
{
    Mesh planeMesh;
    Mesh cubeMesh;
    MeshFilter meshFilter;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();

    [SerializeField] float size;
    [SerializeField] int resolution;
    [SerializeField] bool isSphere;
    [Range(0, 1)] public float morphValue;
    [SerializeField] Vector3 shiftVertex;
    public GameObject vertexObject;

    private List<GameObject> vertexObjects = new List<GameObject>();

    //helper Variables
    float previousSize;
    int previousResolution;
    bool previousSphereState;
    float previousMorphValue;
    Vector3 previousShiftVertex;
    Vector3 previousOrigin;

    private bool boxColliderAttached;

    void Awake()
    {
        planeMesh = new Mesh();
        cubeMesh = new Mesh();
        meshFilter = this.GetComponent<MeshFilter>();
        meshFilter.mesh = new Mesh();

        boxColliderAttached = false;
    }

    void Update()
    {
        //clamps resolution avoid errors and performance issues
        resolution = Mathf.Clamp(resolution, 1, 30);

        //only generate when changes occur
        if (ValuesHaveChanged())
        {
            GenerateCube(size, resolution);
            if (isSphere)
            {
                cubeMesh.vertices = SpherizeVectors(cubeMesh.vertices);
            }
            AssignMesh(cubeMesh);

            if (!boxColliderAttached)
            {
                this.gameObject.AddComponent<MeshCollider>().convex = true;
                boxColliderAttached = true;
            }

            if(size != previousSize)
            {
                Destroy(this.gameObject.GetComponent<MeshCollider>());
                this.gameObject.AddComponent<MeshCollider>().convex = true;
            }

            //help keep track of changes
            AssignValuesAsPreviousValues();
        }
    }

    void DestroyVerticesObjects()
    {
        foreach(GameObject vertexObject in vertexObjects)
        {
            Destroy(vertexObject);
            GrabDropScript.Instance.draggableVertices.Remove(vertexObject);
        }
        vertexObjects.Clear();
    }

    void GenerateVerticesObjects()
    {
        List<Vector3> alreadyCreatedVerticesObects = new List<Vector3>();

        foreach(Vector3 vertex in cubeMesh.vertices)
        {
            if (alreadyCreatedVerticesObects.Contains(vertex))
                continue;

            GameObject gameObject = Instantiate(vertexObject, this.transform.position + vertex, Quaternion.identity);
            gameObject.name = "Vertex" + GrabDropScript.Instance.vertexObjectsCount++;
            gameObject.transform.localScale = new Vector3(size / 20, size / 20, size / 20);
            alreadyCreatedVerticesObects.Add(vertex);
            vertexObjects.Add(gameObject);
            GrabDropScript.Instance.draggableVertices.Add(gameObject);
        }
    }

    void GenerateCube(float size, int resolution)
    {
        vertices.Clear();
        triangles.Clear();
        DestroyVerticesObjects();

        Mesh planeMesh = GeneratePlane(size, resolution);

        //FrontFace 
        List<Vector3> frontVertices = ShiftVertices(planeMesh.vertices, -Vector3.forward * size / 2);
        List<int> frontTriangles = new List<int>(planeMesh.triangles);
        vertices.AddRange(frontVertices);
        triangles.AddRange(frontTriangles);

        //BackFace
        List<Vector3> backVertices = ShiftVertices(planeMesh.vertices, Vector3.forward * size / 2);
        List<int> backTriangles = ShiftTriangleIndexes(ReverseTriangles(planeMesh.triangles), vertices.Count);
        vertices.AddRange(backVertices);
        triangles.AddRange(backTriangles);

        //Dimension switch
        Mesh rotatedPlane = new Mesh();
        rotatedPlane.vertices = SwitchXAndZ(planeMesh.vertices);
        rotatedPlane.triangles = planeMesh.triangles;

        //RightFace
        List<Vector3> rightVertices = ShiftVertices(rotatedPlane.vertices, Vector3.right * size / 2);
        List<int> rightTriangles = ShiftTriangleIndexes(rotatedPlane.triangles, vertices.Count);
        vertices.AddRange(rightVertices);
        triangles.AddRange(rightTriangles);

        //LeftFace
        List<Vector3> leftVertices = ShiftVertices(rotatedPlane.vertices, Vector3.left * size / 2);
        List<int> leftTriangles = ShiftTriangleIndexes(ReverseTriangles(rotatedPlane.triangles), vertices.Count);
        vertices.AddRange(leftVertices);
        triangles.AddRange(leftTriangles);

        //Dimension switch 2: the enswitchening
        rotatedPlane.vertices = SwitchYAndZ(planeMesh.vertices);
        rotatedPlane.triangles = planeMesh.triangles;

        //TopFace
        List<Vector3> topVertices = ShiftVertices(rotatedPlane.vertices, Vector3.up * size / 2);
        List<int> topTriangles = ShiftTriangleIndexes(rotatedPlane.triangles, vertices.Count);
        vertices.AddRange(topVertices);
        triangles.AddRange(topTriangles);

        //BottomFace
        List<Vector3> bottomVertices = ShiftVertices(rotatedPlane.vertices, Vector3.down * size / 2);
        List<int> bottomTriangles = ShiftTriangleIndexes(ReverseTriangles(rotatedPlane.triangles), vertices.Count);
        vertices.AddRange(bottomVertices);
        triangles.AddRange(bottomTriangles);

        cubeMesh.Clear();
        vertices = ShiftVertex(vertices.ToArray(), shiftVertex, vertices[0]);
        cubeMesh.vertices = vertices.ToArray();
        cubeMesh.triangles = triangles.ToArray();

        GenerateVerticesObjects();
    }

    //create a plane on the x,y axis centered at 0,0,0
    //modified version of method explained in https://youtu.be/-3ekimUWb9I 
    //"How to create and modify a plane mesh in Unity (Procedural mesh generation tutorial)"
    Mesh GeneratePlane(float size, int resolution)
    {
        //Create vertices
        List<Vector3> generatedVertices = new List<Vector3>();
        float sizePerStep = size / resolution;
        //Makes sure it's centered in the middle
        Vector3 shiftValue = ((size / 2) * (Vector3.left + Vector3.down));
        for (int y = 0; y < resolution + 1; y++)
        {
            for (int x = 0; x < resolution + 1; x++)
            {
                generatedVertices.Add(new Vector3(x * sizePerStep, y * sizePerStep, 0) + shiftValue);
            }
        }

        //Create triangles
        List<int> generatedTriangles = new List<int>();
        for (int row = 0; row < resolution; row++)
        {
            for (int column = 0; column < resolution; column++)
            {
                int i = (row * resolution) + row + column;

                //first triangle
                generatedTriangles.Add(i);
                generatedTriangles.Add(i + (resolution) + 1);
                generatedTriangles.Add(i + (resolution) + 2);

                //second triangle
                generatedTriangles.Add(i);
                generatedTriangles.Add(i + resolution + 2);
                generatedTriangles.Add(i + 1);
            }
        }
        planeMesh.Clear();
        planeMesh.vertices = generatedVertices.ToArray();
        planeMesh.triangles = generatedTriangles.ToArray();
        return planeMesh;
    }

    void AssignMesh(Mesh mesh)
    {
        Mesh filterMesh = meshFilter.mesh;
        filterMesh.Clear();
        filterMesh.vertices = mesh.vertices;
        filterMesh.triangles = mesh.triangles;
    }

    List<Vector3> ShiftVertices(Vector3[] vertices, Vector3 shiftValue)
    {
        List<Vector3> shiftedVertices = new List<Vector3>();
        foreach (Vector3 vertex in vertices)
        {
            shiftedVertices.Add(this.transform.position + vertex + shiftValue);
        }
        return shiftedVertices;
    }

    List<Vector3> ShiftVertex(Vector3[] vertices, Vector3 shiftValue, Vector3 vertexToBeModified)
    {
        List<Vector3> shiftedVertices = new List<Vector3>();
        foreach (Vector3 vertex in vertices)
        {
            if(vertex == vertexToBeModified)
            {
                shiftedVertices.Add(this.transform.position + vertex + shiftValue);
            }
            else
            {
                shiftedVertices.Add(this.transform.position + vertex);
            }
        }
        return shiftedVertices;
    }

    int[] ReverseTriangles(int[] triangles)
    {
        System.Array.Reverse(triangles);
        return triangles;
    }

    List<int> ShiftTriangleIndexes(int[] triangles, int shiftValue)
    {
        List<int> newTriangles = new List<int>();
        foreach (int triangleIndex in triangles)
        {
            newTriangles.Add(triangleIndex + shiftValue);
        }
        return newTriangles;
    }

    Vector3[] SwitchXAndZ(Vector3[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            Vector3 value = values[i];
            float storedValue = value.x;
            value.x = value.z;
            value.z = storedValue;
            values[i] = value;
        }
        return values;
    }

    Vector3[] SwitchYAndZ(Vector3[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            Vector3 value = values[i];
            float storedValue = value.y;
            value.y = value.z;
            value.z = storedValue;
            values[i] = value;
        }
        return values;
    }

    Vector3[] SpherizeVectors(Vector3[] vectors)
    {
        for (int i = 0; i < vectors.Length; i++)
        {
            Vector3 vector = vectors[i] - this.transform.position;
            Vector3 sphereVector = vector.normalized * (size / 2) * 1.67f;
            Vector3 lerpdVector = Vector3.Lerp(vector, sphereVector, morphValue);
            vectors[i] = this.transform.position + lerpdVector;
        }
        return vectors;
    }

    bool ValuesHaveChanged()
    {
        if (previousShiftVertex != shiftVertex || previousSize != size || previousResolution != resolution || previousOrigin != this.transform.position || previousSphereState != isSphere || morphValue != previousMorphValue)
        {
            return true;
        }
        else return false;
    }

    void AssignValuesAsPreviousValues()
    {
        previousSize = size;
        previousResolution = resolution;
        previousOrigin = this.transform.position;
        previousSize = size;
        previousResolution = resolution;
        previousOrigin = this.transform.position;
        previousSphereState = isSphere;
        previousMorphValue = morphValue;
        previousShiftVertex = shiftVertex;
    }
}