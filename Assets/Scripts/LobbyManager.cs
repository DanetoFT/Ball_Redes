using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public string sceneToLoad = "GameScene_1";
    public Button loadGameButton;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        loadGameButton.interactable = NetworkManager.Singleton.IsHost;
    }
    public void LoadNewGame()
    {
        LoadNewGameServerRPC();
    }

    [ServerRpc(RequireOwnership = false)]
    private void LoadNewGameServerRPC()
    {
        NetworkManager.Singleton.SceneManager.LoadScene(sceneToLoad, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
