using TMPro;
using Unity.Netcode;
using UnityEngine;

public class DontDestroyCode : MonoBehaviour
{
    private static DontDestroyCode singleton;
    public static DontDestroyCode Singleton => singleton;

    public TMP_Text text;  // ← Este es el texto que muestra el código de sala

    private void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
            DontDestroyOnLoad(this.gameObject);  // ← ¡Aquí es el lugar correcto!
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    // Opcional: limpiar el texto al desconectarse
    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStopped += OnClientStopped;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
        }
    }

    private void OnClientStopped(bool _)
    {
        if (text != null)
            text.text = "- -";   // o "" si prefieres
    }
}