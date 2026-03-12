using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    public Transform cameraTransform;
    public float interactDistance = 3f;
    public LayerMask interactableLayer;
    public Key interactKey = Key.E;

    [Header("Object Holding (HL2 Style)")]
    public float holdDistance = 2.5f;
    public float holdSmoothing = 10f;
    public float throwForce = 15f;
    public float rotationSmoothing = 10f;

    private GameObject heldObject;
    private Rigidbody heldRb;
    private float originalDrag;
    private float originalAngularDrag;

    void Update()
    {
        if (Keyboard.current[interactKey].wasPressedThisFrame)
        {
            if (heldObject != null)
                DropObject();
            else
                TryInteract();
        }

        // Throw held object with left mouse button
        if (Mouse.current.leftButton.wasPressedThisFrame && heldObject != null)
        {
            ThrowObject();
        }

        if (heldObject != null)
        {
            CarryObject();
        }
    }

    void TryInteract()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            Debug.Log("Hit: " + hit.collider.name + " | Tag: " + hit.collider.tag);

            if (hit.collider.CompareTag("Taker"))
            {
                PickUpObject(hit.collider.gameObject);
            }
            else if (hit.collider.CompareTag("Interactable"))
            {
                var interactable = hit.collider.GetComponent<IInteractable>();
                Debug.Log("Interactable found: " + (interactable != null));
                if (interactable != null)
                    interactable.Interaction();
            }
        }
        else
        {
            Debug.Log("Nothing hit");
        }
    }

    void PickUpObject(GameObject obj)
    {
        heldObject = obj;
        heldRb = obj.GetComponent<Rigidbody>();

        if (heldRb == null)
        {
            Debug.LogWarning("Taker object has no Rigidbody! Adding one.");
            heldRb = obj.AddComponent<Rigidbody>();
        }

        // Save and override physics drag so it moves smoothly in hand
        originalDrag = heldRb.linearDamping;
        originalAngularDrag = heldRb.angularDamping;

        heldRb.linearDamping = 10f;
        heldRb.angularDamping = 10f;
        heldRb.useGravity = false;
        heldRb.interpolation = RigidbodyInterpolation.Interpolate;

        Debug.Log("Picked up: " + obj.name);
    }

    void CarryObject()
{
    // Auto-drop if object is too far away (e.g. stuck on wall)
    float distance = (Vector3.Distance(cameraTransform.position, heldObject.transform.position)) + 1;
    if (distance > holdDistance + 2f)
    {
        Debug.Log("Object too far, dropping.");
        DropObject();
        return;
    }

    Vector3 targetPosition = cameraTransform.position + cameraTransform.forward * holdDistance;
    Vector3 direction = targetPosition - heldObject.transform.position;
    heldRb.linearVelocity = direction * holdSmoothing;

    Quaternion targetRotation = Quaternion.LookRotation(cameraTransform.forward, cameraTransform.up);
    heldRb.MoveRotation(Quaternion.Slerp(heldObject.transform.rotation, targetRotation, Time.deltaTime * rotationSmoothing));
}

    void DropObject()
    {
        if (heldRb != null)
        {
            heldRb.linearDamping = originalDrag;
            heldRb.angularDamping = originalAngularDrag;
            heldRb.useGravity = true;
        }

        Debug.Log("Dropped: " + heldObject.name);
        heldObject = null;
        heldRb = null;
    }

    void ThrowObject()
    {
        if (heldRb != null)
        {
            heldRb.linearDamping = originalDrag;
            heldRb.angularDamping = originalAngularDrag;
            heldRb.useGravity = true;
            heldRb.linearVelocity = cameraTransform.forward * throwForce;
        }

        Debug.Log("Threw: " + heldObject.name);
        heldObject = null;
        heldRb = null;
    }
}