using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Draggable : NetworkBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private PlayerRole requiredRole;
    private NetworkObject selected;
    private Vector3 offset;

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
            DragServerRpc(new NetworkObjectReference(selected), GetMouseWorld() + offset);

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            selected = null;
    }

    void TryPick()
    {
        Vector2 mouse = Mouse.current.position.ReadValue();
        Vector2 world = cam.ScreenToWorldPoint(mouse);
        Collider2D hit = Physics2D.OverlapPoint(world);
        if (hit == null) return;
        Debug.Log("Hit");
        var netObj = hit.GetComponentInParent<NetworkObject>();
        if (netObj == null) return;

        var draggable = netObj.GetComponent<Draggable>();
        if (draggable == null) return;

        if (draggable.requiredRole != PlayerRoleHolder.LocalRole)
        {
            Debug.Log("wrong role");
            Debug.Log(PlayerRoleHolder.LocalRole);
            return;
        }

        selected = netObj;
        offset = selected.transform.position - (Vector3)world;
    }

    Vector3 GetMouseWorld()
    {
        if (cam == null || !cam.gameObject.activeInHierarchy) cam = Camera.main;
        Vector2 mouse = Mouse.current.position.ReadValue();
        return cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -cam.transform.position.z));
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