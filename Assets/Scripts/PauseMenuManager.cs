using Unity.Netcode;
using UnityEngine;
public class PauseMenuManager : MonoBehaviour
{
    public GameObject pauseMenuHolderObject;
    public bool isPaused;
    private void Start()
    {
        ClosePauseMenu();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isPaused)
            {
                ShowPauseMenu();
            }
            else
            {
                ClosePauseMenu();
            }
        }
    }
    public void ShowPauseMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        pauseMenuHolderObject.SetActive(true);
        isPaused = true;

    }
    public void ClosePauseMenu()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        pauseMenuHolderObject.SetActive(false);
        isPaused = false;
    }
    public void DisconnectFromGame()
    {
        NetworkManager.Singleton.Shutdown();
    }
    public void ExitGame()
    {
        Application.Quit();
    }

    [ServerRpc(RequireOwnership = false)]
    public void LoadNewSceneServerRPC(string newSceneToLoad)
    {
        NetworkManager.Singleton.SceneManager.LoadScene(newSceneToLoad, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}