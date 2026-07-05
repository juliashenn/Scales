using UnityEngine;

public class HUDVisibility : MonoBehaviour
{
    [SerializeField] private CanvasGroup hudRoot;

    void Update()
    {
        if (TimerManager.Instance == null) return;

        bool active = TimerManager.Instance.IsSessionActive();
        hudRoot.alpha = active ? 1f : 0f;
        hudRoot.interactable = active;
        hudRoot.blocksRaycasts = active;
    }
}
