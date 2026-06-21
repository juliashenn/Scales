
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class Spawner : NetworkBehaviour
{
    [SerializeField] private NetworkObject prefab;

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            SpawnServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void SpawnServerRpc()
    {
        var obj = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        obj.Spawn(); 
    }
}