using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;

[Serializable]
public class UserConnectedData : INetworkSerializable
{
    public ulong userId;
    public string userName;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref userId);
        serializer.SerializeValue(ref userName);
    }
}

public class UserListManager : NetworkBehaviour
{
    public List<UserConnectedData> userConnectedList;

    private static UserListManager singleton;

    public string localUserName;

    public static UserListManager Singleton => singleton;

    public static event Action OnUserListUpdated;

    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        userConnectedList = new List<UserConnectedData>();
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedMethod;
    }
    public override void OnNetworkSpawn()
    {
        //base.OnNetworkSpawn();
        AddNewUserServerRPC(NetworkManager.Singleton.LocalClientId, localUserName);
    }
    private void OnClientDisconnectedMethod(ulong userID)
    {
        for (int i = 0; i < userConnectedList.Count; i++)
        {
            if (userConnectedList[i].userId == userID)
            {
                userConnectedList.Remove(userConnectedList[i]);
            }
        }
        RefreshUserConnectedListClientRPC(userConnectedList.ToArray());
    }

    private void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddNewUserServerRPC(ulong newUserId, string newUserName)
    {
        UserConnectedData newUserConnectedData = new UserConnectedData();
        newUserConnectedData.userId = newUserId;
        newUserConnectedData.userName = newUserName;
        userConnectedList.Add(newUserConnectedData);
        RefreshUserConnectedListClientRPC(userConnectedList.ToArray());
    }
    [ClientRpc]
    public void RefreshUserConnectedListClientRPC(UserConnectedData[] userConnectedArray)
    {
        userConnectedList = userConnectedArray.ToList();
        UpdateUsersConnectedList_Visually();

        OnUserListUpdated?.Invoke();
    }


    public void UpdateUsersConnectedList_Visually()
    {
        ChatManager currentChatManager = FindAnyObjectByType<ChatManager>();
        if (currentChatManager != null)
        {
            TMP_Text userListLog = FindAnyObjectByType<ChatManager>().userListLog;
            userListLog.text = "";

            for (int i = 0; i < userConnectedList.Count; i++)
            {
                userListLog.text += userConnectedList[i].userName + "\n";
            }
        }
        else
        {
            Debug.Log("Unable to update userlist log, because there is no " + nameof(ChatManager) + " present");
        }
    }
}