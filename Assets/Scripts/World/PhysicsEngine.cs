using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The PhysicsEngine is a general class which can be instantiated to provide
/// physics for the given world's entities. Those entities will register themselves
/// after being initialized.
/// </summary>
public class PhysicsEngine
{
    private readonly HashSet<Entity> _entities = new HashSet<Entity>();
    private World _world;

    public const float Gravity = -19.62f;

    /// <summary>
    /// Initialize this instance of the physics engine
    /// </summary>
    /// <param name="world"></param>
    public void Init(World world)
    {
        _world = world;
    }

    public void Update()
    {
        foreach (Entity entity in _entities)
        {
            // In order to handle high velocities and / or freezing of the game (high momentary velocity)
            // the below will iterate over the movement of an entity based on momentary velocity to
            // avoid going through objects. It may be wise to adjust the amount of iterations
            // caused by velocity though, as more iterations is more expensive.
            Vector3 movementDifference = entity.Velocity * Time.deltaTime;
            int iterations = Mathf.FloorToInt(movementDifference.magnitude * 10) + 1;

            for (int i = 0; i < iterations; i++)
            {
                // Move one iteration
                entity.InternalPosition += movementDifference / iterations;

                Vector3 previousPosition = entity.CachedPosition;
                Vector3Int previousChunk = entity.CachedChunkOffset;

                Vector3 currentPosition = entity.InternalPosition;
                Vector3Int currentChunk = entity.InternalChunkOffset;

                // Update Chunk Offset
                Vector3Int chunkOff = Chunk.WorldToChunk(entity.InternalPosition);
                entity.InternalChunkOffset += chunkOff;
                entity.LocalPosition -= chunkOff * Chunk.CHUNK_SIZE;

                if (EntityOverlaps(entity, out Bounds[] overlapped))
                {
                    // Reset to only resolve X
                    entity.InternalPosition.y = previousPosition.y;
                    entity.InternalChunkOffset.y = previousChunk.y;

                    entity.InternalPosition.z = previousPosition.z;
                    entity.InternalChunkOffset.z = previousChunk.z;

                    float rX = ResolveDimension(entity, overlapped, 0, 1, 2);
                    entity.InternalPosition.x -= rX;

                    if (rX > 0 || rX < 0)
                    {
                        entity.Velocity.x = 0;
                    }

                    // Update Y to resolve it
                    entity.InternalPosition.y = currentPosition.y;
                    entity.InternalChunkOffset.y = currentChunk.y;

                    float rY = ResolveDimension(entity, overlapped, 1, 2, 0);
                    entity.InternalPosition.y -= rY;

                    if (rY > 0 || rY < 0)
                    {
                        entity.Velocity.y = 0;
                    }

                    // Update Z to resolve it
                    entity.InternalPosition.z = currentPosition.z;
                    entity.InternalChunkOffset.z = currentChunk.z;

                    float rZ = ResolveDimension(entity, overlapped, 2, 0, 1);
                    entity.InternalPosition.z -= rZ;

                    if (rZ > 0 || rZ < 0)
                    {
                        entity.Velocity.z = 0;
                    }

                    // Update Chunk Offset
                    chunkOff = Chunk.WorldToChunk(entity.InternalPosition);
                    entity.InternalChunkOffset += chunkOff;
                    entity.LocalPosition -= chunkOff * Chunk.CHUNK_SIZE;
                }

                // Update old position
                entity.CachedPosition = entity.LocalPosition;
                entity.CachedChunkOffset = entity.ChunkPosition;
            }
        }
    }

    /// <summary>
    /// Checks if the given bounds have any entities within them.
    /// UNOPTIMIZED, BE CAREFUL
    /// </summary>
    /// <param name="bounds">The bounds to check</param>
    /// <returns></returns>
    public bool CheckIfEntityWithinBounds(Bounds bounds)
    {
        foreach (Entity entity in _entities)
        {
            Bounds entityBounds = new Bounds(entity.WorldPosition + entity.Collider.center, entity.Collider.size * 0.99f);
            if (bounds.Intersects(entityBounds))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the given enemy overlaps anything, and if so, what it overlapped
    /// </summary>
    /// <param name="entity">The entity</param>
    /// <param name="overlapped">The resulting overlapped bounds</param>
    /// <returns>Was there any entity overlapping?</returns>
    private bool EntityOverlaps(Entity entity, out Bounds[] overlapped)
    {
        Vector3Int worldBlockPos = Chunk.WorldToGrid(entity.InternalPosition) + entity.InternalChunkOffset * Chunk.CHUNK_SIZE;
        (Block, Vector3Int)[] possiblePositions = _world.Chunks.GetSurroundingBlocks(worldBlockPos, entity.CollisionRadius);
        List<Bounds> hitPositions = new List<Bounds>();

        Bounds entityBounds = new Bounds(entity.WorldPosition + entity.Collider.center, entity.Collider.size);

        for (int i = 0; i < possiblePositions.Length; i++)
        {
            Block blockEntry = possiblePositions[i].Item1;
            Vector3Int blockPos = possiblePositions[i].Item2;
            Bounds blockBounds = new Bounds(blockPos + blockEntry.Bounds.center, blockEntry.Bounds.size);

            if (blockEntry.CanCollide && entityBounds.Intersects(blockBounds))
            {
                hitPositions.Add(blockBounds);
            }
        }

        overlapped = hitPositions.ToArray();
        return hitPositions.Count > 0;
    }

    /// <summary>
    /// Resolves the given dimension for an entity given the following parameters
    /// </summary>
    /// <param name="entity">The entity to resolve for</param>
    /// <param name="gridPositions">The bounds to resolve against</param>
    /// <param name="dimension">The dimension we are checking for</param>
    /// <param name="otherDimension1">The dimension we are checking against</param>
    /// <param name="otherDimension2">The second dimension we are checking against</param>
    /// <returns></returns>
    private float ResolveDimension(Entity entity, Bounds[] gridPositions, int dimension, int otherDimension1, int otherDimension2)
    {
        Vector3 boundSize = entity.Collider.size;
        Vector3 boundCenter = entity.Collider.center + entity.InternalPosition + entity.InternalChunkOffset * Chunk.CHUNK_SIZE;

        Bounds playerBounds = new Bounds(boundCenter, boundSize);
        Bounds currentGrid;
        float maxFound = 0;
        for (int i = 0; i < gridPositions.Length; i++)
        {
            currentGrid = gridPositions[i];
            float[] overlap = new float[3];
            for (int j = 0; j < 3; j++)
            {
                float overlap1 = playerBounds.min[j] - currentGrid.max[j];
                float overlap2 = playerBounds.max[j] - currentGrid.min[j];
                overlap[j] = Mathf.Abs(overlap1) < Mathf.Abs(overlap2) ? overlap1 : overlap2;
            }

            if (Mathf.Abs(overlap[dimension]) > Mathf.Abs(maxFound))
            {
                if (Mathf.Abs(overlap[dimension]) < Mathf.Abs(overlap[otherDimension1]) && Mathf.Abs(overlap[dimension]) < Mathf.Abs(overlap[otherDimension2]))
                {
                    maxFound = overlap[dimension];
                }
            }
        }

        return maxFound;
    }

    /// <summary>
    /// Register an entity to be updated by this physics engine
    /// </summary>
    /// <param name="entity"></param>
    public void Register(Entity entity)
    {
        _entities.Add(entity);
    }

    /// <summary>
    /// Deregister an entity to no longer be updated by this physics engine
    /// </summary>
    /// <param name="entity"></param>
    public void Deregister(Entity entity)
    {
        _entities.Remove(entity);
    }
}
