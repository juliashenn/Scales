using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

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

    // Update is called once per frame
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

        // Notify the lobby UI to refresh when this or any name changes
        PlayerName.OnValueChanged += (oldVal, newVal) => LobbyListUI.Instance?.RefreshList();
        LobbyListUI.Instance?.RefreshList();
    }

    public override void OnNetworkDespawn()
    {
        LobbyListUI.Instance?.RefreshList();
    }
}
