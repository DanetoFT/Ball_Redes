using UnityEngine;
using Unity.Netcode;

public class PlayingWithRPCs : NetworkBehaviour
{
    public void ExecuteClient_RPC()
    {
        MyClientRPC();
    }

    public void ExecuteServer_RPC()
    {
        MyServerRPC();
    }

    [ClientRpc]
    private void MyClientRPC()
    {
        Debug.Log("I'm a ClientRPC called from the: " + (NetworkManager.Singleton.IsHost ? "Host" : "Client"));
    }

    [ServerRpc(RequireOwnership = false)]
    private void MyServerRPC()
    {
        Debug.Log("I'm a ServerRPC called from the: " + (NetworkManager.Singleton.IsHost ? "Host" : "Client"));
    }
}
