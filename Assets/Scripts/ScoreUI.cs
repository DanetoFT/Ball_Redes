using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI introText;
    [SerializeField] private TextMeshProUGUI victoryText;
    [SerializeField] private GameObject canvas;

    private FootballScoreManager scoreManager;

    public static ScoreUI Instance;

    private void Start()
    {
        scoreManager = FootballScoreManager.Instance;
        canvas.gameObject.SetActive(false);
    }

    private void UpdateScoreDisplay(int prev, int curr)
    {
        scoreText.text = $"Jugador 1: {scoreManager.scorePlayer1.Value} | Jugador 2: {scoreManager.scorePlayer2.Value}";
    }

    public void ShowIntro()
    {
        canvas.gameObject.SetActive(true);
        introText.gameObject.SetActive(true);
        Debug.Log($"[UI] Mostrando intro en cliente {NetworkManager.Singleton.LocalClientId}");
    }

    public void HideIntro()
    {
        introText.gameObject.SetActive(false);
    }

    public void ShowVictory(int winner)
    {
        victoryText.text = $"¡Jugador {winner} GANA!";
        victoryText.gameObject.SetActive(true);
    }

    public void HideCanvas()
    {
        canvas.gameObject.SetActive(false);
        introText.gameObject.SetActive(false);
        victoryText.gameObject.SetActive(false);
    }
}