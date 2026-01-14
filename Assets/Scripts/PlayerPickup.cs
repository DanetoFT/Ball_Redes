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
                TryGrabBall();
        }
        else
        {
            if (Input.GetKeyDown(throwKey))
                ThrowBallServerRpc(Camera.main.transform.forward * throwForce);
        }
    }

    void TryGrabBall()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickUpRange, ballLayer))
        {
            if (hit.collider.TryGetComponent(out NetworkObject netObj))
            {
                RequestGrabServerRpc(netObj.NetworkObjectId);
            }
        }
    }

    [ServerRpc]
    void RequestGrabServerRpc(ulong networkObjectId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(networkObjectId, out NetworkObject ballNetObj))
            return;

        BallNetwork ball = ballNetObj.GetComponent<BallNetwork>();
        if (ball == null) return;

        // Evitar que dos jugadores la cojan
        if (ball.isHeld.Value) return;

        ball.SetLastOwner(OwnerClientId);
        ballNetObj.ChangeOwnership(OwnerClientId);

        ball.Hold(holdPoint);

        heldObject = ballNetObj;
        SetHeldObjectClientRpc(networkObjectId);
    }

    [ServerRpc]
    void ThrowBallServerRpc(Vector3 force)
    {
        if (heldObject == null) return;

        BallNetwork ball = heldObject.GetComponent<BallNetwork>();
        if (ball != null)
        {
            ball.Release(force);
        }

        heldObject = null;
        ClearHeldObjectClientRpc();
    }

    [ClientRpc]
    void SetHeldObjectClientRpc(ulong netId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(netId, out NetworkObject obj))
        {
            heldObject = obj;
        }
    }

    [ClientRpc]
    void ClearHeldObjectClientRpc()
    {
        heldObject = null;
    }
}
