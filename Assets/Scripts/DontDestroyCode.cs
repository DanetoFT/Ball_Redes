using TMPro;
using UnityEngine;

public class DontDestroyCode : MonoBehaviour
{
    private static DontDestroyCode singleton;
    public static DontDestroyCode Singleton => singleton;

    public TMP_Text text;

    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    private void Awake()
    {
        if(singleton == null)
        {
            singleton = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
}
