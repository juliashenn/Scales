using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PuzzleGenerator : NetworkBehaviour
{
    [Header("Puzzle Settings")]
    [SerializeField] private int objectCount = 6;       // must be even
    [SerializeField] private int targetSum = 20;
    [SerializeField] private float unsolvableChance = 0.3f;

    [Header("Spawn Settings")]
    [SerializeField] private NetworkObject weightPrefabA; // for RoleA
    [SerializeField] private NetworkObject weightPrefabB; // for RoleB
    [SerializeField] private Transform spawnArea;         // center of spawn zone
    [SerializeField] private float spawnRadius = 3f;

    [Header("References")]
    [SerializeField] private ScaleManager scaleManager;

    // Tracked so ScaleManager can reference total
    public int TotalWeight { get; private set; }
    public bool IsSolvable { get; private set; }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            GeneratePuzzle();
    }

    [ContextMenu("Generate Puzzle (Server Only)")]
    public void GeneratePuzzle()
    {
        if (!IsServer) return;

        objectCount = Mathf.Max(2, objectCount % 2 == 0 ? objectCount : objectCount + 1);
        targetSum = Mathf.Max(objectCount + 1, targetSum);

        int half = objectCount / 2;
        List<int> left = GeneratePartition(targetSum, half);
        List<int> right = GeneratePartition(targetSum, half);

        List<int> combined = new List<int>(left);
        combined.AddRange(right);

        IsSolvable = Random.value > unsolvableChance;
        if (!IsSolvable)
            combined[0] += Random.Range(1, 11);

        TotalWeight = 0;
        foreach (int w in combined) TotalWeight += w;

        Shuffle(combined);
        SpawnWeights(combined, half);

        scaleManager?.SetPuzzleData(TotalWeight, IsSolvable);
    }

    private List<int> GeneratePartition(int sum, int count)
    {
        List<int> partition = new List<int>();
        int remaining = sum;

        for (int i = 0; i < count - 1; i++)
        {
            int rand = Random.Range(1, sum / count + 1);
            partition.Add(rand);
            remaining -= rand;
        }

        partition.Add(Mathf.Max(1, remaining));
        return partition;
    }

    private void SpawnWeights(List<int> weights, int half)
    {
        for (int i = 0; i < weights.Count; i++)
        {
            // First half belongs to RoleA, second half to RoleB
            NetworkObject prefab = i < half ? weightPrefabA : weightPrefabB;

            Vector3 spawnPos = spawnArea != null
                ? spawnArea.position + (Vector3)(Random.insideUnitCircle * spawnRadius)
                : Random.insideUnitCircle * spawnRadius;

            NetworkObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);

            // Assign weight on the Draggable component
            if (obj.TryGetComponent(out Draggable draggable))
                draggable.weight = weights[i];

            // Visual scale: 1 + (weight * 0.1) per the design doc
            float scale = 1f + (weights[i] * 0.1f);
            obj.transform.localScale = Vector3.one * scale;

            obj.Spawn();

            // Sync weight and scale to all clients
            SyncWeightClientRpc(new NetworkObjectReference(obj), weights[i], scale);
        }
    }


    [Rpc(SendTo.NotServer)]
    private void SyncWeightClientRpc(NetworkObjectReference objRef, int weight, float scale)
    {
        if (!objRef.TryGet(out NetworkObject obj)) return;

        if (obj.TryGetComponent(out Draggable draggable))
            draggable.weight = weight;

        obj.transform.localScale = Vector3.one * scale;
    }

    private void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
