// 5. ScoreUI.cs - Ponlo en tu CANVAS (NetworkObject + UICanvas o similar)
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ScoreUI : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;     // "Jugador 1: X | Jugador 2: Y"
    [SerializeField] private TextMeshProUGUI introText;     // "¡Marca 3 goles para ganar!"
    [SerializeField] private TextMeshProUGUI victoryText;   // "¡Jugador X gana!"
    [SerializeField] private Canvas canvas;                 // El Canvas padre

    private FootballScoreManager scoreManager;

    public override void OnNetworkSpawn()
    {
        scoreManager = FootballScoreManager.Instance;
        canvas.gameObject.SetActive(false);  // Oculto al inicio
        introText.gameObject.SetActive(false);
        victoryText.gameObject.SetActive(false);

        scoreManager.scorePlayer1.OnValueChanged += UpdateScoreDisplay;
        scoreManager.scorePlayer2.OnValueChanged += UpdateScoreDisplay;
    }

    private void UpdateScoreDisplay(int prev, int curr)
    {
        scoreText.text = $"Jugador 1: {scoreManager.scorePlayer1.Value} | Jugador 2: {scoreManager.scorePlayer2.Value}";
    }

    public void ShowIntro()
    {
        introText.gameObject.SetActive(true);
        canvas.gameObject.SetActive(true);
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