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
    public float inactivityTimeout = 10f;
    private float lastActivityTime;

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

    // Offset fijo para la posición relativa (ajusta estos valores)
    [SerializeField] private Vector3 holdOffset = new Vector3(0.87f, 0.35f, 0.44f); // Derecha, arriba, delante

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
            lastActivityTime = Time.time;
        }
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (isHeld.Value && followTarget != null)
        {
            // Predicción fuerte en clientes: fuerza posición/rotación cada frame
            transform.position = followTarget.position + followTarget.rotation * holdOffset;
            transform.rotation = followTarget.rotation;

            // Limpieza física en todos
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            if (TryGetComponent(out Collider col))
                col.enabled = false;

            if (IsServer)
            {
                rb.MovePosition(transform.position);
                rb.MoveRotation(transform.rotation);
            }

            return;
        }

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

    public void Hold(Transform holdPointTransform)
    {
        if (!IsServer) return;

        // Chequea si holdPoint tiene NetworkObject
        if (!holdPointTransform.TryGetComponent<NetworkObject>(out var holdPointNetObj))
        {
            Debug.LogError("HoldPoint debe tener NetworkObject para parenting");
            return;
        }

        followTarget = holdPointTransform;
        isHeld.Value = true;

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (TryGetComponent(out Collider col))
            col.enabled = false;

        // Parenting con NGO
        if (!TryGetComponent<NetworkObject>(out var ballNetObj))
        {
            Debug.LogError("Pelota debe tener NetworkObject");
            return;
        }

        if (!ballNetObj.TrySetParent(holdPointTransform, worldPositionStays: false))
        {
            Debug.LogError("Parenting falló");
            return;
        }

        // Reset local para pegar al holdPoint
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        lastActivityTime = Time.time;
    }

    public void Release(Vector3 throwForce)
    {
        if (!IsServer) return;

        isHeld.Value = false;
        followTarget = null;

        // Quitar parentesco (mantiene posición mundial actual)
        transform.SetParent(null, true);

        if (TryGetComponent(out Collider col))
            col.enabled = true;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.AddForce(throwForce, ForceMode.Impulse);

        lastActivityTime = Time.time;
    }

    public void SetLastOwner(ulong clientId)
    {
        if (!IsServer) return;
        lastOwnerId = clientId;
    }

    private IEnumerator RespawnRoutine(string reason = "Desconocido")
    {
        isRespawning = true;

        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

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
        }

        if (TryGetComponent(out NetworkTransform netTransform))
        {
            netTransform.Teleport(targetPosition, Quaternion.identity, transform.localScale);
        }
        else
            transform.position = targetPosition;
        transform.rotation = Quaternion.identity;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;

        isRespawning = false;
        lastActivityTime = Time.time;
    }
}