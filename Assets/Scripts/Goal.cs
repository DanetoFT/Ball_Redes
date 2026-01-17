using Unity.Netcode;
using UnityEngine;

public class Goal : NetworkBehaviour
{
    [SerializeField] private FootballScoreManager scoreManager;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.TryGetComponent<BallNetwork>(out var ball))
        {
            ulong scorerId = ball.GetLastOwnerId();
            scoreManager.AddGoalServerRpc(scorerId);
            Debug.Log($"[Goal] �GOL para jugador {scorerId}!");

            ball.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        }
    }
}