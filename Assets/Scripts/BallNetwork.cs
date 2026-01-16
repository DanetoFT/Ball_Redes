using System.Collections;
using TMPro;
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
        }
        gameObject.SetActive(true);
    }

    void Update()
    {
        // Bloque principal: si está sostenida, TODOS (incluidos clientes) siguen el holdPoint localmente
        if (isHeld.Value && followTarget != null)
        {
            // Predicción suave en clientes + server
            transform.position = followTarget.position;
            transform.rotation = followTarget.rotation;

            // Si es server → también mueve el Rigidbody (para física correcta)
            if (IsServer)
            {
                rb.MovePosition(followTarget.position);
                rb.MoveRotation(followTarget.rotation);
                // Opcional: rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; (ya lo tienes en Hold)
            }

            return;  // Salimos aquí, no hacemos respawns mientras está held
        }

        // Solo el servidor maneja respawns y lógica física cuando no está held
        if (!IsServer) return;

        // Respawn por vacío
        if (transform.position.y < voidYThreshold && !isRespawning)
        {
            StartCoroutine(RespawnRoutine("Vacío"));
            return;
        }

        // Respawn por inactividad
        if (Time.time - lastActivityTime > inactivityTimeout && !isRespawning)
        {
            StartCoroutine(RespawnRoutine("Inactividad"));
            return;
        }
    }

    public void Hold(Transform target)
    {
        if (!IsServer) return;
        Debug.Log("[Hold] Llamado en SERVER - followTarget asignado");
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
        Debug.Log($"[Release] Llamado en SERVER | isHeld antes: {isHeld.Value} | kinematic: {rb.isKinematic} | force: {throwForce.magnitude}");
        isHeld.Value = false;
        followTarget = null;

        if (TryGetComponent(out Collider col))
            col.enabled = true;

        // Orden crítico:
        rb.isKinematic = false;               // Primero dynamic
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;           // Limpieza segura
        rb.angularVelocity = Vector3.zero;

        rb.AddForce(throwForce, ForceMode.Impulse);  // Ahora sí aplica fuerza

        lastActivityTime = Time.time;
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

        // 1. Primero pon kinematic y para velocidades (orden seguro)
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        yield return new WaitForSeconds(tiempoRespawn);

        // DECLARACIÓN OBLIGATORIA: fallback inicial
        Vector3 targetPosition = new Vector3(0, 5, 0); // Centro si no hay jugadores

        ulong usedId = lastOwnerId;
        bool success = false;
        NetworkClient client = null;

        // Intentamos con el último dueño
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(lastOwnerId, out client))
        {
            if (client.PlayerObject != null && client.PlayerObject.IsSpawned)
            {
                success = true;
            }
        }

        // Si no se encontró → cualquier jugador vivo
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

            Debug.Log($"[Ball Respawn {reason}] ÉXITO cerca de jugador {usedId}");
        }
        else
        {
            Debug.LogWarning($"[Ball Respawn {reason}] FALLBACK al centro: No hay jugadores válidos");
        }

        // Teleport usando la variable ya calculada
        if (TryGetComponent(out NetworkTransform netTransform))
        {
            netTransform.Teleport(targetPosition, Quaternion.identity, transform.localScale);
        }
        else
        {
            transform.position = targetPosition;
            transform.rotation = Quaternion.identity;
        }

        // 2. Volver a dinámica
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;           // Limpieza final

        isRespawning = false;
        lastActivityTime = Time.time;

        Debug.Log($"[Ball Respawn {reason}] Completado - kinematic desactivado");
    }
}