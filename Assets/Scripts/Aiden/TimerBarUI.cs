using UnityEngine;
using UnityEngine.UI;

public class TimerBarUI : MonoBehaviour
{
    [SerializeField] private Image timerFillImage;

    void Update()
    {
        if (TimerManager.Instance == null || !TimerManager.Instance.IsSessionActive()) return; 
        timerFillImage.fillAmount = TimerManager.Instance.GetRemainingTime() / TimerManager.Instance.GetTimeLimit();
    }
}
