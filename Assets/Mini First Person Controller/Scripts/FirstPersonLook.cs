using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class FirstPersonLook : NetworkBehaviour
{
    public float sensitivity = 5f;
    public float minY = -80f;
    public float maxY = 80f;

    float xRotation;
    Transform player;
    PauseMenuManager pauseMenu;

    void Start()
    {
        if (!IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }

        player = transform.root;
        pauseMenu = FindObjectOfType<PauseMenuManager>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!IsOwner) return;
        if (pauseMenu != null && pauseMenu.isPaused) return; // 👈 BLOQUEO

        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minY, maxY);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        player.Rotate(Vector3.up * mouseX);
    }
}
