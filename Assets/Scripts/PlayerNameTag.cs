using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Linq;

public class PlayerNameTag : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject nameTagRoot;

    public override void OnNetworkSpawn()
    {
        // Si soy yo ? oculto mi nombre
        if (IsOwner)
        {
            nameTagRoot.SetActive(false);
            return;
        }

        TrySetName();

        UserListManager.OnUserListUpdated += TrySetName;
    }

    private void OnDestroy()
    {
        UserListManager.OnUserListUpdated -= TrySetName;
    }

    private void TrySetName()
    {
        if (nameText.text != "") return;
        if (UserListManager.Singleton == null) return;

        var userData = UserListManager.Singleton.userConnectedList
            .FirstOrDefault(u => u.userId == OwnerClientId);

        if (userData != null)
        {
            nameText.text = userData.userName;
            Debug.Log($"Trying set name for {OwnerClientId}");
        }
    }
}
