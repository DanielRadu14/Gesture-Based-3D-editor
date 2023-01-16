using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class CubeGenerator : UnityEngine.MonoBehaviour
{
    Mesh planeMesh;
    public Mesh cubeMesh;
    MeshFilter meshFilter;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();

    [SerializeField] public float size;
    [SerializeField] public int resolution;
    [SerializeField] bool isSphere;
    [SerializeField] bool isPlane;
    [Range(0, 1)] public float morphValue;
    [SerializeField] Vector3 shiftVertex;
    public Vector3 draggedVertex;
    public GameObject vertexObject;
    private GameObject verticesContainer;

    //helper Variables
    float previousSize;
    int previousResolution;
    bool previousSphereState;
    float previousMorphValue;
    Vector3 previousShiftVertex;
    Vector3 previousOrigin;
    Quaternion previousRotation;
    Vector3 previousDraggedVertex;

    private bool boxColliderAttached;

    void Awake()
    {
        planeMesh = new Mesh();
        cubeMesh = new Mesh();
        meshFilter = this.GetComponent<MeshFilter>();
        meshFilter.mesh = new Mesh();

        boxColliderAttached = false;

        verticesContainer = new GameObject(this.gameObject.name + " - vertices");
    }

    void Update()
    {
        //clamps resolution avoid errors and performance issues
        resolution = Mathf.Clamp(resolution, 1, 30);

        //only generate when changes occur
        
        if (ValuesHaveChanged())
        {
            if(isPlane)
            {
                Mesh planeMesh = GeneratePlane(size, resolution);
                cubeMesh.triangles = new int[0];
                cubeMesh.vertices = planeMesh.vertices;
                cubeMesh.triangles = planeMesh.triangles;

                vertices = new List<Vector3>(planeMesh.vertices);
                triangles = new List<int>(planeMesh.triangles);

                AssignMesh(planeMesh);

                DestroyVerticesObjects();
                GenerateVerticesObjects();

                if (!boxColliderAttached)
                {
                    this.gameObject.AddComponent<BoxCollider>();
                    boxColliderAttached = true;
                }
            }
            else
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

                if (size != previousSize)
                {
                    Destroy(this.gameObject.GetComponent<MeshCollider>());
                    this.gameObject.AddComponent<MeshCollider>().convex = true;
                }
            }

            //help keep track of changes
            AssignValuesAsPreviousValues();
        }
        else if (previousDraggedVertex != draggedVertex || previousShiftVertex != shiftVertex)
        {
            ModifyVertices();
            AssignMesh(cubeMesh);

            previousDraggedVertex = draggedVertex;
            previousShiftVertex = shiftVertex;
        }
    }

    void DestroyVerticesObjects()
    {
        if (!GrabDropScript.Instance.objectCorrespondingVertices.ContainsKey(this.gameObject))
            return;

        foreach(KeyValuePair<GameObject, Vector3> vertexToPositionMap in GrabDropScript.Instance.objectCorrespondingVertices[this.gameObject])
        {
            Destroy(vertexToPositionMap.Key);
            GrabDropScript.Instance.draggableVertices.Remove(vertexToPositionMap.Key);
            GrabDropScript.Instance.draggableObjects.Remove(vertexToPositionMap.Key);
        }

        GrabDropScript.Instance.objectCorrespondingVertices.Clear();
    }

    void GenerateVerticesObjects()
    {
        List<Vector3> alreadyCreatedVerticesObects = new List<Vector3>();
        List<GameObject> createdVerticesObjects = new List<GameObject>();
        Dictionary<GameObject, Vector3> vertexToPositionAuxMap = new Dictionary<GameObject, Vector3>();

        verticesContainer.transform.position = Vector3.zero;
        verticesContainer.transform.rotation = Quaternion.identity;

        foreach (Vector3 vertexRelativePos in cubeMesh.vertices)
        {
            if (alreadyCreatedVerticesObects.Contains(vertexRelativePos))
                continue;

            GameObject vertexGameObject = Instantiate(vertexObject, vertexRelativePos, Quaternion.identity);
            vertexGameObject.name = "Vertex" + GrabDropScript.Instance.vertexObjectsCount++;
            vertexGameObject.transform.localScale = new Vector3(size / 5 / resolution, size / 5 / resolution, size / 5 / resolution);
            vertexGameObject.transform.parent = verticesContainer.transform;

            if (GameModePicker.Instance.GetGameMode() == GameModePicker.GameMode.Default)
                vertexGameObject.GetComponent<SphereCollider>().enabled = false;
            else if (GameModePicker.Instance.GetGameMode() == GameModePicker.GameMode.Vertex ||
                GameModePicker.Instance.GetGameMode() == GameModePicker.GameMode.Terrain)
                vertexGameObject.GetComponent<SphereCollider>().enabled = true;

            GrabDropScript.Instance.draggableVertices.Add(vertexGameObject);
            GrabDropScript.Instance.draggableObjects.Add(vertexGameObject);

            vertexToPositionAuxMap.Add(vertexGameObject, vertexRelativePos);

            alreadyCreatedVerticesObects.Add(vertexRelativePos);
            createdVerticesObjects.Add(vertexGameObject);
        }
        
        verticesContainer.transform.rotation = this.gameObject.transform.rotation;
        verticesContainer.transform.position = this.gameObject.transform.position;

        GrabDropScript.Instance.objectCorrespondingVertices.Add(this.gameObject, vertexToPositionAuxMap);
        //DisplayObjectCorrespondingVertices();
    }

    //for debug only
    private void DisplayObjectCorrespondingVertices()
    {
        foreach(GameObject cube in GrabDropScript.Instance.objectCorrespondingVertices.Keys)
        {
            Debug.Log("For object " + cube);
            foreach (KeyValuePair<GameObject, Vector3> vertexToPositionMap in GrabDropScript.Instance.objectCorrespondingVertices[this.gameObject])
            {
                Debug.Log("Vertex Object " + vertexToPositionMap.Key + " vertex relative position + " + vertexToPositionMap.Value);
            }
        }
    }

    public void AssignShiftValueAndDraggedVertex(Vector3 draggedVertex, Vector3 shiftVertex)
    {
        //GrabDropScript.Instance.debugText.text = "vertex " + draggedVertex + " shift vertex " + shiftVertex;
        this.draggedVertex = draggedVertex;
        this.shiftVertex = shiftVertex;
    }

    void GenerateCube(float size, int resolution)
    {
        List<Vector2> uvCoord = new List<Vector2>();
        vertices.Clear();
        triangles.Clear();
        DestroyVerticesObjects();

        Mesh planeMesh = GeneratePlane(size, resolution);

        //FrontFace 
        List<Vector3> frontVertices = ShiftVertices(planeMesh.vertices, -Vector3.forward * size / 2);
        List<int> frontTriangles = new List<int>(planeMesh.triangles);
        vertices.AddRange(frontVertices);
        triangles.AddRange(frontTriangles);
        Vector2[] uv = new Vector2[(resolution + 1) * (resolution + 1)];
        for (int i = 0, y = 0; y <= resolution; y++)
        {
            for (int x = 0; x <= resolution; x++, i++)
            {
                uv[i] = new Vector2((float)x / resolution, (float)y / resolution);
            }
        }
        uvCoord.AddRange(new List<Vector2>(uv));

        //BackFace
        List<Vector3> backVertices = ShiftVertices(planeMesh.vertices, Vector3.forward * size / 2);
        List<int> backTriangles = ShiftTriangleIndexes(ReverseTriangles(planeMesh.triangles), vertices.Count);
        vertices.AddRange(backVertices);
        triangles.AddRange(backTriangles);
        uv = new Vector2[(resolution + 1) * (resolution + 1)];
        for (int i = 0, y = 0; y <= resolution; y++)
        {
            for (int x = 0; x <= resolution; x++, i++)
            {
                uv[i] = new Vector2((float)x / resolution, (float)y / resolution);
            }
        }
        uvCoord.AddRange(new List<Vector2>(uv));

        //Dimension switch
        Mesh rotatedPlane = new Mesh();
        rotatedPlane.vertices = SwitchXAndZ(planeMesh.vertices);
        rotatedPlane.triangles = planeMesh.triangles;

        //RightFace
        List<Vector3> rightVertices = ShiftVertices(rotatedPlane.vertices, Vector3.right * size / 2);
        List<int> rightTriangles = ShiftTriangleIndexes(rotatedPlane.triangles, vertices.Count);
        vertices.AddRange(rightVertices);
        triangles.AddRange(rightTriangles);
        uv = new Vector2[(resolution + 1) * (resolution + 1)];
        for (int i = 0, y = 0; y <= resolution; y++)
        {
            for (int x = 0; x <= resolution; x++, i++)
            {
                uv[i] = new Vector2((float)x / resolution, (float)y / resolution);
            }
        }
        uvCoord.AddRange(new List<Vector2>(uv));

        //LeftFace
        List<Vector3> leftVertices = ShiftVertices(rotatedPlane.vertices, Vector3.left * size / 2);
        List<int> leftTriangles = ShiftTriangleIndexes(ReverseTriangles(rotatedPlane.triangles), vertices.Count);
        vertices.AddRange(leftVertices);
        triangles.AddRange(leftTriangles);
        uv = new Vector2[(resolution + 1) * (resolution + 1)];
        for (int i = 0, y = 0; y <= resolution; y++)
        {
            for (int x = 0; x <= resolution; x++, i++)
            {
                uv[i] = new Vector2((float)x / resolution, (float)y / resolution);
            }
        }
        uvCoord.AddRange(new List<Vector2>(uv));

        //Dimension switch 2: the enswitchening
        rotatedPlane.vertices = SwitchYAndZ(planeMesh.vertices);
        rotatedPlane.triangles = planeMesh.triangles;

        //TopFace
        List<Vector3> topVertices = ShiftVertices(rotatedPlane.vertices, Vector3.up * size / 2);
        List<int> topTriangles = ShiftTriangleIndexes(rotatedPlane.triangles, vertices.Count);
        vertices.AddRange(topVertices);
        triangles.AddRange(topTriangles);
        uv = new Vector2[(resolution + 1) * (resolution + 1)];
        for (int i = 0, y = 0; y <= resolution; y++)
        {
            for (int x = 0; x <= resolution; x++, i++)
            {
                uv[i] = new Vector2((float)x / resolution, (float)y / resolution);
            }
        }
        uvCoord.AddRange(new List<Vector2>(uv));

        //BottomFace
        List<Vector3> bottomVertices = ShiftVertices(rotatedPlane.vertices, Vector3.down * size / 2);
        List<int> bottomTriangles = ShiftTriangleIndexes(ReverseTriangles(rotatedPlane.triangles), vertices.Count);
        vertices.AddRange(bottomVertices);
        triangles.AddRange(bottomTriangles);
        uv = new Vector2[(resolution + 1) * (resolution + 1)];
        for (int i = 0, y = 0; y <= resolution; y++)
        {
            for (int x = 0; x <= resolution; x++, i++)
            {
                uv[i] = new Vector2((float)x / resolution, (float)y / resolution);
            }
        }
        uvCoord.AddRange(new List<Vector2>(uv));

        cubeMesh.Clear();
        cubeMesh.vertices = vertices.ToArray();
        cubeMesh.triangles = triangles.ToArray();
        cubeMesh.RecalculateNormals();
        cubeMesh.uv = uvCoord.ToArray();
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
        planeMesh.RecalculateNormals();

        Vector2[] uv = new Vector2[(resolution + 1) * (resolution + 1)];
        for (int i = 0, y = 0; y <= resolution; y++)
        {
            for (int x = 0; x <= resolution; x++, i++)
            {
                uv[i] = new Vector2((float)x / resolution, (float)y / resolution);
            }
        }
        planeMesh.uv = uv;
        return planeMesh;
    }

    void AssignMesh(Mesh mesh)
    {
        Mesh filterMesh = meshFilter.mesh;
        filterMesh.Clear();
        filterMesh.vertices = mesh.vertices;
        filterMesh.triangles = mesh.triangles;

        if(isPlane)
        {
            filterMesh.RecalculateNormals();

            Vector2[] uv = new Vector2[(resolution + 1) * (resolution + 1)];
            for (int i = 0, y = 0; y <= resolution; y++)
            {
                for (int x = 0; x <= resolution; x++, i++)
                {
                    uv[i] = new Vector2((float)x / resolution, (float)y / resolution);
                }
            }
            filterMesh.uv = uv;
        }
        else
        {
            filterMesh.normals = mesh.normals;
            filterMesh.uv = mesh.uv;
        }
    }

    void ModifyVertices()
    {
        vertices = ShiftVertex(vertices.ToArray(), shiftVertex, draggedVertex);

        cubeMesh.vertices = vertices.ToArray();
        cubeMesh.triangles = triangles.ToArray();

        DestroyVerticesObjects();
        GenerateVerticesObjects();
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
        List<Vector3> nearbyVertices = new List<Vector3>();
        foreach (Vector3 vertex in vertices)
        {
            if(vertex == vertexToBeModified)
            {
                Vector3 newPos = new Vector3(vertex.x + shiftValue.x, vertex.y + shiftValue.y, vertex.z + shiftValue.z);
                shiftedVertices.Add(newPos);
                nearbyVertices = GetNearbyVertices(vertex);
            }
            else
            {
                shiftedVertices.Add(vertex);
            }
        }
        return ShiftNearbyVertices(shiftedVertices, shiftValue / 2, nearbyVertices);
    }

    List<Vector3> ShiftNearbyVertices(List<Vector3> vertices, Vector3 shiftValue, List<Vector3> verticesToBeModified)
    {
        List<Vector3> shiftedVertices = new List<Vector3>();
        foreach (Vector3 vertex in vertices)
        {
            if (verticesToBeModified.Contains(vertex))
            {
                Vector3 newPos = new Vector3(vertex.x + shiftValue.x, vertex.y + shiftValue.y, vertex.z + shiftValue.z);
                shiftedVertices.Add(newPos);
            }
            else
            {
                shiftedVertices.Add(vertex);
            }
        }
        return shiftedVertices;
    }

    private List<Vector3> GetNearbyVertices(Vector3 origin)
    {
        List<Vector3> nearbyVertices = new List<Vector3>();
        foreach (Vector3 vertex in vertices)
        {
            if (IsNearbyVertex(origin, vertex))
            {
                nearbyVertices.Add(vertex);
            }
        }
        return nearbyVertices;
    }

    private bool IsNearbyVertex(Vector3 origin, Vector3 possibleNearbyVertex)
    {
        if(origin == possibleNearbyVertex)
            return false;

        if (Mathf.Abs(possibleNearbyVertex.x - origin.x) <= size / resolution &&
            Mathf.Abs(possibleNearbyVertex.y - origin.y) <= size / resolution &&
            Mathf.Abs(possibleNearbyVertex.z - origin.z) <= size / resolution)
        {
            return true;
        }
        return false;
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
        if (previousSize != size || previousResolution != resolution || previousOrigin != this.transform.position || previousRotation != this.transform.rotation || previousSphereState != isSphere || morphValue != previousMorphValue)
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
        previousRotation = this.transform.rotation;
        previousSphereState = isSphere;
        previousMorphValue = morphValue;
        previousShiftVertex = shiftVertex;
        previousDraggedVertex = draggedVertex;
    }
}