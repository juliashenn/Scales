using UnityEngine;
using TMPro;

public class ScoreText : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

    void Update()
    {
        if (ScaleManager.Instance == null || !TimerManager.Instance.IsSessionActive()) return;

        int score = Mathf.RoundToInt(ScaleManager.Instance.GetScore());
        int bonusScore = Mathf.RoundToInt(ScaleManager.Instance.GetBonusScore());
        scoreText.text = $"Score: {score}\nBonus Score: {bonusScore}";
    }
}
