using Unity.Netcode;
using UnityEngine;

public class EndZoneTrigger : NetworkBehaviour
{
    public string newSceneToLoad;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            LoadNewSceneServerRPC();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void LoadNewSceneServerRPC()
    {
        NetworkManager.Singleton.SceneManager.LoadScene(newSceneToLoad, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
