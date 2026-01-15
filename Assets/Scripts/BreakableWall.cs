using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class BreakableWall : NetworkBehaviour
{
    [Header("Pared/Muro")]
    [SerializeField] private MeshRenderer wallRenderer;
    [SerializeField] private Collider wallCollider;

    [Header("Fragmentos")]
    [SerializeField] private GameObject[] shardPrefabs;
    [SerializeField] private int numShards = 8;
    [SerializeField] private float explosionForce = 15f;
    [SerializeField] private float shardLifetime = 4f;

    [Header("Comportamiento")]
    [SerializeField] private bool oneTimeOnly = true;
    [SerializeField] private float minHitSpeed = 1f;

    private bool broken = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;
        if (!collision.collider.CompareTag("Ball")) return;
        if (collision.relativeVelocity.magnitude < minHitSpeed) return;
        if (oneTimeOnly && broken) return;
        broken = true;

        wallCollider.enabled = false;
        if (wallRenderer) wallRenderer.enabled = false;

        ContactPoint contact = collision.GetContact(0);
        SpawnShardsClientRpc(contact.point, contact.normal, collision.relativeVelocity.magnitude);
    }

    private void HideWall()
    {
        if (wallRenderer) wallRenderer.enabled = false;
        if (wallCollider) wallCollider.enabled = false;
    }

    [ClientRpc]
    private void SpawnShardsClientRpc(Vector3 hitPoint, Vector3 hitNormal, float hitSpeed)
    {
        System.Random syncedRandom = new System.Random((int)(NetworkManager.Singleton.ServerTime.Time * 1000L));

        float scatterRadius = 0.5f;

        for (int i = 0; i < numShards; i++)
        {
            GameObject shardPrefab = shardPrefabs[
                syncedRandom.Next(shardPrefabs.Length)
            ];

            Vector3 offset = new Vector3(
                (float)syncedRandom.NextDouble() * 2f - 1f,
                (float)syncedRandom.NextDouble() * 0.5f,
                (float)syncedRandom.NextDouble() * 2f - 1f
            ) * scatterRadius;
            Vector3 spawnPos = hitPoint + offset;

            GameObject shard = Instantiate(shardPrefab, spawnPos, Quaternion.identity);

            if (shard.TryGetComponent<Rigidbody>(out Rigidbody shardRb))
            {
                Vector3 dir = hitNormal * 0.8f + new Vector3(
                    (float)syncedRandom.NextDouble() * 2f - 1f,
                    (float)syncedRandom.NextDouble(),
                    (float)syncedRandom.NextDouble() * 2f - 1f
                ).normalized * 0.5f;

                float force = explosionForce * (hitSpeed * 0.1f + 1f);
                shardRb.AddForce(dir * force, ForceMode.Impulse);

                Vector3 torque = new Vector3(
                    (float)syncedRandom.NextDouble() * 10f,
                    (float)syncedRandom.NextDouble() * 10f,
                    (float)syncedRandom.NextDouble() * 10f
                );
                shardRb.AddTorque(torque, ForceMode.Impulse);
            }

            Destroy(shard, shardLifetime);
        }
    }
}