using UnityEngine;
using System.Collections;

public class SelectableTextures : MonoBehaviour, InteractionListenerInterface
{
    [Tooltip("List of the objects that may be dragged and dropped.")]
    public GameObject[] selectableTextures;

    [Tooltip("Material used to outline the currently selected object.")]
    public Material selectedObjectMaterial;

    [Tooltip("Drag speed of the selected object.")]
    public float dragSpeed = 3.0f;

    [Tooltip("Scaling factor for newly created objects.")]
    public float scaleFactor = 7.5f;

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

    [Tooltip("Interaction manager instance, used to detect hand interactions. If left empty, it will be the first interaction manager found in the scene.")]
    private InteractionManager interactionManager;

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
    public Material draggedObjectMaterial;
    private float draggedNormalZ;

    // initial objects' positions and rotations (used for resetting objects)
    private Vector3[] initialObjPos;
    private Quaternion[] initialObjRot;

    // normalized and pixel position of the cursor
    private Vector3 screenNormalPos = Vector3.zero;
    private Vector3 screenPixelPos = Vector3.zero;
    private Vector3 newObjectPos = Vector3.zero;

    // reference to the gesture listener
    private ModelGestureListener gestureListener;
    private Vector3 texturesScrollbarPosition;
    private bool swipedUp = true;

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


    void Start()
    {
        // by default set the main-camera to be screen-camera
        if (screenCamera == null)
        {
            screenCamera = Camera.main;
        }

        // save the initial positions and rotations of the objects
        initialObjPos = new Vector3[selectableTextures.Length];
        initialObjRot = new Quaternion[selectableTextures.Length];

        for (int i = 0; i < selectableTextures.Length; i++)
        {
            initialObjPos[i] = screenCamera ? screenCamera.transform.InverseTransformPoint(selectableTextures[i].transform.position) : selectableTextures[i].transform.position;
            initialObjRot[i] = screenCamera ? Quaternion.Inverse(screenCamera.transform.rotation) * selectableTextures[i].transform.rotation : selectableTextures[i].transform.rotation;
        }

        // get the interaction manager instance
        if (interactionManager == null)
        {
            //interactionManager = InteractionManager.Instance;
            interactionManager = GetInteractionManager();
        }

        // get the gestures listener
        gestureListener = ModelGestureListener.Instance;
        texturesScrollbarPosition = this.transform.position;
    }


    // tries to locate a proper interaction manager in the scene
    private InteractionManager GetInteractionManager()
    {
        // find the proper interaction manager
        MonoBehaviour[] monoScripts = FindObjectsOfType(typeof(MonoBehaviour)) as MonoBehaviour[];

        foreach (MonoBehaviour monoScript in monoScripts)
        {
            if ((monoScript is InteractionManager) && monoScript.enabled)
            {
                InteractionManager manager = (InteractionManager)monoScript;

                if (manager.playerIndex == playerIndex && manager.leftHandInteraction == leftHandInteraction)
                {
                    return manager;
                }
            }
        }

        // not found
        return null;
    }

    void Update()
    {
        if(gestureListener != null && draggedObjectMaterial != null)
        {
            if (gestureListener.IsSwipingDown() && swipedUp)
            {
                texturesScrollbarPosition = new Vector3(this.transform.position.x, this.transform.position.y + 20, this.transform.position.z);
                swipedUp = false;
            }
            else if (gestureListener.IsSwipingUp() && !swipedUp)
            {
                texturesScrollbarPosition = new Vector3(this.transform.position.x, this.transform.position.y - 20, this.transform.position.z);
                swipedUp = true;
            }
            this.transform.position = Vector3.Lerp(this.transform.position, texturesScrollbarPosition, dragSpeed * Time.deltaTime);
        }

        if (interactionManager != null && interactionManager.IsInteractionInited())
        {
            if (resetObjects && draggedObject == null)
            {
                // reset the objects as needed
                resetObjects = false;
                ResetObjects();
            }
            
            // no object is currently selected or dragged.
            bool bHandIntAllowed = (leftHandInteraction && interactionManager.IsLeftHandPrimary()) || (rightHandInteraction && interactionManager.IsRightHandPrimary());

            // check if there is an underlying object to be selected
            if (lastHandEvent == InteractionManager.HandEventType.Grip && bHandIntAllowed)
            {
                // convert the normalized screen pos to pixel pos
                screenNormalPos = interactionManager.IsLeftHandPrimary() ? interactionManager.GetLeftHandScreenPos() : interactionManager.GetRightHandScreenPos();

                screenPixelPos.x = (int)(screenNormalPos.x * (screenCamera ? screenCamera.pixelWidth : Screen.width));
                screenPixelPos.y = (int)(screenNormalPos.y * (screenCamera ? screenCamera.pixelHeight : Screen.height));
                Ray ray = screenCamera ? screenCamera.ScreenPointToRay(screenPixelPos) : new Ray();

                // check if there is an underlying objects
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    foreach (GameObject obj in selectableTextures)
                    {
                        if (hit.collider.gameObject == obj)
                        {
                            // get selection material
                            draggedObject = obj;
                            draggedObjectMaterial = obj.GetComponent<Renderer>().material;
                            break;
                        }
                    }
                }
            }
        }
    }

    void OnGUI()
    {
        if (infoGuiText != null && interactionManager != null && interactionManager.IsInteractionInited())
        {
            string sInfo = string.Empty;

            long userID = interactionManager.GetUserID();
            if (userID != 0)
            {
                if (draggedObjectMaterial != null)
                    sInfo = "Selected " + draggedObject.name.ToLower() + " material.";
            }
            infoGuiText.text = sInfo;
        }
    }


    // reset positions and rotations of the objects
    private void ResetObjects()
    {
        for (int i = 0; i < selectableTextures.Length; i++)
        {
            selectableTextures[i].GetComponent<Rigidbody>().useGravity = false;
            selectableTextures[i].GetComponent<Rigidbody>().velocity = Vector3.zero;

            selectableTextures[i].transform.position = screenCamera ? screenCamera.transform.TransformPoint(initialObjPos[i]) : initialObjPos[i];
            selectableTextures[i].transform.rotation = screenCamera ? screenCamera.transform.rotation * initialObjRot[i] : initialObjRot[i];
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
