using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class FirstPersonMovement : NetworkBehaviour
{
    public float speed = 5f;
    public float runSpeed = 9f;
    public KeyCode runningKey = KeyCode.LeftShift;

    Rigidbody rb;
    PauseMenuManager pause;
    private NetworkObject myNetworkObject;

    public bool IsRunning { get; private set; }

    // 👇 VUELVE ESTO
    public List<System.Func<float>> speedOverrides = new();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pause = FindAnyObjectByType<PauseMenuManager>();
        myNetworkObject = GetComponent<NetworkObject>();
    }

    void FixedUpdate()
    {
        if (NetworkManager.Singleton.LocalClientId != myNetworkObject.OwnerClientId || pause.isPaused)
        {
            return;
        }

        IsRunning = Input.GetKey(runningKey);
        float currentSpeed = IsRunning ? runSpeed : speed;

        // 👇 aplica override si existe
        if (speedOverrides.Count > 0)
        {
            currentSpeed = speedOverrides[^1]();
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = transform.forward * v + transform.right * h;
        move *= currentSpeed;

        rb.linearVelocity = new Vector3(
            move.x,
            rb.linearVelocity.y,
            move.z
        );
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log(
            $"Spawned | IsOwner: {IsOwner} | IsServer: {IsServer} | ClientId: {OwnerClientId}"
        );
    }

}
