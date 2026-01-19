using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkData : NetworkBehaviour
{
    public NetworkVariable<FixedString64Bytes> playerName = 
        new NetworkVariable<FixedString64Bytes>(
            writePerm: NetworkVariableWritePermission.Server);

    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject nameTagRoot;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            string myName = UserListManager.Singleton.localUserName;
            UpdateNameServerRpc(myName);
            nameTagRoot.SetActive(false);
        }
        else
        {
            UpdateNameVisuals(playerName.Value.ToString());
        }

        playerName.OnValueChanged += OnNameChanged;
    }

    [ServerRpc]
    private void UpdateNameServerRpc(string name)
    {
        playerName.Value = name;
    }

    private void OnNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        UpdateNameVisuals(newValue.ToString());
    }

    private void UpdateNameVisuals(string newName)
    {
        if (string.IsNullOrEmpty(newName)) return;
        if (nameText != null) nameText.text = newName;
    }

    private void OnDestroy()
    {
        playerName.OnValueChanged -= OnNameChanged;
    }
}