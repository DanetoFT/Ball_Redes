using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CursorDeactivator : MonoBehaviour
{
    public Button loadGameButton;
    public Button loadGameButton2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        loadGameButton.interactable = NetworkManager.Singleton.IsHost;
        loadGameButton2.interactable = NetworkManager.Singleton.IsHost;
    }

}
