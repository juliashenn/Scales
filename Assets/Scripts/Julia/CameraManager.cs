using UnityEngine;
public class CameraManager : MonoBehaviour
{
    [SerializeField] private Camera topCamera;
    [SerializeField] private Camera frontCamera;

    void Awake()
    {
        // Start with a known default state — top camera on, front off
        topCamera.gameObject.SetActive(true);
        frontCamera.gameObject.SetActive(false);
    }

    void Start()
    {
        PlayerRoleHolder.OnRoleAssigned += HandleRole;

        // Apply immediately if role is already set (e.g. stale static value)
        // Only do this if it's a real role, not None
        if (PlayerRoleHolder.LocalRole != PlayerRole.None)
            HandleRole(PlayerRoleHolder.LocalRole);
    }

    void OnDestroy()
    {
        PlayerRoleHolder.OnRoleAssigned -= HandleRole;
    }

    void HandleRole(PlayerRole role)
    {
        if (topCamera == null || frontCamera == null) return;
        bool isFront = role == PlayerRole.RoleC;
        topCamera.gameObject.SetActive(!isFront);
        frontCamera.gameObject.SetActive(isFront);
    }
}