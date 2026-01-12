using System;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public TMP_Text informationText;
    public TMP_InputField portAdress_Input;
    public TMP_InputField ipAdress_Input;
    public TMP_InputField playerName_Input;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        informationText.text = "Disconnected";
        NetworkManager.Singleton.OnClientStarted += OnClientConnectedMethod;
        NetworkManager.Singleton.OnClientStopped += OnClientDisconectedMethod;
    }

    private void OnClientDisconectedMethod(bool isHost)
    {
        informationText.text = "Disconnected";
    }

    private void OnClientConnectedMethod()
    {
        informationText.text = "Connected as " + (NetworkManager.Singleton.IsHost ? "Host" : "Client");
    }

    public void ConnectAsHost()
    {
        UserListManager.Singleton.localUserName = playerName_Input.text;

        if (SetConnectionData())
        {
            NetworkManager.Singleton.StartHost();
        }
    }

    public void ConnectAsClient()
    {
        UserListManager.Singleton.localUserName = playerName_Input.text;

        if (SetConnectionData())
        {
            NetworkManager.Singleton.StartClient();
        }
    }

    public bool SetConnectionData()
    {
        ushort portNumber = 7777;
        if(ushort.TryParse(portAdress_Input.text, out portNumber))
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipAdress_Input.text, portNumber);
            return true;
        }
        else
        {
            Debug.Log("error while paring port number, must be a value between 0 and 65535");
            return false;
        }
    }

    public void Disconnect()
    {
        NetworkManager.Singleton.Shutdown();
    }

    public void CloseApp()
    {
        Application.Quit();
    }
}
