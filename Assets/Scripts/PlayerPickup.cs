using Unity.Netcode;
using UnityEngine;

public class PlayerPickup : NetworkBehaviour
{
    [Header("Configuración")]
    public Transform holdPoint; // Crea un objeto vacío hijo de la cámara donde irá la mano
    public float pickUpRange = 3f;
    public float throwForce = 15f;
    public LayerMask ballLayer; // Asigna esto a una capa donde esté solo la pelota

    private bool isGrabbed = false;

    [Header("Teclas")]
    public KeyCode grabKey = KeyCode.E;
    public KeyCode throwKey = KeyCode.Mouse0;

    private NetworkObject heldObject;

    public PauseMenuManager referencePauseMenu;

    void Update()
    {
        if (!IsOwner) return;

        /*if (referencePauseMenu.isPaused)
        {
            return;
        }*/

        if (heldObject == null)
        {
            // Intentar agarrar
            if (Input.GetKeyDown(grabKey))
            {
                TryGrabBall();
            }
        }
        else
        {
            heldObject.transform.position = holdPoint.transform.position;
            // Intentar lanzar
            if (Input.GetKeyDown(throwKey))
            {
                ThrowBallServerRpc(Camera.main.transform.forward * throwForce);
            }
        }
    }

    void TryGrabBall()
    {
        // Creamos el rayo desde el centro de la cámara
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickUpRange, ballLayer))
        {
            Debug.Log("Raycast impactó con: " + hit.collider.name);
            if (hit.collider.TryGetComponent(out NetworkObject netObj))
            {
                Debug.Log("Enviando petición de agarre al servidor...");
                RequestGrabServerRpc(netObj.NetworkObjectId);
            }
            else
            {
                Debug.LogError("El objeto impactado no tiene NetworkObject");
            }
        }
        else
        {
            Debug.Log("El Raycast no detectó nada en el rango o capa correcta.");
        }
    }

    [ServerRpc]
    void RequestGrabServerRpc(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject ballNetObj))
        {
            // 1. Autoridad: El servidor le da el permiso al cliente
            ballNetObj.ChangeOwnership(OwnerClientId);

            // 2. Desactivar físicas completamente
            var ballRb = ballNetObj.GetComponent<Rigidbody>();
            if (ballRb)
            {
                ballRb.isKinematic = true;
                ballRb.useGravity = false;
                ballRb.linearVelocity = Vector3.zero;
            }

            // 3. INTENTAR PARENTING
            // El segundo parámetro 'false' evita que Unity intente mantener la posición mundial
            bool success = ballNetObj.TrySetParent(holdPoint, true);

            if (success)
            {
                Debug.Log("Parenting exitoso en el servidor");
                // Forzamos la posición local a cero para que se pegue al HoldPoint
                ballNetObj.transform.localPosition = Vector3.zero;
                ballNetObj.transform.localRotation = Quaternion.identity;
}
            else
            {
                Debug.LogError($"Fallo al emparentar la pelota. ¿Tiene el Player un NetworkObject activo? ID: {OwnerClientId}");
            }

            heldObject = ballNetObj;
            SetHeldObjectClientRpc(networkObjectId);
        }
    }

    [ServerRpc]
    void ThrowBallServerRpc(Vector3 force)
    {
        if (heldObject != null)
        {
            // 1. Soltar
            heldObject.TrySetParent((Transform)null);

            // 2. Reactivar TODO
            var ballRb = heldObject.GetComponent<Rigidbody>();
            if (ballRb)
            {
                ballRb.isKinematic = false;
                ballRb.useGravity = true; // Aseguramos que caiga

                // Aplicamos la fuerza
                ballRb.AddForce(force, ForceMode.Impulse);
            }

            // 3. Limpiar
            heldObject = null;
            ClearHeldObjectClientRpc(); // Ya no necesitas pasar el ID si solo puedes tener una
        }
    }

    [ClientRpc]
    void SetHeldObjectClientRpc(ulong netId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out NetworkObject obj))
        {
            heldObject = obj;

            SphereCollider ballCol = heldObject.GetComponent<SphereCollider>();
            ballCol.isTrigger = true;
            isGrabbed = true;
        }
    }

    [ClientRpc]
    void ClearHeldObjectClientRpc()
    {
        SphereCollider ballCol = heldObject.GetComponent<SphereCollider>();

        ballCol.isTrigger = false;
        heldObject = null;
        isGrabbed = false;
    }
}