using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Disconnect : MonoBehaviour
{
    public Button testing;
    void Start()
    {
        testing.onClick.AddListener(disconnect);
    }

    public void disconnect()
    {
        Debug.Log("disconnecting");
        RelayManager.Instance?.ReturnToMainMenu();
    }
}