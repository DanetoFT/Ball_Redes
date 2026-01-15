using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SimpleNetworkCharacterSpawner : NetworkBehaviour
{
    [Header("Referencias")]
    public NetworkObject playerToSpawn;

    [Tooltip("Arrastra aquí los objetos vacíos donde quieres que nazcan los jugadores")]
    public List<Transform> spawnPoints;

    [Header("Estado")]
    public List<NetworkObject> playerSpawned;

    private void Start()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnectRemovePlayerFromSpawned;
    }

    private void ClientDisconnectRemovePlayerFromSpawned(ulong playerDisconnect)
    {
        for (int i = playerSpawned.Count - 1; i >= 0; i--)
        {
            if (playerSpawned[i].OwnerClientId == playerDisconnect)
            {
                playerSpawned.RemoveAt(i);
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        playerSpawned = new List<NetworkObject>();

        if (IsServer)
        {
            SpawnPlayerServerRPC(NetworkManager.Singleton.LocalClientId);
        }
        else
        {
            SpawnPlayerServerRPC(NetworkManager.Singleton.LocalClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnPlayerServerRPC(ulong ownerPlayerID)
    {
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (spawnPoints != null && spawnPoints.Count > 0)
        {
            int index = playerSpawned.Count % spawnPoints.Count;

            spawnPos = spawnPoints[index].position;
            spawnRot = spawnPoints[index].rotation;
        }
        else
        {
            Debug.LogWarning("No hay Spawn Points asignados en el Spawner. Usando Vector3.zero");
        }
        NetworkObject newPlayerSpawned = Instantiate(playerToSpawn, spawnPos, spawnRot);

        newPlayerSpawned.SpawnWithOwnership(ownerPlayerID);
        playerSpawned.Add(newPlayerSpawned);
        newPlayerSpawned.DestroyWithScene = true;
    }
}