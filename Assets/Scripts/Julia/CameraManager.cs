using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private Camera topCamera;
    [SerializeField] private Camera frontCamera;
    void Start()
    {
        PlayerRoleHolder.OnRoleAssigned += HandleRole;
    }

    void HandleRole(PlayerRole role)
    {
        bool isFront = role == PlayerRole.RoleC;
        topCamera.gameObject.SetActive(!isFront);
        frontCamera.gameObject.SetActive(isFront);
    }
}
