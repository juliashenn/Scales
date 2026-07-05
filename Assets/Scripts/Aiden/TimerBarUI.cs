using UnityEngine;
using UnityEngine.UI;

public class TimerBarUI : MonoBehaviour
{
    [SerializeField] private Image timerFillImage;
    [SerializeField] private float puzzleTimeLimit = 300f; // set to match TimerManager, or use the getter below

    void Update()
    {
        if (TimerManager.Instance == null) return;
        timerFillImage.fillAmount = TimerManager.Instance.GetRemainingTime() / puzzleTimeLimit;
    }
}
