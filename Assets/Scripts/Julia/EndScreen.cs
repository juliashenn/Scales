using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
public class EndScreen : MonoBehaviour
{
    public static EndScreen Instance;
    [SerializeField] private GameObject EndCanvas;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Button QuitButton;
    //[SerializeField] private Button PlayAgainButton;

    void Awake() => Instance = this;
    void Start()
    {
        QuitButton.onClick.AddListener(Quit);
        //PlayAgainButton.onClick.AddListener(PlayAgain);
    }
    public void GetScore()
    {
        if (ScaleManager.Instance == null) return;

        int score = Mathf.RoundToInt(ScaleManager.Instance.LiveScore);
        int bonusScore = Mathf.RoundToInt(ScaleManager.Instance.LiveBonusScore);
        scoreText.text = $"Score: {score}\nBonus Score: {bonusScore}";
    }

    public void Won(float score, float bonusScore)
    {
        Debug.Log("Won");
        resultText.text = "YOU WON!";
        scoreText.text = $"Score: {Mathf.RoundToInt(score)}\nBonus Score: {Mathf.RoundToInt(bonusScore)}";
        EndCanvas.SetActive(true);
    }

    public void Lost(float score, float bonusScore)
    {
        Debug.Log("Lost");
        resultText.text = "Game Over";
        scoreText.text = $"Score: {Mathf.RoundToInt(score)}\nBonus Score: {Mathf.RoundToInt(bonusScore)}";
        EndCanvas.SetActive(true);
    }
    public void Quit()
    {
        EndCanvas.SetActive(false);
        PlayerRoleHolder.ResetRole();
        NetworkManager.Singleton.Shutdown();
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
}
