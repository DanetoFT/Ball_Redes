using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MinigameTrigger : NetworkBehaviour
{
    [SerializeField] private FootballScoreManager scoreManager;

    private HashSet<ulong> playersInside = new();

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (!other.transform.root.TryGetComponent<NetworkObject>(out var netObj))
            return;

        if (!netObj.IsPlayerObject)
            return;

        ulong clientId = netObj.OwnerClientId;

        if (playersInside.Add(clientId))
        {
            Debug.Log($"[Trigger] Player {clientId} entró. Total: {playersInside.Count}");
            CheckStartMinigame();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        if (!other.transform.root.TryGetComponent<NetworkObject>(out var netObj))
            return;

        if (!netObj.IsPlayerObject)
            return;

        ulong clientId = netObj.OwnerClientId;

        if (playersInside.Remove(clientId))
        {
            Debug.Log($"[Trigger] Player {clientId} salió. Total: {playersInside.Count}");
        }
    }

    private void CheckStartMinigame()
    {
        if (playersInside.Count != 2)
            return;

        if (scoreManager.isMinigameActive.Value)
            return;

        ulong[] ids = new ulong[2];
        playersInside.CopyTo(ids);

        ulong id1 = ids[0] < ids[1] ? ids[0] : ids[1];
        ulong id2 = ids[0] < ids[1] ? ids[1] : ids[0];

        scoreManager.StartMinigameServer(id1, id2);

        Debug.Log($"[Minigame] START → P1:{id1} vs P2:{id2}");
    }

    // 👇 OPCIONAL pero MUY recomendado
    public void ResetTrigger()
    {
        playersInside.Clear();
    }
}
