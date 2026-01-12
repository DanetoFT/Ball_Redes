using Unity.Netcode;
using UnityEngine;

public class LobbyMenuManager : MonoBehaviour
{
    public void DisconnectFromServer ()
    {
        NetworkManager.Singleton.Shutdown();
    }
}
