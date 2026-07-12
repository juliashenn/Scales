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

    private int totalWeight;
    private bool isSolvable;
    private bool puzzleOver;

    private void Awake() => Instance = this;

    // Called by PuzzleGenerator after generating
    public void SetPuzzleData(int total, bool solvable)
    {
        totalWeight = total;
        isSolvable = solvable;
        puzzleOver = false;
    }

    void Update()
    {
        if (!IsServer || puzzleOver || !PuzzleGenerator.Instance.hasPuzzle()) return;
        CheckSolvedCondition();
    }

    private void CheckSolvedCondition()
    {
        if (TimerManager.Instance == null) return;
        if (PuzzleGenerator.Instance.hasPuzzle() && !TimerManager.Instance.IsSessionActive())
        {
            puzzleOver = true;
            NotifyResultClientRpc(PuzzleResult.UnsolvableWrong, GetScore(), GetBonusScore());
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
                float solvedScore = PuzzleGenerator.Instance.CurrentTargetSum * 2f * scoreMultiplier;
                NotifyResultClientRpc(PuzzleResult.Solved, solvedScore, GetBonusScore());
            }
            else
            {
                ResetPans();
                PuzzleGenerator.Instance.AdvanceStage();
                NotifyStageCompleteClientRpc();
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

        bool closeEnough = diff < unsolvableBalanceThreshold && ((left + right) >= totalWeight);

        if (!isSolvable && closeEnough)
        {
            puzzleOver = true;
            NotifyResultClientRpc(PuzzleResult.UnsolvableCorrect, GetScore(), GetBonusScore());
        }
        else
        {
            puzzleOver = true;
            NotifyResultClientRpc(PuzzleResult.UnsolvableWrong, GetScore(), GetBonusScore());
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
        score = Mathf.Clamp(score, 0, totalWeight * 2); // Ensure score is non-negative and does not exceed max possible
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
    private void NotifyStageCompleteClientRpc()
    {
        onStageCompleted?.Invoke();
    }

    private enum PuzzleResult { Solved, UnsolvableCorrect, UnsolvableWrong }

    [Rpc(SendTo.Server)]
    public void TimeExpiredServerRpc()
    {
        if (puzzleOver) return;
        puzzleOver = true;
        NotifyResultClientRpc(PuzzleResult.UnsolvableWrong, GetScore(), GetBonusScore());
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
        TimerManager.Instance?.StartSession();
        PuzzleGenerator generator = PuzzleGenerator.Instance;
        generator?.ResetStages();
        generator?.GeneratePuzzle();
    }
}