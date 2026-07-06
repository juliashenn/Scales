using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

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
        if (!IsServer || puzzleOver) return;
        CheckSolvedCondition();
    }

    private void CheckSolvedCondition()
    {
        float left = leftPan.totalWeight;
        float right = rightPan.totalWeight;
        float diff = Mathf.Abs(left - right);

        // Win: both sides non-zero, perfectly balanced, all weights placed
        bool balanced = diff == 0 && left > 0 && right > 0;
        bool allPlaced = (left + right) >= totalWeight;

        if (balanced && allPlaced && isSolvable)
        {
            puzzleOver = true;
            NotifyResultClientRpc(PuzzleResult.Solved);
        }
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
            NotifyResultClientRpc(PuzzleResult.UnsolvableCorrect);
        }
        else
        {
            NotifyResultClientRpc(PuzzleResult.UnsolvableWrong);
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
    private void NotifyResultClientRpc(PuzzleResult result)
    {
        switch (result)
        {
            case PuzzleResult.Solved:           onPuzzleSolved?.Invoke();        break;
            case PuzzleResult.UnsolvableCorrect: onUnsolvableCorrect?.Invoke();  break;
            case PuzzleResult.UnsolvableWrong:  onUnsolvableWrong?.Invoke();     break;
        }
    }

    private enum PuzzleResult { Solved, UnsolvableCorrect, UnsolvableWrong }
}