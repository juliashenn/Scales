using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class TimerManager : NetworkBehaviour
{
    public static TimerManager Instance;

    [Header("Timers")]
    [SerializeField] private float roleSwitchInterval = 30f;
    [SerializeField] private float puzzleTimeLimit = 300f; // 5 minutes, change as needed

    [Header("Events")]
    public UnityEvent onSessionStarted;
    public UnityEvent onTimeExpired;           // Time up, game over
    public UnityEvent onRolesSwapped;

    private float roleTimer = 0f;
    private readonly NetworkVariable<float> puzzleTimer = new NetworkVariable<float>(0f);
    private readonly NetworkVariable<bool> sessionActive = new NetworkVariable<bool>(false);

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        //NetworkManager.Singleton.OnClientConnectedCallback += CheckPlayerCount;
    }

    private void Update()
    {
        if (!IsServer || !sessionActive.Value) return;

        // Role switching
        //roleTimer += Time.deltaTime;
        //if (roleTimer >= roleSwitchInterval)
        //{
        //    roleTimer = 0f;
        //    RoleManager.Instance?.CycleRoles();   // Extended function in RoleManager
        //    onRolesSwapped?.Invoke();
        //}

        // Puzzle timer
        puzzleTimer.Value += Time.deltaTime;
        if (puzzleTimer.Value >= puzzleTimeLimit)
        {
            EndSessionDueToTime();
        }
    }

    private void CheckPlayerCount(ulong clientId)
    {
        if (sessionActive.Value) return;

        // Start session when we have at least 3 players
        if (NetworkManager.Singleton.ConnectedClients.Count >= 3)
        {
            StartSession();
        }
    }

    public void StartSession()
    {
        if (!IsServer) return;
        sessionActive.Value = true;
        HUDVisibility.Instance?.SetVisible(true);
        roleTimer = 0f;
        puzzleTimer.Value = 0f;

        onSessionStarted?.Invoke();
        Debug.Log("[TimerManager] Game session started with 3+ players.");
    }

    private void EndSessionDueToTime()
    {
        if (!IsServer) return;
        sessionActive.Value = false;
        HUDVisibility.Instance?.SetVisible(false);
        onTimeExpired?.Invoke();
        Debug.Log("[TimerManager] Puzzle time expired - Game Over");

        // TODO: End the game properly (idk how that's done)
        ScaleManager.Instance?.TimeExpiredServerRpc();
    }

    public void EndSession()
    {
        HUDVisibility.Instance?.SetVisible(false);
        if (!IsServer) return;
        sessionActive.Value = false;
    }

    public float GetTimeLimit() => puzzleTimeLimit;

    public bool IsSessionActive() => sessionActive.Value;

    // Optional helper
    public float GetRemainingTime() => Mathf.Max(0, puzzleTimeLimit - puzzleTimer.Value);
}