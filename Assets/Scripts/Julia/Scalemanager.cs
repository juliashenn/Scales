using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;

public class ScaleManager : NetworkBehaviour
{
    public static ScaleManager Instance;

    [Header("References")]
    [SerializeField] private ScalePlatform leftPan;
    [SerializeField] private ScalePlatform rightPan;

    [Header("Win Condition")]
    [SerializeField] private float unsolvableBalanceThreshold = 10f; // diff < this to claim unsolvable

    [Header("Score Multiplier")]
    [SerializeField] private float scoreMultiplier = 10f;

    [Header("Events")]
    public UnityEvent onPuzzleSolved;
    public UnityEvent onStageCompleted;
    public UnityEvent onUnsolvableCorrect;
    public UnityEvent onUnsolvableWrong;

    private readonly NetworkVariable<int> totalWeight = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<bool> isSolvable = new(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Server-computed score mirrored to clients, since GetScore() relies on
    // ScalePlatform.totalWeight, which is only tracked accurately via the server's own physics.
    private readonly NetworkVariable<float> liveScore = new(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> liveBonusScore = new(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float LiveScore => liveScore.Value;
    public float LiveBonusScore => liveBonusScore.Value;

    // Sum of GetScore() from every stage completed so far this session, for the end screen's total.
    private float cumulativeStageScore;

    private bool puzzleOver;

    private void Awake() => Instance = this;

    // Called by PuzzleGenerator after generating
    public void SetPuzzleData(int total, bool solvable)
    {
        totalWeight.Value = total;
        isSolvable.Value = solvable;
        puzzleOver = false;
    }

    void Update()
    {
        if (!IsServer) return;

        if (PuzzleGenerator.Instance.hasPuzzle() && TimerManager.Instance != null && TimerManager.Instance.IsSessionActive())
        {
            liveScore.Value = GetScore();
            liveBonusScore.Value = GetBonusScore();
        }

        if (puzzleOver || !PuzzleGenerator.Instance.hasPuzzle()) return;
        CheckSolvedCondition();
    }

    private void CheckSolvedCondition()
    {
        if (TimerManager.Instance == null) return;
        if (PuzzleGenerator.Instance.hasPuzzle() && !TimerManager.Instance.IsSessionActive())
        {
            puzzleOver = true;
            NotifyResultClientRpc(PuzzleResult.UnsolvableWrong, cumulativeStageScore + GetScore(), GetBonusScore());
            return;
        }

        //float left = leftPan.totalWeight;
        //float right = rightPan.totalWeight;
        //float diff = Mathf.Abs(left - right);

        //// Win: both sides non-zero, perfectly balanced, all weights placed
        //bool balanced = diff == 0 && left > 0 && right > 0;
        //bool allPlaced = (left + right) >= totalWeight;

        //if (balanced && allPlaced && isSolvable)
        if (leftPan.totalWeight == PuzzleGenerator.Instance?.CurrentTargetSum && leftPan.totalWeight == rightPan.totalWeight)
        {
            if (PuzzleGenerator.Instance.IsFinalStage)
            {
                puzzleOver = true;
                NotifyResultClientRpc(PuzzleResult.Solved, cumulativeStageScore + GetScore(), GetBonusScore());
            }
            else
            {
                int completedStageNumber = PuzzleGenerator.Instance.CurrentStage + 1;
                int totalStages = PuzzleGenerator.Instance.TotalStages;

                cumulativeStageScore += GetScore();
                ResetPans();
                NotifyStageCompleteClientRpc(completedStageNumber, totalStages);
            }
        }
    }

    private void ResetPans()
    {
        leftPan.ResetWeight();
        rightPan.ResetWeight();
    }

    // Players call this to claim the puzzle is unsolvable
    [Rpc(SendTo.Server)]
    public void ClaimUnsolvableServerRpc()
    {
        if (puzzleOver) return;

        float left = leftPan.totalWeight;
        float right = rightPan.totalWeight;
        float diff = Mathf.Abs(left - right);

        bool closeEnough = diff < unsolvableBalanceThreshold && ((left + right) >= totalWeight.Value);

        if (!isSolvable.Value && closeEnough)
        {
            puzzleOver = true;
            NotifyResultClientRpc(PuzzleResult.UnsolvableCorrect, cumulativeStageScore + GetScore(), GetBonusScore());
        }
        else
        {
            puzzleOver = true;
            NotifyResultClientRpc(PuzzleResult.UnsolvableWrong, cumulativeStageScore + GetScore(), GetBonusScore());
        }
    }

    // -------------------------------------------------------
    // Scoring: Total weight minus difference, with remaining time as bonus
    // -------------------------------------------------------
    public float GetScore()
    {
        float left = leftPan.totalWeight;
        float right = rightPan.totalWeight;
        float diff = Mathf.Abs(left - right) * 2; // Penalize difference more heavily
        float score = left + right - diff;
        score = Mathf.Clamp(score, 0, totalWeight.Value * 2); // Ensure score is non-negative and does not exceed max possible
        return score * scoreMultiplier;
    }

    public float GetBonusScore()
    {
        return TimerManager.Instance.GetRemainingTime(); // Remaining time as bonus
    }

    public float GetTotalScore()
    {
        return GetScore() + GetBonusScore();
    }

    [Rpc(SendTo.Everyone)]
    private void NotifyResultClientRpc(PuzzleResult result, float score, float bonusScore)
    {
        TimerManager.Instance?.EndSession();
        print("notifying");
        switch (result)
        {
            case PuzzleResult.Solved:
                print("solved");
                onPuzzleSolved?.Invoke();
                EndScreen.Instance?.Won(score, bonusScore);
                break;
            case PuzzleResult.UnsolvableCorrect:
                onUnsolvableCorrect?.Invoke();
                EndScreen.Instance?.Won(score, bonusScore);
                break;
            case PuzzleResult.UnsolvableWrong:
                print("unsolvable wrong = lost");
                onUnsolvableWrong?.Invoke();
                EndScreen.Instance?.Lost(score, bonusScore);
                break;
        }
    }

    [Rpc(SendTo.Everyone)]
    private void NotifyStageCompleteClientRpc(int completedStageNumber, int totalStages)
    {
        // Runs on the server and every client so each machine's local ScalePlatform
        // totals (which are only computed from local physics, not networked) get
        // zeroed out too, not just the server's.
        ResetPans();
        PuzzleGenerator.Instance.ShowStageCompletePopup(completedStageNumber, totalStages);
        onStageCompleted?.Invoke();
    }

    private enum PuzzleResult { Solved, UnsolvableCorrect, UnsolvableWrong }

    [Rpc(SendTo.Server)]
    public void TimeExpiredServerRpc()
    {
        if (puzzleOver) return;
        puzzleOver = true;
        NotifyResultClientRpc(PuzzleResult.UnsolvableWrong, cumulativeStageScore + GetScore(), GetBonusScore());
    }

    private Dictionary<ulong, bool> readyStates = new Dictionary<ulong, bool>();

    // ready check to start
    [Rpc(SendTo.Server)]
    public void ReadyCheckServerRpc(ulong clientId, bool isReady)
    {
        // Track ready states server-side instead of reading NetworkVariable
        readyStates[clientId] = isReady;

        int readyCount = 0;
        foreach (var kvp in readyStates)
            if (kvp.Value) readyCount++;

        int totalCount = NetworkManager.Singleton.ConnectedClientsList.Count;
        if (readyCount == totalCount && totalCount == 3)
            StartGameClientRpc();
    }

    [Rpc(SendTo.Everyone)]
    public void StartGameClientRpc()
    {
        LobbyListUI.Instance?.HideBlackout();
        ServerStartSession();
    }
   
    public void ServerStartSession()
    {
        if (!IsServer) return;
        cumulativeStageScore = 0f;
        TimerManager.Instance?.StartSession();
        PuzzleGenerator generator = PuzzleGenerator.Instance;
        generator?.ResetStages();
        generator?.GeneratePuzzle();
    }
}