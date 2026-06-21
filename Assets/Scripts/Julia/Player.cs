using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public enum PlayerRole
{
    None = 0,
    RoleA = 1,
    RoleB = 2,
    RoleC = 3
}

public class Player : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    public float speed = 5f;

    void Start()
    {
        if (IsOwner)
        {
            GetComponent<Renderer>().material.color = Color.red;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(horizontal, vertical, 0f).normalized;
        transform.Translate(direction * speed * Time.deltaTime);   
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            PlayerName.Value = NameManager.LocalPlayerName;
        }

        PlayerName.OnValueChanged += (oldVal, newVal) => LobbyListUI.Instance?.RefreshList();
        LobbyListUI.Instance?.RefreshList();
    }

    public override void OnNetworkDespawn()
    {
        LobbyListUI.Instance?.RefreshList();
    }
}
