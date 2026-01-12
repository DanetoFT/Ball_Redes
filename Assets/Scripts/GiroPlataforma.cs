using Unity.Netcode;
using UnityEngine;

public class GiroPlataforma : MonoBehaviour
{
    [SerializeField] private GameObject plataformaGiratoria;
    [SerializeField] private float velocidadRotacion = 5f;
    public bool spinning;
    private void Update()
    {
        if (spinning)
        {
            plataformaGiratoria.transform.Rotate(Vector3.up * velocidadRotacion * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            spinning = true;
            UpdatePlatformStatusServerRPC(spinning);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            spinning = false;
            UpdatePlatformStatusServerRPC(spinning);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            spinning = true;
            UpdatePlatformStatusServerRPC(spinning);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdatePlatformStatusServerRPC(bool girando)
    {
        UpdatePlatformStatusClientRPC(girando);
    }

    [ClientRpc]
    private void UpdatePlatformStatusClientRPC(bool girando)
    {
        if (girando)
        {
            plataformaGiratoria.transform.Rotate(Vector3.up * velocidadRotacion * Time.deltaTime);
        }
    }
}
