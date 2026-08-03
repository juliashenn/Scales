using System;
using UnityEngine;

[Serializable]
public struct PuzzleStage
{
    public int objectCount;      // must be even
    public int targetSum;
    [Range(0f, 0f)] public float unsolvableChance;
}
