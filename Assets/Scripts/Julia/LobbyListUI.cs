using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;

public class LobbyListUI : MonoBehaviour
{
    public static LobbyListUI Instance;
    [SerializeField] TextMeshProUGUI listText;

    void Awake() => Instance = this;

    public void RefreshList()
    {
        if (listText == null) return;

        var names = new List<string>();
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObj = client.PlayerObject;
            if (playerObj != null && playerObj.TryGetComponent<Player>(out var info))
            {
                names.Add(info.PlayerName.Value.ToString());
            }
        }

        listText.text = "Players in lobby:\n" + string.Join("\n", names);
    }
}
