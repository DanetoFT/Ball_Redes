using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class BallNetwork : NetworkBehaviour
{
    private Rigidbody rb;
    private NetworkObject netObj;

    [Header("Respawn")]
    public float voidYThreshold = -10f;
    public float tiempoRespawn = 2f;
    private bool isRespawning = false;
    private ulong lastOwnerId;

    [Header("Posición Respawn")]
    public float respawnForwardDistance = 1.5f;
    [SerializeField] private LayerMask groundMask = -1;
    [SerializeField] private float raycastHeight = 10f;
    [SerializeField] private float raycastMaxDistance = 20f;
    [SerializeField] private float spawnAboveGroundOffset = 0.1f;
    public float fallbackHeight = 2f;

    [Header("Hold")]
    public NetworkVariable<bool> isHeld = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private Transform followTarget;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        netObj = GetComponent<NetworkObject>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            lastOwnerId = NetworkManager.Singleton.LocalClientId;
        }
    }

    void Update()
    {
        if (!IsServer) return;

        if (isHeld.Value && followTarget != null)
        {
            rb.MovePosition(followTarget.position);
            rb.MoveRotation(followTarget.rotation);
            return;
        }

        if (transform.position.y < voidYThreshold && !isRespawning)
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    public void Hold(Transform target)
    {
        if (!IsServer) return;
        followTarget = target;
        isHeld.Value = true;
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        if (TryGetComponent(out Collider col))
            col.enabled = false;
    }

    public void Release(Vector3 throwForce)
    {
        if (!IsServer) return;
        isHeld.Value = false;
        followTarget = null;
        if (TryGetComponent(out Collider col))
            col.enabled = true;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.AddForce(throwForce, ForceMode.Impulse);
    }

    public void SetLastOwner(ulong clientId)
    {
        if (!IsServer) return;
        lastOwnerId = clientId;
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        yield return new WaitForSeconds(tiempoRespawn);

        Vector3 targetPosition = new Vector3(0, 5, 0); // fallback final
        bool success = false;
        ulong usedId = lastOwnerId;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(lastOwnerId, out NetworkClient client))
        {
            // Chequeo más seguro: PlayerObject existe y está spawned
            if (client.PlayerObject != null && client.PlayerObject.IsSpawned)
            {
                success = true;
            }
            else
            {
                Debug.LogWarning($"[Ball] Cliente {lastOwnerId} encontrado pero PlayerObject es NULL o no spawned");
            }
        }
        else
        {
            Debug.LogWarning($"[Ball] No se encontró cliente con ID {lastOwnerId}");
        }

        // Si falló con el último dueño → elige cualquier jugador vivo (incluye Host)
        if (!success)
        {
            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                client = kvp.Value;
                if (client.PlayerObject != null && client.PlayerObject.IsSpawned)
                {
                    usedId = kvp.Key;
                    success = true;
                    break;
                }
            }
        }

        if (success)
        {
            Transform playerT = client.PlayerObject.transform;

            Vector3 rayOrigin = playerT.position +
                               playerT.forward * respawnForwardDistance +
                               Vector3.up * raycastHeight;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastMaxDistance, groundMask))
            {
                targetPosition = hit.point + Vector3.up * spawnAboveGroundOffset;
            }
            else
            {
                targetPosition = playerT.position + Vector3.up * fallbackHeight;
            }

            Debug.Log($"[Ball Respawn] ÉXITO - Cerca de jugador {usedId} {(usedId == lastOwnerId ? "(último)" : "(fallback otro)")}");
        }
        else
        {
            Debug.LogWarning("[Ball Respawn] FALLBACK centro: NO hay jugadores vivos con PlayerObject válido");
        }

        // Teleport
        if (TryGetComponent(out NetworkTransform netTransform))
        {
            netTransform.Teleport(targetPosition, Quaternion.identity, transform.localScale);
        }
        else
        {
            transform.position = targetPosition;
            transform.rotation = Quaternion.identity;
        }

        rb.isKinematic = false;
        isRespawning = false;
    }
}