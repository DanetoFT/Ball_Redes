using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class BallNetwork : NetworkBehaviour
{
    private Rigidbody rb;
    private NetworkObject netObj;

    [Header("Respawn por Vacío")]
    public float voidYThreshold = -10f;
    public float tiempoRespawn = 2f;
    private bool isRespawning = false;
    private ulong lastOwnerId;

    [Header("Respawn por Inactividad")]
    public float inactivityTimeout = 10f;     // ← NUEVO: Tiempo sin tocarla para respawn auto
    private float lastActivityTime;           // ← NUEVO: Última vez que se Hold/Release

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
    public ulong GetLastOwnerId() => lastOwnerId;

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
            lastActivityTime = Time.time;  // ← Inicial
        }
    }

    void Update()
    {
        if (!IsServer) return;

        // Si está sostenida, seguir
        if (isHeld.Value && followTarget != null)
        {
            rb.MovePosition(followTarget.position);
            rb.MoveRotation(followTarget.rotation);
            return;
        }

        // ← PRIORIDAD 1: Respawn por vacío (como antes)
        if (transform.position.y < voidYThreshold && !isRespawning)
        {
            StartCoroutine(RespawnRoutine("Vacío"));
            return;
        }

        // ← NUEVO: PRIORIDAD 2: Respawn por inactividad
        if (!isHeld.Value && Time.time - lastActivityTime > inactivityTimeout && !isRespawning)
        {
            StartCoroutine(RespawnRoutine("Inactividad"));
            return;
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

        lastActivityTime = Time.time;  // ← RESET TIMER
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

        lastActivityTime = Time.time;  // ← RESET TIMER
    }

    public void SetLastOwner(ulong clientId)
    {
        if (!IsServer) return;
        lastOwnerId = clientId;
    }

    // ← MEJORADO: RespawnRoutine con parámetro para log (opcional)
    private IEnumerator RespawnRoutine(string reason = "Desconocido")
    {
        isRespawning = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        yield return new WaitForSeconds(tiempoRespawn);

        Vector3 targetPosition = new Vector3(0, 5, 0);
        ulong usedId = lastOwnerId;
        bool success = false;
        NetworkClient client = null;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(lastOwnerId, out client))
        {
            if (client.PlayerObject != null && client.PlayerObject.IsSpawned)
            {
                success = true;
            }
        }

        if (!success)
        {
            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                NetworkClient tempClient = kvp.Value;
                if (tempClient.PlayerObject != null && tempClient.PlayerObject.IsSpawned)
                {
                    usedId = kvp.Key;
                    client = tempClient;
                    success = true;
                    break;
                }
            }
        }

        if (success)
        {
            Transform playerT = client.PlayerObject.transform;
            Vector3 rayOrigin = playerT.position + playerT.forward * respawnForwardDistance + Vector3.up * raycastHeight;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastMaxDistance, groundMask))
            {
                targetPosition = hit.point + Vector3.up * spawnAboveGroundOffset;
            }
            else
            {
                targetPosition = playerT.position + Vector3.up * fallbackHeight;
            }

            Debug.Log($"[Ball Respawn {reason}] ÉXITO cerca de {usedId}");
        }
        else
        {
            Debug.LogWarning($"[Ball Respawn {reason}] FALLBACK centro: No hay jugadores válidos");
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
        lastActivityTime = Time.time;  // ← Reset al respawnear
    }
}