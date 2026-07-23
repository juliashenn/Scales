using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PuzzleGenerator : NetworkBehaviour
{
    public static PuzzleGenerator Instance;

    [Header("Puzzle Settings")]
    [SerializeField] private PuzzleStage[] stages;

    public int CurrentTargetSum { get; private set; }
    public int CurrentStage { get; private set; } = 0;
    public bool IsFinalStage => CurrentStage >= LastStageIndex;
    public int TotalStages => stages.Length;

    [Header("Stage Complete Popup")]
    [SerializeField] private float stageCompletePopupDuration = 2f;

    [Header("Spawn Settings")]
    [SerializeField] private NetworkObject weightPrefabA; // for RoleA
    [SerializeField] private NetworkObject weightPrefabB; // for RoleB
    [SerializeField] private Transform spawnArea;         // center of spawn zone
    [SerializeField] private float spawnSpacing = 0.3f;

    [Header("References")]
    [SerializeField] private ScaleManager scaleManager;
    [SerializeField] private StageCompleteText stageCompleteText;

    // Tracked so ScaleManager can reference total
    public int TotalWeight { get; private set; }
    public bool IsSolvable { get; private set; }

    private bool spawned = false;
    private readonly List<NetworkObject> spawnedWeights = new List<NetworkObject>();

    private int LastStageIndex => stages.Length - 1;

    private void Awake()
    {
        Instance = this;
    }

    public bool hasPuzzle()
    {
        return spawned;
    }

    public override void OnNetworkSpawn()
    {
        //if (IsServer)
        //    GeneratePuzzle();
    }

    // Resets progress back to the first stage. Call before starting a new game session.
    public void ResetStages()
    {
        CurrentStage = 0;
    }

    // Runs on every peer (called from ScaleManager's stage-complete Rpc) to display
    // "Stage X/Total cleared!" briefly using each machine's own local text reference,
    // then advances to the next stage once the popup disappears. AdvanceStage() is
    // server-only internally, so the delayed advance only actually does anything on the server.
    public void ShowStageCompletePopup(int completedStageNumber, int totalStages)
    {
        if (stageCompleteText != null)
        {
            stageCompleteText.SetText(completedStageNumber, totalStages);
            stageCompleteText.Show();
        }

        StopAllCoroutines();
        StartCoroutine(StageCompleteSequence());
    }

    private IEnumerator StageCompleteSequence()
    {
        yield return new WaitForSeconds(stageCompletePopupDuration);

        if (stageCompleteText != null)
            stageCompleteText.Hide();

        AdvanceStage();
    }

    // Advances to the next stage (if any remain) and generates its puzzle.
    public void AdvanceStage()
    {
        if (!IsServer) return;
        if (IsFinalStage) return;

        CurrentStage++;
        GeneratePuzzle();
    }

    [ContextMenu("Generate Puzzle (Server Only)")]
    public void GeneratePuzzle()
    {
        if (!IsServer) return;
        if (stages == null || stages.Length == 0)
        {
            Debug.LogError("[PuzzleGenerator] No stages configured.");
            return;
        }

        spawned = true;
        DespawnCurrentWeights();

        PuzzleStage stage = stages[Mathf.Clamp(CurrentStage, 0, stages.Length - 1)];

        int objectCount = Mathf.Max(2, stage.objectCount % 2 == 0 ? stage.objectCount : stage.objectCount + 1);
        CurrentTargetSum = Mathf.Max(objectCount + 1, stage.targetSum);

        int half = objectCount / 2;
        List<int> left = GeneratePartition(CurrentTargetSum, half);
        List<int> right = GeneratePartition(CurrentTargetSum, half);

        List<int> combined = new List<int>(left);
        combined.AddRange(right);

        IsSolvable = UnityEngine.Random.value > stage.unsolvableChance;
        if (!IsSolvable)
            combined[0] += UnityEngine.Random.Range(1, 11);

        TotalWeight = 0;
        foreach (int w in combined) TotalWeight += w;

        Shuffle(combined);
        SpawnWeights(combined, half);

        scaleManager?.SetPuzzleData(TotalWeight, IsSolvable);
    }

    private void DespawnCurrentWeights()
    {
        foreach (NetworkObject obj in spawnedWeights)
        {
            if (obj == null) continue;
            if (obj.IsSpawned) obj.Despawn();
            else Destroy(obj.gameObject);
        }
        spawnedWeights.Clear();
    }

    private List<int> GeneratePartition(int sum, int count)
    {
        List<int> partition = new List<int>();
        int remaining = sum;

        for (int i = 0; i < count - 1; i++)
        {
            int rand = UnityEngine.Random.Range(1, sum / count + 1);
            partition.Add(rand);
            remaining -= rand;
        }

        partition.Add(Mathf.Max(1, remaining));
        return partition;
    }

    private void SpawnWeights(List<int> weights, int half)
    {

        // Randomize the order first
        weights = weights.OrderBy(x => UnityEngine.Random.value).ToList();

        float startX = -(weights.Count - 1) * spawnSpacing * 0.5f; // center the row

        List<WeightShape> shapePoolA = Enum.GetValues(typeof(WeightShape)).Cast<WeightShape>().ToList();
        List<WeightShape> shapePoolB = Enum.GetValues(typeof(WeightShape)).Cast<WeightShape>().ToList();

        // Shuffle both independently
        shapePoolA = shapePoolA.OrderBy(x => UnityEngine.Random.value).ToList();
        shapePoolB = shapePoolB.OrderBy(x => UnityEngine.Random.value).ToList();

        for (int i = 0; i < weights.Count; i++)
        {
            // First half belongs to RoleA, second half to RoleB
            bool isRoleA = i < half;
            NetworkObject prefab = isRoleA ? weightPrefabA : weightPrefabB;

            // Spawn in a horizontal row
            Vector3 spawnPos = spawnArea != null
                ? spawnArea.position + new Vector3(startX + (i * spawnSpacing), 0f, 0f)
                : new Vector3(startX + (i * spawnSpacing), 0f, 0f);

            NetworkObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);

            obj.Spawn();
            spawnedWeights.Add(obj);

            if (obj.TryGetComponent(out Draggable draggable))
            {
                //draggable.weight.Value = weights[i];
                WeightShape chosenShape;
                if (isRoleA)
                {
                    chosenShape = shapePoolA[i];
                }
                else
                {
                    chosenShape = shapePoolB[i - half];
                }
                draggable.ServerSetup(weights[i], chosenShape);
            }
                
           

            float scale = 0.5f + (weights[i] * 0.1f);
            obj.transform.localScale = Vector3.one * scale;
        }
    }

    private void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
