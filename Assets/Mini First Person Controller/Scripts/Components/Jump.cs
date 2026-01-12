using UnityEngine;
using Unity.Netcode; // Necesario para NetworkBehaviour

// Cambiamos a NetworkBehaviour
public class Jump : NetworkBehaviour
{
    Rigidbody rigidbody;
    public float jumpStrength = 2;
    public event System.Action Jumped;

    [SerializeField] GroundCheck groundCheck;

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    void Update() // Input suele ir mejor en Update que en LateUpdate
    {
        // CRÍTICO: Solo el dueño de este personaje puede hacerlo saltar
        if (!IsOwner) return;

        if (Input.GetButtonDown("Jump"))
        {
            // Verificación de suelo
            bool isGrounded = groundCheck == null || groundCheck.isGrounded;

            if (isGrounded)
            {
                // Resetear velocidad Y para salto consistente
                Vector3 vel = rigidbody.linearVelocity; // o .velocity en versiones viejas
                vel.y = 0;
                rigidbody.linearVelocity = vel;

                rigidbody.AddForce(Vector3.up * 100 * jumpStrength);
                Jumped?.Invoke();
            }
        }
    }
}