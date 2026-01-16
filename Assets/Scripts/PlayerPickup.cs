using Unity.Netcode;
using UnityEngine;

public class PlayerPickup : NetworkBehaviour
{
    [Header("Configuración")]
    public Transform holdPoint;
    public float pickUpRange = 3f;
    public float throwForce = 15f;
    public LayerMask ballLayer;

    [Header("Teclas")]
    public KeyCode grabKey = KeyCode.E;
    public KeyCode throwKey = KeyCode.Mouse0;

    private NetworkObject heldObject;

    void Update()
    {
        if (!IsOwner) return;

        if (heldObject == null)
        {
            if (Input.GetKeyDown(grabKey))
            {
                TryGrabBall();
            }
        }
        else
        {
            if (Input.GetKeyDown(throwKey))
            {
                ThrowBallServerRpc(Camera.main.transform.forward * throwForce);
            }
        }
    }

    private void TryGrabBall()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, pickUpRange, ballLayer))
        {
            if (hit.collider.TryGetComponent(out NetworkObject netObj))
            {
                RequestGrabServerRpc(netObj.NetworkObjectId);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestGrabServerRpc(ulong networkObjectId, ServerRpcParams rpcParams = default)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(networkObjectId, out NetworkObject ballNetObj))
            return;

        if (!ballNetObj.TryGetComponent<BallNetwork>(out BallNetwork ball))
            return;

        // Evitar doble agarre
        if (ball.isHeld.Value) return;

        ulong grabberId = rpcParams.Receive.SenderClientId;

        // NO cambiamos ownership de la pelota (queda server-owned)
        ball.SetLastOwner(grabberId);
        ball.Hold(holdPoint);

        // Guardamos referencia en el jugador que pidió el agarre
        heldObject = ballNetObj;

        // Si es el host (LocalClientId == 0), actualizamos localmente también
        if (grabberId == NetworkManager.Singleton.LocalClientId)
        {
            heldObject = ballNetObj;
        }

        // Enviamos solo al cliente que pidió (no broadcast)
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { grabberId }
            }
        };

        SetHeldObjectClientRpc(networkObjectId, clientRpcParams);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ThrowBallServerRpc(Vector3 force, ServerRpcParams rpcParams = default)
    {
        ulong throwerId = rpcParams.Receive.SenderClientId;

        // Si heldObject es null, intentamos recuperarlo (medida de seguridad)
        if (heldObject == null)
        {
                foreach (var spawned in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
                {
                    if (spawned.TryGetComponent<BallNetwork>(out var fallbackBall) && fallbackBall.isHeld.Value)
                    {
                        heldObject = spawned;
                        break;
                    }
                }

            if (heldObject == null)
            {
                Debug.LogWarning($"[Throw] No se encontró pelota para jugador {throwerId}");
                return;
            }
        }

        if (heldObject.TryGetComponent<BallNetwork>(out BallNetwork ball))
        {
            ball.Release(force);
        }

        heldObject = null;

        // Limpiamos solo en el cliente que lanzó
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { throwerId }
            }
        };

        ClearHeldObjectClientRpc(clientRpcParams);
    }

    [ClientRpc]
    private void SetHeldObjectClientRpc(ulong netId, ClientRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(netId, out NetworkObject obj))
        {
            heldObject = obj;
        }
    }

    [ClientRpc]
    private void ClearHeldObjectClientRpc(ClientRpcParams rpcParams = default)
    {
        heldObject = null;
    }
}