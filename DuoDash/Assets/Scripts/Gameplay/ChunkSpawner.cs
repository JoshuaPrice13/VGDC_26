using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Deterministic, seed-driven chunk spawner.
///
/// Both clients call Initialize(seed) with the same seed and will produce
/// the identical chunk sequence — no world state needs to travel over the network.
///
/// A "chunk" is a prefab that contains the track segment and any obstacles for that section.
/// All chunk prefabs should be the same length (set chunkLength to match).
/// Obstacles inside chunks should use ObstacleCollision components.
/// </summary>
public class ChunkSpawner : MonoBehaviour
{
    [Header("Chunk Prefabs")]
    [Tooltip("All possible track segment prefabs. Must all be the same length along Z.")]
    public GameObject[] chunkPrefabs;

    [Header("Spawner Settings")]
    [Tooltip("Length of each chunk along the Z axis. Must match your prefabs.")]
    public float chunkLength = 20f;

    [Tooltip("How many chunks to keep spawned ahead of the ball.")]
    public int chunksAheadCount = 5;

    [Tooltip("Despawn chunks this many units behind the ball.")]
    public float despawnDistance = 30f;

    [Header("References")]
    public Transform ballTransform;

    // -------------------------------------------------------------------------
    // Internal State
    // -------------------------------------------------------------------------

    private System.Random rng;
    private float nextSpawnZ = 0f;
    private readonly List<GameObject> activeChunks = new List<GameObject>();

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by GameManager.RPC_StartLevel on both clients with the shared seed.
    /// Destroys any existing chunks and rebuilds from scratch.
    /// </summary>
    public void Initialize(int seed)
    {
        // Clear existing chunks
        foreach (GameObject chunk in activeChunks)
        {
            if (chunk != null) Destroy(chunk);
        }
        activeChunks.Clear();

        rng = new System.Random(seed);
        nextSpawnZ = 0f;

        // Pre-spawn initial chunks so the ball never sees empty track at start
        for (int i = 0; i < chunksAheadCount; i++)
            SpawnNextChunk();
    }

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    void Update()
    {
        if (rng == null || ballTransform == null) return;

        SpawnAhead();
        DespawnBehind();
    }

    // -------------------------------------------------------------------------
    // Spawning / Despawning
    // -------------------------------------------------------------------------

    void SpawnAhead()
    {
        float spawnThreshold = ballTransform.position.z + chunksAheadCount * chunkLength;
        while (nextSpawnZ < spawnThreshold)
            SpawnNextChunk();
    }

    void SpawnNextChunk()
    {
        if (chunkPrefabs == null || chunkPrefabs.Length == 0) return;

        int index = rng.Next(0, chunkPrefabs.Length);
        Vector3 spawnPos = new Vector3(0f, 0f, nextSpawnZ);
        GameObject chunk = Instantiate(chunkPrefabs[index], spawnPos, Quaternion.identity);
        activeChunks.Add(chunk);

        nextSpawnZ += chunkLength;
    }

    void DespawnBehind()
    {
        for (int i = activeChunks.Count - 1; i >= 0; i--)
        {
            if (activeChunks[i] == null)
            {
                activeChunks.RemoveAt(i);
                continue;
            }

            float distBehind = ballTransform.position.z - activeChunks[i].transform.position.z;
            if (distBehind > despawnDistance)
            {
                Destroy(activeChunks[i]);
                activeChunks.RemoveAt(i);
            }
        }
    }
}
