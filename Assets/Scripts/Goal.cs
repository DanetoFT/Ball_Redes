// 3. Goal.cs - Ponlo en CADA PORTER�A (Collider IsTrigger=true) + NetworkObject (opcional)
using Unity.Netcode;
using UnityEngine;

public class Goal : NetworkBehaviour
{
    [SerializeField] private FootballScoreManager scoreManager;  // Arrastra el mismo ScoreManager

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.TryGetComponent<BallNetwork>(out var ball))
        {
            ulong scorerId = ball.GetLastOwnerId();  // �El �ltimo que toc� ANOTA!
            scoreManager.AddGoalServerRpc(scorerId);
            Debug.Log($"[Goal] �GOL para jugador {scorerId}!");

            // Opcional: Reset posici�n pelota inmediatamente (evita goles m�ltiples)
            ball.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            // Respawn r�pido o efecto
        }
    }
}