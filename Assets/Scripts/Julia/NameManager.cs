using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class NameManager : MonoBehaviour
{
    [SerializeField] TMP_InputField nameInput;
    [SerializeField] GameObject nameScreen;
    [SerializeField] GameObject hostJoinScreen;
    [SerializeField] Button confirmButton;

    public static string LocalPlayerName { get; private set; } = "Player";
    void Start()
    {
        nameInput.onSubmit.AddListener((text) => ConfirmName(text.Trim()));
        confirmButton.onClick.AddListener(() => ConfirmName(nameInput.text.Trim()));
    }

    void ConfirmName(string name)
    {
        //string name = nameInput.text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            name = "Player" + Random.Range(1000, 9999);
        }

        LocalPlayerName = name;
        nameScreen.SetActive(false);
        hostJoinScreen.SetActive(true);
    }
}
