using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public TMP_Text roomCodeText;
    public TMP_InputField roomCodeInput;

    MainMenuManager mainMenu;


    async void Start()
    {
        roomCodeText.text = "- -";

        NetworkManager.Singleton.OnClientStopped += ResetRoomCodeText;
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        mainMenu = FindAnyObjectByType<MainMenuManager>();
    }

    private void ResetRoomCodeText(bool obj)
    {
        roomCodeText.text = "- -";
    }

    public async void CreateRoom()
    {
        if (mainMenu != null && mainMenu.playerName_Input != null)
        {
            string name = mainMenu.playerName_Input.text;
            UserListManager.Singleton.localUserName = string.IsNullOrEmpty(name) ? "Host" : name;
        }

        string joinCode = await StartHostWithRelay();

        if (!string.IsNullOrEmpty(joinCode))
        {
            if (DontDestroyCode.Singleton != null && DontDestroyCode.Singleton.text != null)
            {
                DontDestroyCode.Singleton.text.text = joinCode;
            }
        }
    }

    public async void JoinRoom()
    {
        if (mainMenu != null && mainMenu.playerName_Input != null)
        {
            string name = mainMenu.playerName_Input.text;
            UserListManager.Singleton.localUserName = string.IsNullOrEmpty(name) ? "Client" : name;
        }

        await StartClientWithRelay(roomCodeInput.text);
        roomCodeText.text = roomCodeInput.text;
    }

    private async Task<string> StartHostWithRelay(int maxConnections = 3)
    {
        Allocation allocation;
        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        }
        catch
        {
            roomCodeText.text = "- -";
            throw;
        }
        if (allocation != null)
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            return NetworkManager.Singleton.StartHost() ? joinCode : null;
        }
        else
        {
            return "error";
        }
    }

    private async Task<bool> StartClientWithRelay(String roomCode)
    {
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(roomCode);

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));

        bool hasJoined = !string.IsNullOrEmpty(roomCode) && NetworkManager.Singleton.StartClient();
        return hasJoined;
    }
}
