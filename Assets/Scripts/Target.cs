using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Target : NetworkBehaviour
{
    [Header("Puerta Futurista")]
    [SerializeField] private GameObject doorObject;         
    [SerializeField] private float openHeight = 3f;         
    [SerializeField] private float animationTime = 1f;      

    [Header("Comportamiento")]
    [SerializeField] private bool oneTimeOnly = false;

    private Vector3 closedPosition;
    private bool alreadyHit = false;
    private NetworkTransform doorNetTransform;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (doorObject == null)
        {
            Debug.LogError("¡Asigna un GameObject a 'doorObject' en el inspector!");
            return;
        }

        // Guardar posición cerrada (usa localPosition para que sea relativa al padre)
        closedPosition = doorObject.transform.localPosition;

        // Obtener NetworkTransform de la puerta (¡IMPORTANTE: ponle uno al doorObject!)
        if (!doorObject.TryGetComponent(out doorNetTransform))
        {
            Debug.LogError("¡El 'doorObject' necesita un NetworkTransform para sincronizar el movimiento!");
        }
    }

    // ¡CAMBIO PRINCIPAL: Ahora usa OnCollisionEnter para colisión física real!
    private void OnCollisionEnter(Collision collision)
    {
        // Solo el servidor procesa la lógica importante
        if (!IsServer) return;

        // Verificamos que sea nuestra pelota (usa collision.collider)
        if (!collision.collider.CompareTag("Ball")) return;
        // O más robusto: if (!collision.collider.TryGetComponent<BallNetwork>(out _)) return;

        if (oneTimeOnly && alreadyHit) return;
        alreadyHit = true;

        // ¡Abrir/cerrar la puerta!
        ToggleDoor();

        // Efectos visuales/sonoros en TODOS los clientes
        PlayDoorEffectClientRpc();
    }

    private void ToggleDoor()
    {
        if (doorObject == null) return;

        // Detectar estado actual (aprox.)
        bool isOpen = doorObject.transform.localPosition.y > closedPosition.y + openHeight * 0.5f;
        Vector3 targetPos = isOpen ? closedPosition : closedPosition + Vector3.up * openHeight;

        // Animación suave
        StartCoroutine(AnimateDoor(doorObject.transform.localPosition, targetPos));
    }

    private IEnumerator AnimateDoor(Vector3 startPos, Vector3 endPos)
    {
        float elapsed = 0f;
        Transform doorTransform = doorObject.transform;

        while (elapsed < animationTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationTime;
            t = Mathf.SmoothStep(0f, 1f, t);  // Curva suave (ease in-out)

            doorTransform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        // Asegurar posición final exacta
        doorTransform.localPosition = endPos;
    }

    [ClientRpc]
    private void PlayDoorEffectClientRpc()
    {
        Debug.Log("¡Puerta futurista abriéndose/cerrándose!");
    }
}