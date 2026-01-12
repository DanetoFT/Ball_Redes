using UnityEngine;
using Unity.Netcode;
using TMPro;

public class ChatManager : NetworkBehaviour
{
    public TMP_Text chat;
    public TMP_InputField inputChat;
    public TMP_Text userListLog;

    public override void OnNetworkSpawn()
    {
        UserListManager.Singleton.RefreshUserConnectedListClientRPC(UserListManager.Singleton.userConnectedList.ToArray());
    }

    public void EnviarMensaje()
    {
        string chatMessage = inputChat.text;
        inputChat.text = "";
        SendMessageServerRPC(chatMessage, UserListManager.Singleton.localUserName);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendMessageServerRPC(string chatMessage, string userName)
    {
        SendMessageClientRPC(chatMessage, userName);
    }

    [ClientRpc]
    private void SendMessageClientRPC(string chatMessage, string userName)
    {
        chat.text += userName + ": " + chatMessage + "\n";
    }

    public void DisconnectGame()
    {
        NetworkManager.Singleton.Shutdown();
    }


    /*[ClientRpc]
    private void MyClientRPC(string texto)
    {
        Debug.Log("I'm a ClientRPC called from the: " + (NetworkManager.Singleton.IsHost ? "Host" : "Client"));
        chat.text += "\n" + texto;
    }

    [ServerRpc(RequireOwnership = false)]
    private void MyServerRPC(string texto)
    {
        MyClientRPC(texto);
        Debug.Log("I'm a ServerRPC called from the: " + (NetworkManager.Singleton.IsHost ? "Host" : "Client"));

        //inputChat.text = "";
    }*/
}
