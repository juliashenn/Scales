using UnityEngine;
using TMPro;

public class StageCompleteText : MonoBehaviour
{
    [SerializeField] private TMP_Text stageText;

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);

    public void SetText(int currentStage, int totalStages)
    {
        stageText.text = $"Stage {currentStage}/{totalStages} cleared!";
    }
}
