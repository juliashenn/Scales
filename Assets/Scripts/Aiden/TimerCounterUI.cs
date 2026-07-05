using UnityEngine;
using TMPro;

public class TimerCounterUI : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;

    void Update()
    {
        if (TimerManager.Instance == null) return;

        float remaining = TimerManager.Instance.GetRemainingTime();
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
