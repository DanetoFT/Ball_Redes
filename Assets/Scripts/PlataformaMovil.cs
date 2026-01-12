using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class PlataformaMovil : NetworkBehaviour
{
    public enum ModoDesplazamiento
    {
        horizontalZ,
        horizontalX,
        vertical
    }

    [SerializeField] ModoDesplazamiento modo;
    [SerializeField] float distancia;
    [SerializeField] Transform puntoA;
    [SerializeField] Transform puntoB;
    [SerializeField] float velocidad = 1f;
    [SerializeField] float tiempoDePausa = 0.5f;

    private float movementAlpha;
    private bool puntoAHaciaB = true;
    private bool enPausa;
    private float temporizadorPausa;

    private NetworkVariable<bool> activated = new(false);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            movementAlpha = 0f;
        }
    }

    void Update()
    {
        if (!IsServer) return;
        if (!activated.Value) return;

        if (enPausa)
        {
            temporizadorPausa += Time.deltaTime;
            if (temporizadorPausa >= tiempoDePausa)
            {
                enPausa = false;
                temporizadorPausa = 0f;
                puntoAHaciaB = !puntoAHaciaB;
                movementAlpha = 0f;
            }
            return;
        }

        transform.position = Vector3.Lerp(
            puntoAHaciaB ? puntoA.position : puntoB.position,
            puntoAHaciaB ? puntoB.position : puntoA.position,
            movementAlpha
        );

        movementAlpha += Time.deltaTime * velocidad;

        if (movementAlpha >= 1f)
        {
            movementAlpha = 1f;
            enPausa = true;
        }
    }

    private void OnValidate()
    {
        if (modo == ModoDesplazamiento.horizontalZ)
        {
            puntoB.position = transform.position + new Vector3(0, 0,  distancia / 2);
            puntoA.position = transform.position - new Vector3(0, 0, distancia / 2);
        }
        else if (modo == ModoDesplazamiento.horizontalX)
        {
            puntoB.position = transform.position + new Vector3(distancia / 2, 0, 0);
            puntoA.position = transform.position - new Vector3(distancia / 2, 0, 0);
        }
        else if (modo == ModoDesplazamiento.vertical)
        {
            puntoB.position = transform.position + new Vector3(0, distancia / 2, 0);
            puntoA.position = transform.position - new Vector3(0, distancia / 2, 0);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetActivatedServerRpc(bool value)
    {
        activated.Value = value;
    }
}
