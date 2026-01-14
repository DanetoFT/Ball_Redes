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

    private bool isRespawning = false;
    private ulong lastOwnerId;

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

        // Si está en la mano de un jugador, seguir el HoldPoint
        if (isHeld.Value && followTarget != null)
        {
            rb.MovePosition(followTarget.position);
            rb.MoveRotation(followTarget.rotation);
            return;
        }

        // Respawn si cae al vacío
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

        yield return new WaitForSeconds(2f);

        Vector3 targetPosition;
        Quaternion targetRotation = Quaternion.identity;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(lastOwnerId, out NetworkClient client)
            && client.PlayerObject != null)
        {
            targetPosition = client.PlayerObject.transform.position + Vector3.up * 2f;
        }
        else
        {
            targetPosition = new Vector3(0, 5, 0);
        }

        if (TryGetComponent(out NetworkTransform netTransform))
        {
            netTransform.Teleport(targetPosition, targetRotation, transform.localScale);
        }
        else
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }

        rb.isKinematic = false;
        isRespawning = false;
    }
}
