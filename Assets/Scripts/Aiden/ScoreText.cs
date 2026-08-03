using UnityEngine;
using TMPro;

public class ScoreText : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

    /**void Update()
    {
        if (PlayerRoleHolder.LocalRole != PlayerRole.RoleC)
        {
            scoreText.gameObject.SetActive(false);
            return;
        }
        scoreText.gameObject.SetActive(true);

        if (ScaleManager.Instance == null || !TimerManager.Instance.IsSessionActive()) return;

        int score = Mathf.RoundToInt(ScaleManager.Instance.LiveScore);
        int bonusScore = Mathf.RoundToInt(ScaleManager.Instance.LiveBonusScore);
        scoreText.text = $"Score: {score}\nBonus Score: {bonusScore}";
    }**/
}
