using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class FootballScoreManager : NetworkBehaviour
{
    public static FootballScoreManager Instance;

    [Header("Scores")]
    public NetworkVariable<int> scorePlayer1 = new(0);
    public NetworkVariable<int> scorePlayer2 = new(0);

    [Header("Estado")]
    public NetworkVariable<bool> isMinigameActive = new(false);

    private ulong player1Id, player2Id;
    private bool playersAssigned = false;

    public ScoreUI scoreUI;

    public override void OnNetworkSpawn()
    {
        Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartMinigameServerRpc(ulong id1, ulong id2)
    {
        StartMinigameServer(id1, id2);
    }

    public void StartMinigameServer(ulong id1, ulong id2)
    {
        player1Id = id1;
        player2Id = id2;
        playersAssigned = true;

        scorePlayer1.Value = 0;
        scorePlayer2.Value = 0;
        isMinigameActive.Value = true;

        StartCoroutine(ShowIntroText());
    }

    private IEnumerator ShowIntroText()
    {
        ShowIntroClientRpc();
        yield return new WaitForSeconds(2f);
        HideIntroClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddGoalServerRpc(ulong scorerId)
    {
        AddGoal(scorerId);
    }

    private void AddGoal(ulong scorerId)
    {
        if (!isMinigameActive.Value || !playersAssigned) return;

        if (scorerId == player1Id)
            scorePlayer1.Value++;
        else if (scorerId == player2Id)
            scorePlayer2.Value++;

        CheckVictory(scorerId);
    }

    private void CheckVictory(ulong winnerId)
    {
        int score1 = scorePlayer1.Value;
        int score2 = scorePlayer2.Value;

        if (score1 >= 3 || score2 >= 3)
        {
            ShowVictoryClientRpc(winnerId == player1Id ? 1 : 2);
            isMinigameActive.Value = false;
            StartCoroutine(ResetMinigame(5f));
        }
    }

    private IEnumerator ResetMinigame(float delay)
    {
        yield return new WaitForSeconds(delay);
        scorePlayer1.Value = 0;
        scorePlayer2.Value = 0;
        isMinigameActive.Value = false;
        playersAssigned = false;
        HideCanvasClientRpc();
        Debug.Log("[Minigame] ¡RESET!");
    }

    // ─── CLIENT RPCs para UI ───
    [ClientRpc]
    private void ShowIntroClientRpc()
    {
        scoreUI.ShowIntro();
    }


    [ClientRpc]
    private void HideIntroClientRpc()
    {
        FindObjectOfType<ScoreUI>()?.HideIntro();
    }

    [ClientRpc]
    private void ShowVictoryClientRpc(int winnerPlayerNum)  // 1 o 2
    {
        FindObjectOfType<ScoreUI>()?.ShowVictory(winnerPlayerNum);
    }

    [ClientRpc]
    private void HideCanvasClientRpc()
    {
        FindObjectOfType<ScoreUI>()?.HideCanvas();
    }
}