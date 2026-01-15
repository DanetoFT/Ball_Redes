using System.Collections;
using UnityEngine;

public class ShardFade : MonoBehaviour
{
    [SerializeField] private float fadeStartTime = 2f;
    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(fadeStartTime);
        float t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime * 6f;
            Color c = rend.material.color;
            c.a = t;
            rend.material.color = c;
            yield return null;
        }
    }
}