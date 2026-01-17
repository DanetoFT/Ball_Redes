using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class TargetTrigger : NetworkBehaviour
{
    [Header("Objeto a Rotar")]
    [SerializeField] private Transform objectToRotate;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float rotationAngle = 90f;
    [SerializeField] private float rotationDuration = 1.2f;

    [Header("Comportamiento")]
    [SerializeField] private bool oneTimeOnly = false;
    [SerializeField] private bool toggleDirection = true;

    private bool alreadyTriggered = false;
    private bool currentDirectionPositive = true;
    private NetworkTransform netTransform;

    public override void OnNetworkSpawn()
    {
        if (objectToRotate == null)
        {
            Debug.LogError("¡Asigna el objeto a rotar!");
            return;
        }

        if (!objectToRotate.TryGetComponent(out netTransform))
        {
            Debug.LogError("¡El objeto a rotar necesita NetworkTransform!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (!other.CompareTag("Ball")) return;

        if (oneTimeOnly && alreadyTriggered) return;
        alreadyTriggered = true;

        if (toggleDirection)
            currentDirectionPositive = !currentDirectionPositive;

        float angle = rotationAngle * (currentDirectionPositive ? 1f : -1f);

        StartCoroutine(RotateSmoothly(angle));

        PlayEffectClientRpc();
    }

    private IEnumerator RotateSmoothly(float angle)
    {
        Quaternion startRotation = objectToRotate.localRotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(rotationAxis * angle);

        float elapsed = 0f;

        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rotationDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            objectToRotate.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
            yield return null;
        }

        objectToRotate.localRotation = endRotation;
    }

    [ClientRpc]
    private void PlayEffectClientRpc()
    {
        Debug.Log("¡Objeto rotando! → Efectos aquí");
    }
}