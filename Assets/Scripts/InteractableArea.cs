using System;
using Unity.Netcode;
using UnityEngine;

public class InteractableArea : NetworkBehaviour
{
    public GameObject associatedPlatform;
    public bool isPlayerInside;

    private void Start()
    {
        associatedPlatform.SetActive(false);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            UpdatePlatformStatusServerRPC(isPlayerInside);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            UpdatePlatformStatusServerRPC(isPlayerInside);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            UpdatePlatformStatusServerRPC(isPlayerInside);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdatePlatformStatusServerRPC(bool platformStatus)
    {
        UpdatePlatformStatusClientRPC(platformStatus);
    }

    [ClientRpc]
    private void UpdatePlatformStatusClientRPC(bool platformStatus)
    {
        associatedPlatform.SetActive(platformStatus);
    }
}
