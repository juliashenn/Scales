using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Draggable : NetworkBehaviour
{
    public float weight;
    [SerializeField] private bool useXZPlane = true;
    [SerializeField] private Camera cam;
    [SerializeField] private PlayerRole requiredRole;
    [SerializeField] private float dragThreshold = 0.001f;
    [SerializeField] private LayerMask draggableLayer;

    private NetworkObject selected;
    private Vector3 offset;
    private Plane dragPlane;
    private Vector3 lastSentPosition;

    public override void OnNetworkSpawn()
    {
        if (cam == null)
            cam = Camera.main;
    }

    void Update()
    {
        if (!IsClient) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryPick();

        if (Mouse.current.leftButton.isPressed && selected != null)
        {
            if (TryGetMouseWorld(out Vector3 worldPoint))
            {
                Vector3 targetPos = worldPoint + offset;

                if (Vector3.Distance(targetPos, lastSentPosition) > dragThreshold)
                {
                    DragServerRpc(new NetworkObjectReference(selected), targetPos);
                    lastSentPosition = targetPos;
                }
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            selected = null;
            lastSentPosition = Vector3.zero;
        }
    }

    void TryPick()
    {
        if (cam == null) cam = Camera.main;

        Vector2 mouse = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mouse);

        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, draggableLayer)) return;

        var netObj = hit.collider.GetComponentInParent<NetworkObject>();
        if (netObj == null) return;

        var draggable = netObj.GetComponent<Draggable>();
        if (draggable == null) return;

        if (draggable.requiredRole != PlayerRoleHolder.LocalRole)
        {
            Debug.Log("wrong role: " + PlayerRoleHolder.LocalRole);
            return;
        }

        selected = netObj;
        dragPlane = useXZPlane
            ? new Plane(Vector3.up, selected.transform.position)
            : new Plane(Vector3.forward, selected.transform.position);

        if (TryGetMouseWorld(out Vector3 worldPoint))
        {
            offset = selected.transform.position - worldPoint;
            lastSentPosition = selected.transform.position;
        }
    }

    bool TryGetMouseWorld(out Vector3 worldPoint)
    {
        Vector2 mouse = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mouse);

        if (dragPlane.Raycast(ray, out float distance))
        {
            worldPoint = ray.GetPoint(distance);
            return true;
        }

        worldPoint = Vector3.zero;
        return false;
    }

    [Rpc(SendTo.Server)]
    void DragServerRpc(NetworkObjectReference objRef, Vector3 targetPos, RpcParams rpcParams = default)
    {
        if (!objRef.TryGet(out NetworkObject obj)) return;

        var draggable = obj.GetComponent<Draggable>();
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (RoleManager.Instance.GetRole(senderId) != draggable.requiredRole) return;

        obj.transform.position = targetPos;
    }
}