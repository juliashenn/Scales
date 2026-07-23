using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum GameRole { None, Holder, Caller }

public class PartitionGameManager : MonoBehaviour
{
    public static PartitionGameManager Instance;

    [Header("Role & State")]
    public GameRole currentRole = GameRole.None;
    public bool isGameActive = false;

    [Header("Game Settings")]
    public int weightCount = 12; // 12 weights total

    [Header("3D Shape Prefabs (3D Scale)")]
    public GameObject tier1Prefab; // 1-4 kg (Triangle)
    public GameObject tier2Prefab; // 5-8 kg (Square)
    public GameObject tier3Prefab; // 9-12 kg (Pentagon)
    public GameObject tier4Prefab; // 13-16 kg (Circle)

    [Header("2D Shape Sprites (Staging Box UI)")]
    public Sprite tier1Sprite; // Triangle Sprite
    public Sprite tier2Sprite; // Square Sprite
    public Sprite tier3Sprite; // Pentagon Sprite
    public Sprite tier4Sprite; // Circle Sprite

    [Header("Scale 3D Container References")]
    public GameObject scale3DContainer;
    public Transform scaleBeam;
    public Transform leftPan;
    public Transform rightPan;

    [Header("UI Canvas Groups")]
    public GameObject menuCanvas;
    public GameObject gameCanvas;
    public GameObject gameOverCanvas;

    [Header("Holder UI System")]
    public GameObject holderControlArea;
    public Transform stagingBoxContainer;
    public GameObject uiWeightItemPrefab;

    [Header("Caller UI System")]
    public GameObject callerControlArea;

    [Header("UI Text References")]
    public TextMeshProUGUI roleTitleText;
    public TextMeshProUGUI nextWeightText;
    public TextMeshProUGUI scaleTotalsText;
    public TextMeshProUGUI botPromptText;
    public TextMeshProUGUI finalScoreText;

    // Internal Game State
    private List<int> currentWeights = new List<int>();
    private float leftTotal = 0f;
    private float rightTotal = 0f;
    private int leftCount = 0;
    private int rightCount = 0;

    // Sprite Tallies per side [0: Triangle, 1: Square, 2: Pentagon, 3: Circle]
    private int[] leftPanSpriteCounts = new int[4];
    private int[] rightPanSpriteCounts = new int[4];

