using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;
using Unity.Services.Authentication;

public class SimpleNetworkCharacterController : NetworkBehaviour
{
    public NetworkObject playerNetworkPrefab;

    public override void OnNetworkSpawn()
    {
        SpawnPlayerServerRPC(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerServerRPC(ulong playerId)
    {
        NetworkObject newPlayer = Instantiate(playerNetworkPrefab);
        newPlayer.SpawnWithOwnership(playerId);
        newPlayer.DestroyWithScene = true;
    }
}
