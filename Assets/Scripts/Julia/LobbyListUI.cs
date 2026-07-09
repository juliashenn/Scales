using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyListUI : MonoBehaviour
{
    public static LobbyListUI Instance;
    [SerializeField] TextMeshProUGUI listText;
    [SerializeField] Button readyButton;
    [SerializeField] Image readyButtonImg;
    [SerializeField] TextMeshProUGUI readyButtontext;
    [SerializeField] Button leaveButton;


    [SerializeField] private GameObject blackoutPanel;
    [SerializeField] private GameObject lobbyUI;

    [SerializeField] private Color green;
    [SerializeField] private Color red;

    private List<string> readyButtonTexts = new List<string> {"I'm Ready!", "Cancel Ready"};
    private bool localReady = false;

    void Awake() => Instance = this;

    public void ShowLobby()
    {
        Debug.Log("showing lobby");
        ResetState();
        blackoutPanel.SetActive(true);
        lobbyUI.SetActive(true);

        Debug.Log("lobbyUI activeSelf: " + lobbyUI.activeSelf);
        Debug.Log("lobbyUI activeInHierarchy: " + lobbyUI.activeInHierarchy);
        Debug.Log("blackoutPanel activeInHierarchy: " + blackoutPanel.activeInHierarchy);
        RefreshList();
    }

    public void ResetState()
    {
        localReady = false;
        readyButtontext.text = readyButtonTexts[0];
        readyButtonImg.color = green;
    }
    void Start()
    {
        readyButton.onClick.AddListener(ToggleReady);
        leaveButton.onClick.AddListener(LeaveLobby);

    }

    void ToggleReady()
    {
        localReady = !localReady;
        readyButtontext.text = localReady ? readyButtonTexts[1] : readyButtonTexts[0];
        readyButtonImg.color = localReady ? red : green;

        // Set on our own Player NetworkObject
        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer != null && localPlayer.TryGetComponent<Player>(out var player))
            player.IsReady.Value = localReady;

        ulong localId = NetworkManager.Singleton.LocalClientId;
        if (ScaleManager.Instance != null && ScaleManager.Instance.IsSpawned)
            ScaleManager.Instance.ReadyCheckServerRpc(localId, localReady);
    }

    public void HideBlackout()
    {
        blackoutPanel.SetActive(false);
        lobbyUI?.SetActive(false);
    }
    public void RefreshList()
    {
        if (listText == null) return;

        var names = new List<string>();
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObj = client.PlayerObject;
            if (playerObj != null && playerObj.TryGetComponent<Player>(out var info))
            {
                string check = info.IsReady.Value ? "<sprite=0>" : "";
                names.Add(info.PlayerName.Value.ToString() + check);
            }
        }

        listText.text = "Players in lobby:\n" + string.Join("\n", names);
    }

    void LeaveLobby()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            // Host leaving kicks everyone since the relay dies with it
            // Warn them first via a confirm dialog if you want, or just shut down
            NetworkManager.Singleton.Shutdown();
        }
        else
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Reset local state
        PlayerRoleHolder.ResetRole();
        localReady = false;
        readyButtontext.text = readyButtonTexts[0];
        readyButtonImg.color = green;

        // Show the host/join screen again
        // You'll need a reference to HostJoinCanvas here, or call back into RelayManager
        RelayManager.Instance?.ShowHostJoinScreen();
    }
}