    private bool isSolvable = false;
    private List<GameObject> spawned3DObjects = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ShowMainMenu();
    }

    private void Update()
    {
        if (!isGameActive) return;

        // Smoothly tilt scale beam based on balance difference
        if (scaleBeam != null)
        {
            float diff = Mathf.Clamp(leftTotal - rightTotal, -15f, 15f);
            Quaternion targetRot = Quaternion.Euler(0, 0, diff * 1.5f);
            scaleBeam.localRotation = Quaternion.Slerp(scaleBeam.localRotation, targetRot, Time.deltaTime * 5f);
        }
    }

    public void ShowMainMenu()
    {
        isGameActive = false;
        currentRole = GameRole.None;

        // Purge state and lingering GameObjects
        ClearAllWeights();

        if (menuCanvas) menuCanvas.SetActive(true);
        if (gameCanvas) gameCanvas.SetActive(false);
        if (gameOverCanvas) gameOverCanvas.SetActive(false);
        if (scale3DContainer) scale3DContainer.SetActive(false);

        if (scaleBeam) scaleBeam.localRotation = Quaternion.identity;
    }

    public void StartHolderGame()
    {
        currentRole = GameRole.Holder;
        InitializeGame();
    }

    public void StartCallerGame()
    {
        currentRole = GameRole.Caller;
        InitializeGame();
    }

    public void RestartCurrentMode()
    {
        if (currentRole == GameRole.None) currentRole = GameRole.Holder;
        InitializeGame();
    }

    public void InitializeGame()
    {
        // 1. Purge all past objects and reset state
        ClearAllWeights();

        leftTotal = 0f;
        rightTotal = 0f;
        leftCount = 0;
        rightCount = 0;

        System.Array.Clear(leftPanSpriteCounts, 0, 4);
        System.Array.Clear(rightPanSpriteCounts, 0, 4);

        if (scaleBeam) scaleBeam.localRotation = Quaternion.identity;

        // 2. Activate proper Canvas UI states
        if (menuCanvas) menuCanvas.SetActive(false);
        if (gameCanvas) gameCanvas.SetActive(true);
        if (gameOverCanvas) gameOverCanvas.SetActive(false);

        // Scale 3D container visible ONLY for Caller
        if (scale3DContainer) scale3DContainer.SetActive(currentRole == GameRole.Caller);

        if (holderControlArea) holderControlArea.SetActive(currentRole == GameRole.Holder);
        if (callerControlArea) callerControlArea.SetActive(currentRole == GameRole.Caller);

        // 3. Generate new weights
        currentWeights.Clear();
        for (int i = 0; i < weightCount; i++)
        {
            currentWeights.Add(Random.Range(1, 17));
        }

        isSolvable = CheckIfSolvable(currentWeights);

        if (currentRole == GameRole.Holder)
        {
            SpawnStagingBoxUI();
        }

        if (roleTitleText) roleTitleText.text = $"ROLE: {currentRole.ToString().ToUpper()}";

        isGameActive = true;
        UpdateHUD();
    }

    private bool CheckIfSolvable(List<int> weights)
    {
        int sum = 0;
        foreach (int w in weights) sum += w;
        if (sum % 2 != 0) return false;

        int target = sum / 2;
        bool[] dp = new bool[target + 1];
        dp[0] = true;

        foreach (int w in weights)
        {
            for (int j = target; j >= w; j--)
            {
                if (dp[j - w]) dp[j] = true;
            }
        }
        return dp[target];
    }

    private void SpawnStagingBoxUI()
    {
        if (stagingBoxContainer == null || uiWeightItemPrefab == null) return;

        foreach (int weight in currentWeights)
        {
            GameObject item = Instantiate(uiWeightItemPrefab, stagingBoxContainer);
            DraggableWeightItem dragScript = item.GetComponent<DraggableWeightItem>();
            if (dragScript != null)
            {
                Sprite shapeSprite = GetSpriteForWeight(weight);
                dragScript.Setup(weight, shapeSprite);
            }
        }
    }

    public Sprite GetSpriteForWeight(int weight)
    {
        if (weight <= 4) return tier1Sprite;
        if (weight <= 8) return tier2Sprite;
        if (weight <= 12) return tier3Sprite;
        return tier4Sprite;
    }

    private int GetTierIndex(int weight)
    {
        if (weight <= 4) return 0; // Triangle
        if (weight <= 8) return 1; // Square
        if (weight <= 12) return 2; // Pentagon
        return 3;                  // Circle
    }

    private string GetShapeName(int weight)
    {
        if (weight <= 4) return "▲ Triangle";
        if (weight <= 8) return "■ Square";
        if (weight <= 12) return "⬟ Pentagon";
        return "⬢ Circle";
    }

    public void PlaceWeightValue(int weight, bool isLeft)
    {
        if (!isGameActive) return;

        int tierIndex = GetTierIndex(weight);

        if (isLeft)
        {
            leftTotal += weight;
            leftPanSpriteCounts[tierIndex]++;
            Spawn3DShapeOnPan(weight, leftPan, leftCount);
            leftCount++;
        }
        else
        {
            rightTotal += weight;
            rightPanSpriteCounts[tierIndex]++;
            Spawn3DShapeOnPan(weight, rightPan, rightCount);
            rightCount++;
        }

        currentWeights.Remove(weight);
        UpdateHUD();

        if (currentWeights.Count == 0)
        {
            EndGame(claimedUnsolvable: false);
        }
    }

    public void OnCallerPlaceLeft()
    {
        if (currentWeights.Count > 0) PlaceWeightValue(currentWeights[0], true);
    }

    public void OnCallerPlaceRight()
    {
        if (currentWeights.Count > 0) PlaceWeightValue(currentWeights[0], false);
    }

    private void Spawn3DShapeOnPan(int weight, Transform panTarget, int stackIndex)
    {
        if (panTarget == null) return;

        GameObject prefab = tier1Prefab;
        if (weight > 12) prefab = tier4Prefab;
        else if (weight > 8) prefab = tier3Prefab;
        else if (weight > 4) prefab = tier2Prefab;

        if (prefab == null) return;

        Vector3 offset = Vector3.up * (0.15f + stackIndex * 0.4f);
        GameObject spawned = Instantiate(prefab, panTarget.position + offset, Quaternion.identity, panTarget);
        
        float scaleVal = Mathf.Lerp(0.35f, 0.7f, weight / 16f);
        spawned.transform.localScale = Vector3.one * scaleVal;

        spawned3DObjects.Add(spawned);
    }

    public void OnClaimUnsolvable()
    {
        if (!isGameActive) return;
        EndGame(claimedUnsolvable: true);
    }

    public void UpdateHUD()
    {
        // 1. Display Shape Tallies
        if (scaleTotalsText)
        {
            string leftStr = BuildShapeCountString(leftPanSpriteCounts);
            string rightStr = BuildShapeCountString(rightPanSpriteCounts);
            scaleTotalsText.text = $"<b>LEFT:</b> {leftStr}\n<b>RIGHT:</b> {rightStr}";
        }

        // 2. Display Next Shape in Queue
        if (nextWeightText)
        {
            if (currentWeights.Count > 0)
            {
                string nextShape = GetShapeName(currentWeights[0]);
                nextWeightText.text = $"NEXT IN LINE: <b>{nextShape}</b> ({currentWeights.Count} left)";
            }
            else
            {
                nextWeightText.text = "<b>All shapes placed!</b>";
            }
        }

        // 3. Dynamic Scale Shift Feedback
        if (botPromptText)
        {
            float diff = leftTotal - rightTotal;
            float absDiff = Mathf.Abs(diff);

            if (absDiff == 0)
            {
                botPromptText.text = "Scale State: <i>The scale is currently perfectly balanced.</i>";
            }
            else
            {
                string direction = (diff > 0) ? "LEFT" : "RIGHT";
                string magnitude = (absDiff <= 4) ? "slightly" : ((absDiff <= 8) ? "by a moderate margin" : "a lot");
                botPromptText.text = $"Scale State: <i>The scale has shifted <b>{magnitude}</b> to the <b>{direction}</b>.</i>";
            }
        }
    }

    private string BuildShapeCountString(int[] counts)
    {
        List<string> parts = new List<string>();
        string[] shapeNames = { "Triangles", "Squares", "Pentagons", "Circles" };

        int total = 0;
        for (int i = 0; i < 4; i++)
        {
            if (counts[i] > 0)
            {
                parts.Add($"{counts[i]} {shapeNames[i]}");
                total += counts[i];
            }
        }

        if (total == 0) return "Empty";
        return string.Join(", ", parts);
    }

    private void ClearAllWeights()
    {
        // Destroy all 3D spawned scale shapes
        for (int i = spawned3DObjects.Count - 1; i >= 0; i--)
        {
            if (spawned3DObjects[i] != null)
            {
                Destroy(spawned3DObjects[i]);
            }
        }
        spawned3DObjects.Clear();

        // Destroy all UI cards in staging box
        if (stagingBoxContainer != null)
        {
            foreach (Transform child in stagingBoxContainer)
            {
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    private void EndGame(bool claimedUnsolvable)
    {
        isGameActive = false;
        if (gameCanvas) gameCanvas.SetActive(false);
        if (gameOverCanvas) gameOverCanvas.SetActive(true);

        float diff = Mathf.Abs(leftTotal - rightTotal);
        string result = "";

        if (claimedUnsolvable)
        {
            result = !isSolvable 
                ? "<color=green><b>CORRECT!</b></color>\nThe puzzle was mathematically unsolvable." 
                : "<color=red><b>INCORRECT!</b></color>\nAn exact balance was possible!";
        }
        else
        {
            result = (diff == 0) 
                ? "<color=green><b>PERFECT BALANCE!</b></color>" 
                : $"Difference: {diff} kg";
        }

        if (finalScoreText) finalScoreText.text = result;
    }

    public void QuitGame()
    {
        isGameActive = false;
        ClearAllWeights();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}