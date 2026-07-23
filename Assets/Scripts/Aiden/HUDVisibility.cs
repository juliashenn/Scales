using Unity.Netcode;
using UnityEngine;

public class HUDVisibility : MonoBehaviour
{

    public static HUDVisibility Instance;
    [SerializeField] private CanvasGroup hudRoot;

    private void Awake()
    {
        Instance = this;
        SetVisible(false);
    }
    void Update()
    {
        if (TimerManager.Instance == null)
        {
            SetVisible(false);
            return;
        }

        bool inNetworkSession = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        bool active = inNetworkSession && TimerManager.Instance.IsSessionActive();
        hudRoot.alpha = active ? 1f : 0f;
        hudRoot.interactable = active;
        hudRoot.blocksRaycasts = active;
    }

    public void SetVisible(bool visible)
    {
        Debug.Log("[HUD] setting visibility" + visible);
        hudRoot.alpha = visible ? 1f : 0f;
        hudRoot.interactable = visible;
        hudRoot.blocksRaycasts = visible;
    }
}
