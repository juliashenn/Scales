using UnityEngine;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine.UI;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using Mono.Cecil.Cil;

public class RelayManager : MonoBehaviour
{

    public static RelayManager Instance;
    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;
    [SerializeField] TMP_InputField joinInput;
    [SerializeField] GameObject LobbyCanvas;
    [SerializeField] TextMeshProUGUI codeText;
    [SerializeField] GameObject HostJoinCanvas;

    private void Awake() => Instance = this;
    async void Start()
    {
        PlayerRoleHolder.ResetRole();
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        hostButton.onClick.AddListener(CreateRelay);
        joinButton.onClick.AddListener(() => JoinRelay(joinInput.text));
        joinInput.onSubmit.AddListener((code) => JoinRelay(code));
    }

    async void CreateRelay()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3); // allow 3 max connections
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        codeText.text = "Lobby Code: " + joinCode;
        if (HostJoinCanvas != null) HostJoinCanvas.SetActive(false);
        if (LobbyCanvas != null) LobbyCanvas.SetActive(true);

        var relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
        RegisterLobbyCallbacks();
        NetworkManager.Singleton.StartHost();
        LobbyListUI.Instance?.ShowLobby();
    }

    async void JoinRelay(string joinCode)
    {
        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        codeText.text = "Lobby Code: " + joinCode;
        if (HostJoinCanvas != null) HostJoinCanvas.SetActive(false);
        if (LobbyCanvas != null) LobbyCanvas.SetActive(true);

        var relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
        RegisterLobbyCallbacks();
        NetworkManager.Singleton.StartClient();
        LobbyListUI.Instance?.ShowLobby();
    }

    void RegisterLobbyCallbacks()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnected;
    }

    void OnClientChanged(ulong clientId)
    {
        LobbyListUI.Instance?.RefreshList();
    }

    public void ShowHostJoinScreen()
    {
        HostJoinCanvas?.SetActive(true);
        LobbyCanvas?.SetActive(false);
        LobbyListUI.Instance?.ResetState();
    }

    void OnDisconnected(ulong clientId)
    {
        // If we're a client and we get disconnected (not by our own choice), go back to menu
        if (NetworkManager.Singleton.IsHost) return;
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        PlayerRoleHolder.SetRole(PlayerRole.None);
        ShowHostJoinScreen();
    }
}
