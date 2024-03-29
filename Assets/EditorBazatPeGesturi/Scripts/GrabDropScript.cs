using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GrabDropScript : UnityEngine.MonoBehaviour, InteractionListenerInterface
{
	[Tooltip("List of the objects that may be dragged and dropped.")]
	public List<GameObject> draggableObjects;

    [Tooltip("List of the objects' vertices that may be dragged")]
    public List<GameObject> draggableVertices;

    [Tooltip("Material used to outline the currently selected object.")]
	public Material selectedObjectMaterial;
	
	[Tooltip("Drag speed of the selected object.")]
	public float dragSpeed = 3.0f;

    [Tooltip("Drag speed of the selected vertex in Terrain Mode.")]
    public float terrainDragSpeed = 3.0f;

    [Tooltip("Smooth factor used for object rotation.")]
    public float smoothFactor = 3.0f;

    [Tooltip("Minimum Z-position of the dragged object, when moving forward and back.")]
	public float minZ = 0f;

	[Tooltip("Maximum Z-position of the dragged object, when moving forward and back.")]
	public float maxZ = 5f;

	// public options (used by the Options GUI)
	[Tooltip("Whether the objects obey gravity when released, or not. Used by the Options GUI-window.")]
	public bool useGravity = true;
	[Tooltip("Whether the objects should be put in their original positions. Used by the Options GUI-window.")]
	public bool resetObjects = false;

	[Tooltip("Camera used for screen ray-casting. This is usually the main camera.")]
	public Camera screenCamera;

	[Tooltip("UI-Text used to display information messages.")]
	public UnityEngine.UI.Text infoGuiText;

    [Tooltip("UI-Text used to display the picked game mode.")]
    public UnityEngine.UI.Text debugText;

    [Tooltip("Interaction manager instance, used to detect hand interactions. If left empty, it will be the first interaction manager found in the scene.")]
	public InteractionManager interactionManager;

    [Tooltip("Index of the player, tracked by the respective InteractionManager. 0 means the 1st player, 1 - the 2nd one, 2 - the 3rd one, etc.")]
    public int playerIndex = 0;

    [Tooltip("Whether the left hand interaction is allowed by the respective InteractionManager.")]
	public bool leftHandInteraction = false;

	[Tooltip("Whether the right hand interaction is allowed by the respective InteractionManager.")]
	public bool rightHandInteraction = true;


	// hand interaction variables
	//private bool isLeftHandDrag = false;
	private InteractionManager.HandEventType lastHandEvent = InteractionManager.HandEventType.None;

	// currently dragged object and its parameters
	private GameObject draggedObject;
	//private float draggedObjectDepth;
	private Vector3 draggedObjectOffset;
	private Material draggedObjectMaterial;
	private float draggedNormalZ;

	// initial objects' positions and rotations (used for resetting objects)
	private Vector3[] initialObjPos;
	private Quaternion[] initialObjRot;

	// normalized and pixel position of the cursor
	private Vector3 screenNormalPos = Vector3.zero;
	private Vector3 screenPixelPos = Vector3.zero;
	private Vector3 newObjectPos = Vector3.zero;

    public int vertexObjectsCount = 0;
    public Dictionary<GameObject, Dictionary<GameObject, Vector3>> objectCorrespondingVertices = 
        new Dictionary<GameObject, Dictionary<GameObject, Vector3>>();
    private Vector3 draggedVertex = Vector3.zero;
    private CubeGenerator draggedVertexCubeGenerator = null;
    public bool rotatingObject = false;
    private bool canInstantiateTree = true;

    private bool gameModeSelected = false;

    protected static GrabDropScript instance = null;
    public static GrabDropScript Instance
    {
        get
        {
            return instance;
        }
    }

    // choose whether to use gravity or not
    public void SetUseGravity(bool bUseGravity)
	{
		this.useGravity = bUseGravity;
	}

	// request resetting of the draggable objects
	public void RequestObjectReset()
	{
		resetObjects = true;
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

	void Start()
	{
		// by default set the main-camera to be screen-camera
		if (screenCamera == null) 
		{
			screenCamera = Camera.main;
		}

		// save the initial positions and rotations of the objects
		initialObjPos = new Vector3[draggableObjects.Count];
		initialObjRot = new Quaternion[draggableObjects.Count];

		for(int i = 0; i < draggableObjects.Count; i++)
		{
			initialObjPos[i] = screenCamera ? screenCamera.transform.InverseTransformPoint(draggableObjects[i].transform.position) : draggableObjects[i].transform.position;
			initialObjRot[i] = screenCamera ? Quaternion.Inverse(screenCamera.transform.rotation) * draggableObjects[i].transform.rotation : draggableObjects[i].transform.rotation;
		}

		// get the interaction manager instance
		if(interactionManager == null)
		{
            //interactionManager = InteractionManager.Instance;
            interactionManager = GetInteractionManager();
        }
	}


    // tries to locate a proper interaction manager in the scene
    private InteractionManager GetInteractionManager()
    {
        // find the proper interaction manager
        UnityEngine.MonoBehaviour[] monoScripts = FindObjectsOfType(typeof(UnityEngine.MonoBehaviour)) as UnityEngine.MonoBehaviour[];

        foreach (UnityEngine.MonoBehaviour monoScript in monoScripts)
        {
            if ((monoScript is InteractionManager) && monoScript.enabled)
            {
                InteractionManager manager = (InteractionManager)monoScript;

                if (manager.playerIndex == playerIndex && manager.rightHandInteraction == rightHandInteraction)
                {
                    return manager;
                }
            }
        }

        // not found
        return null;
    }


    private Vector3 FindVertexRelativePosition(GameObject vertex)
    {
        foreach (Dictionary<GameObject, Vector3> vertexToPositionMap in objectCorrespondingVertices.Values)
        {
            if (vertexToPositionMap.ContainsKey(vertex))
            {
                return vertexToPositionMap[vertex];
            }
        }
        return Vector3.zero;
    }

    private CubeGenerator GetCubeGeneratorForDraggedVertex(GameObject vertex)
    {
        foreach (KeyValuePair<GameObject, Dictionary<GameObject, Vector3>> kpv in objectCorrespondingVertices)
        {
            Dictionary<GameObject, Vector3> vertexGameObjToVertexPos = kpv.Value;
            if (vertexGameObjToVertexPos.ContainsKey(vertex))
            {
                return kpv.Key.GetComponent<CubeGenerator>();
            }
        }
        return null;
    }

    void Update() 
	{
        if (draggedVertexCubeGenerator != null)
            debugText.text = draggedVertexCubeGenerator.gameObject.name;
        else
            debugText.text = "null";

        if (interactionManager != null && interactionManager.IsInteractionInited())
		{
			if(resetObjects && draggedObject == null)
			{
				// reset the objects as needed
				resetObjects = false;
				ResetObjects();
			}

            //Terrain Mode
            //move grabbed vertex as long as last hand event is Gripped
            if (GameModePicker.Instance.GetGameMode() == GameModePicker.GameMode.Terrain && draggedVertexCubeGenerator != null)
            {
                float moveFactor;
                if (interactionManager.IsRightHandPrimary())
                {
                    moveFactor = -Time.deltaTime * terrainDragSpeed;
                }
                else
                {
                    moveFactor = Time.deltaTime * terrainDragSpeed;
                }
                Vector3 newPos = new Vector3(0, 0, moveFactor);
                draggedVertexCubeGenerator.AssignShiftValueAndDraggedVertex(draggedVertex, newPos);
                draggedVertex = draggedVertex + newPos;

                if(lastHandEvent == InteractionManager.HandEventType.Release)
                {
                    draggedVertexCubeGenerator = null;
                }
            }

            if (draggedObject == null)
			{
                // no object is currently selected or dragged.
                bool bHandIntAllowed = (leftHandInteraction && interactionManager.IsLeftHandPrimary()) || (rightHandInteraction && interactionManager.IsRightHandPrimary());

				// check if there is an underlying object to be selected
				if(lastHandEvent == InteractionManager.HandEventType.Grip && bHandIntAllowed)
				{
                    // convert the normalized screen pos to pixel pos
                    screenNormalPos = interactionManager.IsLeftHandPrimary() ? interactionManager.GetLeftHandScreenPos() : interactionManager.GetRightHandScreenPos();

					screenPixelPos.x = (int)(screenNormalPos.x * (screenCamera ? screenCamera.pixelWidth : Screen.width));
					screenPixelPos.y = (int)(screenNormalPos.y * (screenCamera ? screenCamera.pixelHeight : Screen.height));
					Ray ray = screenCamera ? screenCamera.ScreenPointToRay(screenPixelPos) : new Ray();

					// check if there is an underlying objects
					RaycastHit hit;
					if(Physics.Raycast(ray, out hit))
					{
                        //tree mode
                        if (GameModePicker.Instance.GetGameMode() == GameModePicker.GameMode.Tree && canInstantiateTree && hit.collider.gameObject == GameObject.Find("Plane"))
                        {
                            if (interactionManager.IsRightHandPrimary())
                            {
                                CreatableTreeTypes.Instance.CreateTree(hit.point);
                                canInstantiateTree = false;
                                StartCoroutine(ResetCanInstantiateTree());
                            }
                            else
                            {
                                CreatableTreeTypes.Instance.DeleteTrees(hit.point);
                            }
                        }

                        //all created objects in the scene, all vertices from the objects in the scene, all selectable objects (panels on the right of the screen)
                        foreach (GameObject obj in draggableObjects)
						{
							if(hit.collider.gameObject == obj)
							{
                                if (interactionManager.IsRightHandPrimary())
                                {
                                    // an object was hit by the ray. select it and start drgging
                                    draggedObject = obj;

                                    if (draggableVertices.Contains(draggedObject))
                                    {
                                        if(GameModePicker.Instance.GetGameMode() == GameModePicker.GameMode.Vertex)
                                        {
                                            draggedVertex = FindVertexRelativePosition(draggedObject);
                                        }
                                        else if(GameModePicker.Instance.GetGameMode() == GameModePicker.GameMode.Terrain)
                                        {
                                            draggedVertex = FindVertexRelativePosition(draggedObject);
                                            draggedVertexCubeGenerator = GetCubeGeneratorForDraggedVertex(draggedObject);
                                        }
                                    }

                                    if(GameModePicker.Instance.GetGameMode() != GameModePicker.GameMode.Terrain)
                                    {
                                        draggedObjectOffset = hit.point - draggedObject.transform.position;
                                        draggedObjectOffset.z = 0; // don't change z-pos

                                        draggedNormalZ = (minZ + screenNormalPos.z * (maxZ - minZ)) -
                                            draggedObject.transform.position.z; // start from the initial hand-z

                                        // set selection material
                                        draggedObjectMaterial = draggedObject.GetComponent<Renderer>().material;
                                        draggedObject.GetComponent<Renderer>().material = selectedObjectMaterial;

                                        // stop using gravity while dragging object
                                        if (draggedObject.GetComponent<Rigidbody>() != null)
                                            draggedObject.GetComponent<Rigidbody>().useGravity = false;
                                    }
                                }
								else if(interactionManager.IsLeftHandPrimary())
                                {
                                    draggedObject = obj;

                                    if (draggableVertices.Contains(draggedObject))
                                    {
                                        if (GameModePicker.Instance.GetGameMode() == GameModePicker.GameMode.Terrain)
                                        {
                                            draggedVertex = FindVertexRelativePosition(draggedObject);
                                            draggedVertexCubeGenerator = GetCubeGeneratorForDraggedVertex(draggedObject);
                                        }
                                    }
                                    else
                                    {
                                        draggedObjectMaterial = draggedObject.GetComponent<Renderer>().material;
                                        draggedObject.GetComponent<Renderer>().material = selectedObjectMaterial;
                                    }
                                }
								break;
                            }
						}

                        //all selectable textures panels on the left of the screen
                        foreach (GameObject obj in SelectableObjects.Instance.selectableTextureTypes)
                        {
                            if (hit.collider.gameObject == obj)
                            {
                                if (interactionManager.IsLeftHandPrimary())
                                {
                                    draggedObject = obj;

                                    //select texture
                                    if (SelectTextureType.Instance.GetMaterial() == null)
                                    {
                                        SelectTextureType.Instance.SetMaterial(draggedObject.GetComponent<Renderer>().material);
                                    }
                                }
                                break;
                            }
                        }

                        //all the selectable game mode types panels on top of the screen
                        foreach (GameObject obj in SelectableObjects.Instance.selectableGameModeTypes)
                        {
                            if (hit.collider.gameObject == obj)
                            {
                                if (interactionManager.IsRightHandPrimary())
                                {
                                    if(!gameModeSelected)
                                    {
                                        GameModePicker.Instance.SetGameMode(obj.name);
                                        gameModeSelected = true;
                                        StartCoroutine(SetGameMode());
                                    }
                                }
                                break;
                            }
                        }
                    }
				}
			}
			else
			{
				bool bHandIntAllowed = (leftHandInteraction && interactionManager.IsLeftHandPrimary()) || (rightHandInteraction && interactionManager.IsRightHandPrimary());

				if (bHandIntAllowed) 
				{
                    if (interactionManager.IsRightHandPrimary())
                    {
                        // continue dragging the object
                        screenNormalPos = interactionManager.IsLeftHandPrimary() ? interactionManager.GetLeftHandScreenPos() : interactionManager.GetRightHandScreenPos();

                        // convert the normalized screen pos to 3D-world pos
                        screenPixelPos.x = (int)(screenNormalPos.x * (screenCamera ? screenCamera.pixelWidth : Screen.width));
                        screenPixelPos.y = (int)(screenNormalPos.y * (screenCamera ? screenCamera.pixelHeight : Screen.height));
                        //screenPixelPos.z = screenNormalPos.z + draggedObjectDepth;
                        screenPixelPos.z = (minZ + screenNormalPos.z * (maxZ - minZ)) - draggedNormalZ -
                            (screenCamera ? screenCamera.transform.position.z : 0f);

                        newObjectPos = screenCamera.ScreenToWorldPoint(screenPixelPos) - draggedObjectOffset;

                        //if it's an object or a vertex, move it to the new position
                        if (!SelectableObjects.Instance.selectableObjectTypes.Contains(draggedObject) && GameModePicker.Instance.GetGameMode() != GameModePicker.GameMode.Terrain)
                        {
                            draggedObject.transform.position = Vector3.Lerp(draggedObject.transform.position, newObjectPos, dragSpeed * Time.deltaTime);
                        }

                        // check if the object (hand grip) was released
                        bool isReleased = lastHandEvent == InteractionManager.HandEventType.Release;

                        if (isReleased)
                        {
                            //create object (a creatable object type panel was selected)
                            if (SelectableObjects.Instance.selectableObjectTypes.Contains(draggedObject))
                            {
                                CreateObjectType.Instance.CreateObject(draggedObject, newObjectPos);
                            }
                            //drop dragged object (whole object or vertex)
                            else
                            {
                                //drop dragged vertex
                                if (draggableVertices.Contains(draggedObject) && GameModePicker.Instance.GetGameMode() == GameModePicker.GameMode.Vertex)
                                {
                                    float x = newObjectPos.x != draggedVertex.x ? newObjectPos.x - draggedVertex.x : newObjectPos.x;
                                    float y = newObjectPos.x != draggedVertex.y ? newObjectPos.y - draggedVertex.y : newObjectPos.y;
                                    float z = newObjectPos.x != draggedVertex.z ? newObjectPos.z - draggedVertex.z : newObjectPos.z;
                                    Vector3 newPos = new Vector3(x, y, z);

                                    CubeGenerator cubeGenerator = GetCubeGeneratorForDraggedVertex(draggedObject);
                                    if(cubeGenerator)
                                    {
                                        cubeGenerator.AssignShiftValueAndDraggedVertex(draggedVertex, newPos);
                                    }
                                }
                                //drop dragged object
                                else
                                {
                                    Destroy(draggedObject.GetComponent<MeshCollider>());
                                    draggedObject.AddComponent<MeshCollider>().convex = true;
                                }

                                if (useGravity)
                                {
                                    // add gravity to the object
                                    draggedObject.GetComponent<Rigidbody>().useGravity = true;
                                }
                            }

                            draggedObject.GetComponent<Renderer>().material = draggedObjectMaterial;
                            draggedObject = null;
                            draggedObjectMaterial = null;
                        }
                    }
                    //apply selected texture with the left hand
                    else if (interactionManager.IsLeftHandPrimary())
                    {
                        // continue dragging the object
                        screenNormalPos = interactionManager.IsLeftHandPrimary() ? interactionManager.GetLeftHandScreenPos() : interactionManager.GetRightHandScreenPos();

                        float angleArounfY = screenNormalPos.x * 360f;  // horizontal rotation
                        float angleArounfX = screenNormalPos.y * 360f;  // vertical rotation

                        Vector3 vObjectRotation = new Vector3(-angleArounfX, -angleArounfY, 180f);
                        Quaternion qObjectRotation = screenCamera ? screenCamera.transform.rotation * Quaternion.Euler(vObjectRotation) : Quaternion.Euler(vObjectRotation);

                        Material material = SelectTextureType.Instance.GetMaterial();
                        //rotate object only if a material was not selected
                        if (material == null)
                        {
                            draggedObject.transform.rotation = Quaternion.Slerp(draggedObject.transform.rotation, qObjectRotation, smoothFactor * Time.deltaTime);
                            rotatingObject = true;
                        }
                        // check if the object (hand grip) was released
                        bool isReleased = lastHandEvent == InteractionManager.HandEventType.Release;

                        if (isReleased)
                        {
                            //apply selected texture
                            if (SelectTextureType.Instance.GetMaterial() != null)
                            {
                                // convert the normalized screen pos to pixel pos
                                screenNormalPos = interactionManager.IsLeftHandPrimary() ? interactionManager.GetLeftHandScreenPos() : interactionManager.GetRightHandScreenPos();

                                screenPixelPos.x = (int)(screenNormalPos.x * (screenCamera ? screenCamera.pixelWidth : Screen.width));
                                screenPixelPos.y = (int)(screenNormalPos.y * (screenCamera ? screenCamera.pixelHeight : Screen.height));
                                Ray ray = screenCamera ? screenCamera.ScreenPointToRay(screenPixelPos) : new Ray();

                                // check if there is an underlying objects
                                if (Physics.Raycast(ray, out RaycastHit hit))
                                {
                                    foreach (GameObject obj in draggableObjects)
                                    {
                                        if (hit.collider.gameObject == obj)
                                        {
                                            //apply texture
                                            obj.GetComponent<Renderer>().material = material;
                                            StartCoroutine(ResetDraggedObjectMaterial());
                                            break;
                                        }
                                    }
                                }
                            }
                            // restore the object's material and stop rotating the object
                            else
                            {
                                draggedObject.GetComponent<Renderer>().material = draggedObjectMaterial;
                                rotatingObject = false;
                            }
                            
                            draggedObject = null;
                            draggedObjectMaterial = null;
                        }
                    }
                }
			}
		}
	}

    private IEnumerator ResetDraggedObjectMaterial()
    {
        yield return new WaitForSeconds(0.5f);
        SelectTextureType.Instance.SetMaterial(null);
    }

    private IEnumerator SetGameMode()
    {
        yield return new WaitForSeconds(1.0f);
        gameModeSelected = false;
    }

    private IEnumerator ResetCanInstantiateTree()
    {
        yield return new WaitForSeconds(0.5f);
        canInstantiateTree = true;
    }

    void OnGUI()
	{
		if(infoGuiText != null && interactionManager != null && interactionManager.IsInteractionInited())
		{
			string sInfo = string.Empty;
			
			long userID = interactionManager.GetUserID();
			if(userID != 0)
			{
				if(draggedObject != null)
					sInfo = "Dragging the " + draggedObject.name + " around.";
				else
					sInfo = "Please grab and drag an object around.";
			}
			else
			{
				KinectManager kinectManager = KinectManager.Instance;

				if(kinectManager && kinectManager.IsInitialized())
				{
					sInfo = "Waiting for Users...";
				}
				else
				{
					sInfo = "Kinect is not initialized. Check the log for details.";
				}
			}
			
			infoGuiText.text = sInfo;
		}
	}


	// reset positions and rotations of the objects
	private void ResetObjects()
	{
		for(int i = 0; i < draggableObjects.Count; i++)
		{
			draggableObjects[i].GetComponent<Rigidbody>().useGravity = false;
			draggableObjects[i].GetComponent<Rigidbody>().velocity = Vector3.zero;

			draggableObjects[i].transform.position = screenCamera ? screenCamera.transform.TransformPoint(initialObjPos[i]) : initialObjPos[i];
			draggableObjects[i].transform.rotation = screenCamera ? screenCamera.transform.rotation * initialObjRot[i] : initialObjRot[i];
		}
	}


	public void HandGripDetected(long userId, int userIndex, bool isRightHand, bool isHandInteracting, Vector3 handScreenPos)
	{
		if (!isHandInteracting || !interactionManager)
			return;
		if (userId != interactionManager.GetUserID())
			return;

		lastHandEvent = InteractionManager.HandEventType.Grip;
		//isLeftHandDrag = !isRightHand;
		screenNormalPos = handScreenPos;
	}

	public void HandReleaseDetected(long userId, int userIndex, bool isRightHand, bool isHandInteracting, Vector3 handScreenPos)
	{
		if (!isHandInteracting || !interactionManager)
			return;
		if (userId != interactionManager.GetUserID())
			return;

		lastHandEvent = InteractionManager.HandEventType.Release;
		//isLeftHandDrag = !isRightHand;
		screenNormalPos = handScreenPos;
	}

	public bool HandClickDetected(long userId, int userIndex, bool isRightHand, Vector3 handScreenPos)
	{
		return true;
	}


}
