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
    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;
    [SerializeField] TMP_InputField joinInput;
    [SerializeField] GameObject LobbyCanvas;
    [SerializeField] TextMeshProUGUI codeText;
    [SerializeField] GameObject HostJoinCanvas;

    async void Start()
    {
        await UnityServices.InitializeAsync();

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
    }

    void RegisterLobbyCallbacks()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientChanged;
    }

    void OnClientChanged(ulong clientId)
    {
        LobbyListUI.Instance?.RefreshList();
    }
}
