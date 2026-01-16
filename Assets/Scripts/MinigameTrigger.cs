// 2. MinigameTrigger.cs - Ponlo en un GameObject con Collider (IsTrigger = true) + NetworkObject
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MinigameTrigger : NetworkBehaviour
{
    [SerializeField] private FootballScoreManager scoreManager;  // Arrastra en inspector

    private HashSet<ulong> playersInside = new HashSet<ulong>();
    private bool minigameActive = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.TryGetComponent<NetworkObject>(out var netObj))
        {
            ulong clientId = netObj.OwnerClientId;
            if (playersInside.Add(clientId))  // Añadido → cuenta jugadores únicos
            {
                Debug.Log($"[Trigger] Jugador {clientId} entró. Total: {playersInside.Count}");
                CheckStartMinigame();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        if (other.TryGetComponent<NetworkObject>(out var netObj))
        {
            ulong clientId = netObj.OwnerClientId;
            if (playersInside.Remove(clientId))
            {
                Debug.Log($"[Trigger] Jugador {clientId} salió. Total: {playersInside.Count}");
            }
        }
    }

    private void CheckStartMinigame()
    {
        if (playersInside.Count == 2 && !minigameActive)
        {
            minigameActive = true;
            // Obtener los dos IDs y ordenarlos (P1 = menor ID, P2 = mayor)
            ulong id1 = ulong.MaxValue, id2 = ulong.MaxValue;
            foreach (ulong id in playersInside)
            {
                if (id < id1) { id2 = id1; id1 = id; }
                else id2 = id;
            }
            scoreManager.StartMinigameServer(id1, id2);
            Debug.Log($"[Minigame] ¡INICIADO! P1:{id1} vs P2:{id2}");
        }
    }
}