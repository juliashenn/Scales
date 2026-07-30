using System.Threading.Tasks;
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

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance;
    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;
    [SerializeField] TMP_InputField joinInput;
    [SerializeField] GameObject LobbyCanvas;
    [SerializeField] TMP_InputField codeText;
    [SerializeField] GameObject HostJoinCanvas;

    private bool pendingReturnToMenu;
    private bool connectingRelay;
    private float connectionTimeout = 5f;
    private float connectionTimer = 0f;
    private bool waitingForConnection = false;

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
        if (connectingRelay) return;

        connectingRelay = true;
        CancelPendingReturnToMenu();

        try
        {
            await EnsureNetworkShutdownAsync();

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            codeText.text = joinCode;

            var relayServerData = AllocationUtils.ToRelayServerData(allocation, "wss");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            RegisterLobbyCallbacks();

            if (!NetworkManager.Singleton.StartHost())
            {
                Debug.LogError("[RelayManager] StartHost failed.");
                ShowHostJoinScreen();
                return;
            }

            ShowLobbyScreen();
            LobbyListUI.Instance?.ShowLobby();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[RelayManager] CreateRelay failed: {ex.Message}");
            ShowHostJoinScreen();
        }
        finally
        {
            connectingRelay = false;
        }
    }

    async void JoinRelay(string joinCode)
    {
        if (connectingRelay || string.IsNullOrWhiteSpace(joinCode)) return;

        connectingRelay = true;
        CancelPendingReturnToMenu();

        try
        {
            await EnsureNetworkShutdownAsync();

            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            codeText.text = joinCode;

            var relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, "wss");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            RegisterLobbyCallbacks();

            if (!NetworkManager.Singleton.StartClient())
            {
                Debug.LogError("[RelayManager] StartClient failed.");
                ShowHostJoinScreen();
                return;
            }

            // Start timeout check in case host is gone
            waitingForConnection = true;
            connectionTimer = connectionTimeout;

            ShowLobbyScreen();
            LobbyListUI.Instance?.ShowLobby();
            LobbyListUI.Instance?.ShowConnecting();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[RelayManager] JoinRelay failed: {ex.Message}");
            ShowHostJoinScreen();
        }
        finally
        {
            connectingRelay = false;
        }
    }

    void Update()
    {
        if (!waitingForConnection) return;

        // Stop waiting once fully connected
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            waitingForConnection = false;
            return;
        }

        connectionTimer -= Time.deltaTime;
        if (connectionTimer <= 0f)
        {
            waitingForConnection = false;
            Debug.LogWarning("[RelayManager] Connection timed out, returning to menu.");
            ReturnToMainMenu();
        }
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
        if (NetworkManager.Singleton.IsHost && TimerManager.Instance != null && TimerManager.Instance.IsSessionActive())
        {
            ScaleManager.Instance?.PlayerLeftDuringGameServerRpc();
        }
    }

    public void ReturnToMainMenu()
    {
        EndScreen.Instance?.Hide();
        PlayerRoleHolder.ResetRole();
        ScaleManager.Instance?.ResetPans();
        TimerManager.Instance?.EndSession();
        HUDVisibility.Instance?.SetVisible(false);
        LobbyListUI.Instance?.HideBlackout();

        if (connectingRelay || pendingReturnToMenu)
            return;

        var networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsListening)
        {
            ShowHostJoinScreen();
            return;
        }

        pendingReturnToMenu = true;
        networkManager.OnClientStopped -= OnNetworkStoppedReturnToMenu;
        networkManager.OnClientStopped += OnNetworkStoppedReturnToMenu;
        networkManager.Shutdown();
    }

    public void ShowHostJoinScreen()
    {
        if (connectingRelay) return;

        TimerManager.Instance?.EndSession();
        LobbyListUI.Instance?.HideBlackout();
        HostJoinCanvas?.SetActive(true);
        LobbyCanvas?.SetActive(false);
        LobbyListUI.Instance?.ResetState();
    }

    void ShowLobbyScreen()
    {
        TimerManager.Instance?.EndSession();
        HostJoinCanvas?.SetActive(false);
        LobbyCanvas?.SetActive(true);
    }

    void OnDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost) return;
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        Debug.Log("[RelayManager] Local client disconnected, returning to main menu.");
        ReturnToMainMenu();
    }

    void OnNetworkStoppedReturnToMenu(bool wasHost)
    {
        NetworkManager.Singleton.OnClientStopped -= OnNetworkStoppedReturnToMenu;

        if (!pendingReturnToMenu || connectingRelay)
            return;

        pendingReturnToMenu = false;
        ShowHostJoinScreen();
    }

    void CancelPendingReturnToMenu()
    {
        pendingReturnToMenu = false;
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientStopped -= OnNetworkStoppedReturnToMenu;
    }

    static Task EnsureNetworkShutdownAsync()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsListening)
            return Task.CompletedTask;

        var shutdownComplete = new TaskCompletionSource<bool>();
        void OnStopped(bool wasHost)
        {
            networkManager.OnClientStopped -= OnStopped;
            shutdownComplete.TrySetResult(true);
        }

        networkManager.OnClientStopped += OnStopped;
        networkManager.Shutdown();
        return shutdownComplete.Task;
    }

    public void ShowLobbyCanvas()
    {
        ShowLobbyScreen();
    }
}
