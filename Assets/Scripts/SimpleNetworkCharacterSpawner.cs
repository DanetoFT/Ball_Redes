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
        // Iteramos hacia atrás para poder remover elementos de la lista sin romper el loop
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

        // Solo el servidor gestiona el spawn inicial
        if (IsServer)
        {
            // Spawnear al Host inmediatamente
            SpawnPlayerServerRPC(NetworkManager.Singleton.LocalClientId);
        }
        else
        {
            // Si eres cliente, pides al servidor que te spawnee
            SpawnPlayerServerRPC(NetworkManager.Singleton.LocalClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnPlayerServerRPC(ulong ownerPlayerID)
    {
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        // LÓGICA DE SPAWN:
        if (spawnPoints != null && spawnPoints.Count > 0)
        {
            // Usamos el operador módulo (%) para ciclar los puntos.
            // Si hay 2 puntos: El jugador 0 va al 0, el 1 al 1, el 2 vuelve al 0, etc.
            int index = playerSpawned.Count % spawnPoints.Count;

            spawnPos = spawnPoints[index].position;
            spawnRot = spawnPoints[index].rotation;
        }
        else
        {
            Debug.LogWarning("No hay Spawn Points asignados en el Spawner. Usando Vector3.zero");
        }

        // Instanciamos en la posición y rotación calculadas
        NetworkObject newPlayerSpawned = Instantiate(playerToSpawn, spawnPos, spawnRot);

        newPlayerSpawned.SpawnWithOwnership(ownerPlayerID);
        playerSpawned.Add(newPlayerSpawned);
        newPlayerSpawned.DestroyWithScene = true;
    }
}