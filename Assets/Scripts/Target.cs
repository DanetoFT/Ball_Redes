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

        closedPosition = doorObject.transform.localPosition;

        if (!doorObject.TryGetComponent(out doorNetTransform))
        {
            Debug.LogError("¡El 'doorObject' necesita un NetworkTransform para sincronizar el movimiento!");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if (!collision.collider.CompareTag("Ball")) return;

        if (oneTimeOnly && alreadyHit) return;
        alreadyHit = true;

        ToggleDoor();

        PlayDoorEffectClientRpc();
    }

    private void ToggleDoor()
    {
        if (doorObject == null) return;

        bool isOpen = doorObject.transform.localPosition.y > closedPosition.y + openHeight * 0.5f;
        Vector3 targetPos = isOpen ? closedPosition : closedPosition + Vector3.up * openHeight;

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
            t = Mathf.SmoothStep(0f, 1f, t);

            doorTransform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        doorTransform.localPosition = endPos;
    }

    [ClientRpc]
    private void PlayDoorEffectClientRpc()
    {
        Debug.Log("¡Puerta futurista abriéndose/cerrándose!");
    }
}