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
    

    async void Start()
    {
        roomCodeText.text = "- -";

        NetworkManager.Singleton.OnClientStopped += ResetRoomCodeText;
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void ResetRoomCodeText(bool obj)
    {
        roomCodeText.text = "- -";
    }

    public async void CreateRoom()
    {
        string joinCode = await StartHostWithRelay();
        roomCodeText.text = joinCode;
        Debug.Log(roomCodeText.text);
    }

    public async void JoinRoom()
    {
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
