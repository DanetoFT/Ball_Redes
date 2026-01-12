using UnityEngine;
using Unity.Netcode.Components;
public class NetworkTransformClientAuthoritative : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return base.OnIsServerAuthoritative();
    }
}
