using System;
using UnityEngine;

public class Entity : MonoBehaviour
{
    public Vector3 LocalPosition
    {
        get { return InternalPosition; }
        set { InternalPosition = value; transform.position = ChunkPosition * Chunk.CHUNK_SIZE + InternalPosition; }
    }

    public Vector3Int ChunkPosition
    {
        get { return InternalChunkOffset; }
        set { InternalChunkOffset = value; transform.position = ChunkPosition * Chunk.CHUNK_SIZE + InternalPosition; }
    }

    public Vector3 WorldPosition { get { return InternalPosition + ChunkPosition * Chunk.CHUNK_SIZE; } }

    [Header("Entity")]
    [Header("Internal Usage")]
    public Vector3 InternalPosition;
    public Vector3Int InternalChunkOffset;
    public Vector3 Velocity;

    [Header("Collision Variables")]
    public Bounds Collider;
    public int CollisionRadius = 1;

    [NonSerialized] public Vector3 CachedPosition;
    [NonSerialized] public Vector3Int CachedChunkOffset;

    protected World _world;

    public void Init(World world)
    {
        _world = world;
        _world?.PhysicsEngine.Register(this);
    }

    public void OnDisable()
    {
        _world?.PhysicsEngine.Deregister(this);
    }
}
