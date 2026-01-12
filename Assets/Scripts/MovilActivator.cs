using Unity.Netcode;
using UnityEngine;

public class MovilActivator : NetworkBehaviour
{
    [SerializeField] private PlataformaMovil plataforma;

    private int playersInside = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playersInside++;
        plataforma.SetActivatedServerRpc(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playersInside--;
        if (playersInside <= 0)
        {
            playersInside = 0;
            plataforma.SetActivatedServerRpc(false);
        }
    }
}
