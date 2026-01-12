using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class NetworkBall : NetworkBehaviour
{
    Rigidbody rb;
    public float voidYThreshold = -10f; // Altura a la que se considera "vacío"
    private ulong lastOwnerId;
    private bool isRespawning = false;
    private NetworkObject netObj;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        netObj = GetComponent<NetworkObject>();
    }

    public override void OnNetworkSpawn()
    {
        // El servidor inicializa el ID del dueño (por defecto el host o nadie)
        if (IsServer)
        {
            lastOwnerId = NetworkManager.Singleton.LocalClientId;
        }
    }

    void Update()
    {
        // Solo el servidor vigila si la pelota cae al vacío
        if (!IsServer) return;

        // Si la pelota cae muy bajo y no está siendo sostenida (tiene padre) ni está respawneando
        if (transform.position.y < voidYThreshold && transform.parent == null && !isRespawning)
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    // Método llamado por el Player cuando la agarra
    public void SetLastOwner(ulong clientId)
    {
        lastOwnerId = clientId;
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;

        // 1. Detener físicas por completo
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        yield return new WaitForSeconds(2f);

        Vector3 targetPosition;
        // 2. Definir rotación limpia (sin rotación)
        Quaternion targetRotation = Quaternion.identity;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(lastOwnerId, out NetworkClient client))
        {
            if (client.PlayerObject != null)
            {
                // Posición: 2 metros sobre el jugador
                targetPosition = client.PlayerObject.transform.position + Vector3.up * 2f;
            }
            else
            {
                targetPosition = new Vector3(0, 5, 0);
            }
        }
        else
        {
            targetPosition = new Vector3(0, 5, 0);
        }

        // 3. TELETRANSPORTE OFICIAL (Esto evita el "desplazamiento" extraño)
        if (TryGetComponent(out NetworkTransform netTransform))
        {
            // Teleport es la forma correcta de mover objetos con NetworkTransform instantáneamente
            netTransform.Teleport(targetPosition, targetRotation, transform.localScale);
        }
        else
        {
            // Si por alguna razón no tienes NetworkTransform, el cambio manual:
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }

        // 4. Reactivar físicas
        rb.isKinematic = false;
        isRespawning = false;
    }
}