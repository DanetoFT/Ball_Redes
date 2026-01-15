using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkData : NetworkBehaviour
{
    public NetworkVariable<FixedString64Bytes> playerName =
        new NetworkVariable<FixedString64Bytes>(
            writePerm: NetworkVariableWritePermission.Owner);

    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject nameTagRoot;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            playerName.Value = UserListManager.Singleton.localUserName;
        }

        if (IsOwner)
        {
            nameTagRoot.SetActive(false);
        }
        else
        {
            UpdateName(playerName.Value.ToString());
        }

        playerName.OnValueChanged += OnNameChanged;
    }

    private void OnDestroy()
    {
        playerName.OnValueChanged -= OnNameChanged;
    }

    private void OnNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        UpdateName(newValue.ToString());
    }

    private void UpdateName(string newName)
    {
        if (string.IsNullOrEmpty(newName)) return;
        nameText.text = newName;
    }
}
