using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class RoleManager : NetworkBehaviour
{
    public static RoleManager Instance;
    private Dictionary<ulong, PlayerRole> assignedRoles = new();
    private List<ulong> pendingClients = new List<ulong>();

    void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        NetworkManager.Singleton.OnClientConnectedCallback += AssignRole;
        NetworkManager.Singleton.OnClientDisconnectCallback += RemoveRole;

        // Handle any clients that connected before we spawned
        foreach (ulong clientId in pendingClients)
            AssignRole(clientId);
        pendingClients.Clear();
    }
    public PlayerRole GetRole(ulong clientId)
    {
        return assignedRoles.TryGetValue(clientId, out var role) ? role : PlayerRole.None;
    }

    void AssignRole(ulong clientId)
    {

        // If not spawned yet, queue for later
        if (!IsSpawned)
        {
            pendingClients.Add(clientId);
            return;
        }

        if (assignedRoles.ContainsKey(clientId)) return;

        PlayerRole role = assignedRoles.Count switch
        {
            0 => PlayerRole.RoleA,
            1 => PlayerRole.RoleB,
            2 => PlayerRole.RoleC,
            _ => PlayerRole.None 
        };

        assignedRoles[clientId] = role;

        SetRoleClientRpc((int)role, RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    void RemoveRole(ulong clientId)
    {
        assignedRoles.Remove(clientId);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    void SetRoleClientRpc(int role, RpcParams rpcParams = default)
    {
        PlayerRoleHolder.SetRole((PlayerRole)role);
    }

    public void CycleRoles()
    {
        if (assignedRoles.Count < 3) return;

        var clientIds = assignedRoles.Keys.ToList();
        var currentRoles = clientIds.ToDictionary(id => id, id => assignedRoles[id]);

        for (int i = 0; i < clientIds.Count; i++)
        {
            ulong clientId = clientIds[i];
            PlayerRole newRole = currentRoles[clientIds[(i + 1) % clientIds.Count]];

            assignedRoles[clientId] = newRole;
            SetRoleClientRpc((int)newRole, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }
    }
}

public static class PlayerRoleHolder
{
    public static PlayerRole LocalRole = PlayerRole.None;
    public static event Action<PlayerRole> OnRoleAssigned;

    public static void SetRole(PlayerRole role)
    {
        LocalRole = role;
        OnRoleAssigned?.Invoke(role);
    }

    // Clears role without notifying subscribers
    public static void ResetRole()
    {
        LocalRole = PlayerRole.None;
    }
}