using UnityEngine;
using TMPro;

public class StageCompleteText : MonoBehaviour
{
    [SerializeField] private TMP_Text stageText;

    public void Show() => stageText.gameObject.SetActive(true);
    public void Hide() => stageText.gameObject.SetActive(false);

    void Start()
    {
        Hide();
    }

    public void SetText(int currentStage, int totalStages)
    {
        stageText.text = $"Stage {currentStage}/{totalStages} cleared!";
    }
}
