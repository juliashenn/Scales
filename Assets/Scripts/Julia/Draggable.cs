using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public enum WeightShape
{
    Square, Circle, Triangle, Star, Heart, Pentagon
}

public class Draggable : NetworkBehaviour
{
    public NetworkVariable<float> weight = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<WeightShape> shape = new NetworkVariable<WeightShape>(
        WeightShape.Square,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField] private bool useXZPlane = true;
    [SerializeField] public PlayerRole requiredRole;
    [SerializeField] private float dragThreshold = 0.001f;
    [SerializeField] private LayerMask draggableLayer;

    [Header("Sound FX")]
    public AudioClip pickupClip;
    public AudioClip placeClip;
    public AudioSource audioSource;

    [Header("Shapes")]
    [SerializeField] private GameObject square;
    [SerializeField] private GameObject circle;
    [SerializeField] private GameObject triangle;
    [SerializeField] private GameObject star;
    [SerializeField] private GameObject heart;
    [SerializeField] private GameObject pentagon;

    private Dictionary<WeightShape, GameObject> shapePrefabs;
    private NetworkObject selected;
    private Vector3 offset;
    private Plane dragPlane;
    private Vector3 lastSentPosition;

    private Camera Cam
    {
        get
        {
            var c = Camera.main;
            return c != null && c.gameObject.activeInHierarchy ? c : null;
        }
    }

    private void Awake()
    {
        shapePrefabs = new Dictionary<WeightShape, GameObject>
        {
            { WeightShape.Square,   square   },
            { WeightShape.Circle,   circle   },
            { WeightShape.Triangle, triangle },
            { WeightShape.Star,     star     },
            { WeightShape.Heart,    heart    },
            { WeightShape.Pentagon, pentagon }
        };

        foreach (var kvp in shapePrefabs)
            kvp.Value.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        weight.OnValueChanged += (oldVal, newVal) => ApplyScale(newVal);
        shape.OnValueChanged += (oldVal, newVal) => ApplyShape(newVal);

        ApplyScale(weight.Value);
        ApplyShape(shape.Value);

        if (audioSource != null) audioSource.volume = 0.1f;
    }

    public void ServerSetup(float newWeight, WeightShape newShape)
    {
        if (!IsServer) return;
        weight.Value = newWeight;
        shape.Value = newShape;
    }

    void ApplyScale(float w)
    {
        float scale = 0.5f + (w * 0.1f);
        transform.localScale = Vector3.one * scale;
    }

    void ApplyShape(WeightShape newShape)
    {
        foreach (var kvp in shapePrefabs)
            kvp.Value.SetActive(kvp.Key == newShape);
    }

    void Update()
    {
        if (!IsClient) return;
        if (Cam == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryPick();

        if (Mouse.current.leftButton.isPressed && selected != null)
        {
            if (TryGetMouseWorld(out Vector3 worldPoint))
            {
                Vector3 targetPos = worldPoint + offset;
                if (Vector3.Distance(targetPos, lastSentPosition) > dragThreshold)
                {
                    // Predict locally immediately — don't wait for server round trip
                    selected.transform.position = targetPos;
                    DragServerRpc(new NetworkObjectReference(selected), targetPos);
                    lastSentPosition = targetPos;
                }
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (selected != null)
                audioSource?.PlayOneShot(placeClip);
            selected = null;
            lastSentPosition = Vector3.zero;
        }
    }

    void TryPick()
    {
        Ray ray = Cam.ScreenPointToRay(Mouse.current.position.ReadValue());
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

        audioSource?.PlayOneShot(pickupClip);

        if (TryGetMouseWorld(out Vector3 worldPoint))
        {
            offset = selected.transform.position - worldPoint;
            lastSentPosition = selected.transform.position;
        }
    }

    bool TryGetMouseWorld(out Vector3 worldPoint)
    {
        Ray ray = Cam.ScreenPointToRay(Mouse.current.position.ReadValue());
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