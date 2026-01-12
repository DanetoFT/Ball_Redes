using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectionCallbackManager : MonoBehaviour
{
    private static ConnectionCallbackManager singleton;
    public static ConnectionCallbackManager Singleton => singleton;

    public TMP_Text informationalText;

    private void Awake()
    {
        if(singleton == null)
        {
            singleton = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        NetworkManager.Singleton.OnClientStarted += OnClientStartedMethod;
        NetworkManager.Singleton.OnClientStopped += OnClientStoppedMethod;
    }

    private void OnClientStoppedMethod(bool obj)
    {
        if(informationalText != null)
        {
            informationalText.text = "Disconnected";
        }
        SceneManager.LoadScene("MainMenu");
        UserListManager.Singleton.userConnectedList.Clear();
    }

    private void OnClientStartedMethod()
    {
        if(informationalText != null)
        {
            informationalText.text = "Connected as " + (NetworkManager.Singleton.IsHost ? "Host" : "Client");
        }

        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("Lobby", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}
