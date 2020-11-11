using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager
{
    public ConcurrentDictionary<Vector3Int, Chunk> Chunks = new ConcurrentDictionary<Vector3Int, Chunk>();

    public readonly Vector3Int WorldSize = new Vector3Int(32, 4, 32);

    public bool ChunksReady { get { return _chunkOperations.ChunksReady; } }

    private readonly ChunkOperator _chunkOperations;
    private readonly Block Air;

    private readonly bool _savingEnabled;

    public readonly int ChunkCount;


    public ChunkManager(int seed, string saveName, string worlName, bool savingEnabled)
    {
        Air = BlockManager.Inst.GetBlockOrDefault("Base/Block/Air");
        _chunkOperations = new ChunkOperator(this, saveName, worlName, seed);
        _savingEnabled = savingEnabled;

        ChunkCount = WorldSize.x * WorldSize.y * WorldSize.z;

        _chunkOperations.Start();
    }

    public void OnApplicationQuit()
    {
        _chunkOperations.ContinueUsingThreads = false;
        //_chunkOperations.ReleaseAll();
    }

    public void Update()
    {
        foreach (var item in Chunks)
        {
            item.Value.Draw();
        }

        //Debug.Log(_chunkOperations._terrainGenerator.GetDebugData(Initialization.Inst.Player.WorldPosition.x, Initialization.Inst.Player.WorldPosition.z));
    }

    /// <summary>
    /// Notifies the chunk operator to mark the provided chunk for meshing
    /// </summary>
    /// <param name="chunk">The chunk to mesh</param>
    public void MarkForMeshGeneration(Chunk chunk)
    {
        _chunkOperations.MarkForMeshGeneration(chunk);
    }

    /// <summary>
    /// Notifies the chunk operator to mark the provided chunk for saving
    /// </summary>
    /// <param name="chunk">The chunk to save</param>
    public void MarkForSaving(Chunk chunk)
    {
        if (_savingEnabled)
            _chunkOperations.MarkForSaving(chunk);
    }

    /// <summary>
    /// Get the blocks within the given radius of the global block position
    /// </summary>
    /// <param name="blockPos">The global block position we are starting at</param>
    /// <param name="radius">The radius around the global block we are retrieving</param>
    /// <returns></returns>
    public (Block, Vector3Int)[] GetSurroundingBlocks(Vector3Int blockPos, int radius)
    {
        Chunk chunkCache = null;
        List<(Block, Vector3Int)> possibleHits = new List<(Block, Vector3Int)>();
        for (int x = -radius + 1; x <= radius - 1; x++)
        {
            for (int y = -radius + 1; y <= radius - 1; y++)
            {
                for (int z = -radius + 1; z <= radius - 1; z++)
                {
                    Vector3Int newPos = blockPos + new Vector3Int(x, y, z);
                    Vector3Int newChunkPos = Chunk.WorldToChunk(newPos);

                    if (chunkCache == null || chunkCache.Position != newChunkPos)
                    {
                        Chunks.TryGetValue(newChunkPos, out chunkCache);
                    }

                    if (chunkCache != null)
                    {
                        possibleHits.Add((chunkCache.GetBlock(Chunk.WorldToIndex(newPos)), newPos));
                    }
                }
            }
        }

        return possibleHits.ToArray();
    }

    /// <summary>
    /// All adjacent positions
    /// </summary>
    private static readonly Vector3Int[] _neighborPositions = new Vector3Int[]
    {
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 0, -1),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
    };

    /// <summary>
    /// Get the adjacent chunks to the given chunk position
    /// </summary>
    /// <param name="chunkPos">The center chunk position</param>
    /// <param name="output">The chunks we were able to find</param>
    /// <returns>If true, success. Otherwise the operation failed.</returns>
    public bool GetChunkNeighbors(Vector3Int chunkPos, out Chunk[] output)
    {
        output = new Chunk[_neighborPositions.Length];
        for (int i = 0; i < _neighborPositions.Length; i++)
        {
            Vector3Int neighborPos = _neighborPositions[i] + chunkPos;
            if (neighborPos.x >= 0 && neighborPos.x < WorldSize.x &&
                neighborPos.y >= 0 && neighborPos.y < WorldSize.y &&
                neighborPos.z >= 0 && neighborPos.z < WorldSize.z)
            {
                if (!Chunks.TryGetValue(neighborPos, out output[i]))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Gets the surrounding neighbors, specifically for structure generation
    /// </summary>
    /// <param name="location">The center chunnk position</param>
    /// <param name="neighbors">The neighbors we found</param>
    /// <returns>If true, it was successful, otherwise it failed.</returns>
    public bool GetSurroundingNeighbors(Vector3Int location, out Chunk[] neighbors)
    {
        neighbors = new Chunk[27];

        int index = 0;
        for (int z = -1; z <= 1; z++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (!(x == 0 && y == 0 && z == 0))
                    {
                        Vector3Int nextPos = location + new Vector3Int(x, y, z);
                        if (Chunks.TryGetValue(nextPos, out neighbors[index]) == false)
                        {
                            if (nextPos.x > 0 && nextPos.x < WorldSize.x - 1 &&
                                nextPos.y > 0 && nextPos.y < WorldSize.y - 1 &&
                                nextPos.z > 0 && nextPos.z < WorldSize.z - 1)
                            {
                                return false;
                            }
                        }
                    }

                    index++;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Breaks the block at the given position.
    /// </summary>
    /// <param name="globalPosition">The global position of the block</param>
    /// <param name="chunk">The potential chunk we can use for this operation</param>
    public void BreakBlock(Vector3Int globalPosition, Chunk chunk = null)
    {
        if (chunk == null && !Chunks.TryGetValue(Chunk.WorldToChunk(globalPosition), out chunk))
        {
            return;
        }

        chunk.SetBlock(Air, Chunk.WorldToIndex(globalPosition), false);
        PropogateUpdate(globalPosition, Air, chunk, BlockUpdate.BlockBroke);
        MarkNeighborsDirty(globalPosition, chunk);
    }

    /// <summary>
    /// Places a block at the given position. If a block exists,
    /// the block will first be broken, then placed to handle
    /// events properly.
    /// </summary>
    /// <param name="globalPosition">The global position of the block</param>
    /// <param name="block">The block type we are setting</param>
    /// <param name="chunk">The (potential) chunk we can use for this</param>
    public void PlaceBlock(Vector3Int globalPosition, Block block, Chunk chunk = null)
    {
        if (chunk == null && !Chunks.TryGetValue(Chunk.WorldToChunk(globalPosition), out chunk))
        {
            return;
        }

        int blockIndex = Chunk.WorldToIndex(globalPosition);
        Block existingBlock = chunk.GetBlock(blockIndex);
        if (existingBlock != Air)
        {
            BreakBlock(globalPosition, chunk);
        }

        chunk.SetBlock(block, blockIndex, false);
        PropogateUpdate(globalPosition, block, chunk, BlockUpdate.BlockPlaced);
        MarkNeighborsDirty(globalPosition, chunk);
    }

    /// <summary>
    /// Mark the neighbor chunks as dirty for mesh generation.
    /// </summary>
    /// <param name="blockWorldPosition">The block position to check</param>
    /// <param name="source">The source of the marking</param>
    private void MarkNeighborsDirty(Vector3Int blockWorldPosition, Chunk source)
    {
        for (int i = 0; i < _neighborPositions.Length; i++)
        {
            Vector3Int nextPos = blockWorldPosition + _neighborPositions[i];
            if (Chunk.WorldToChunk(nextPos) != source.Position)
            {
                if (Chunks.TryGetValue(Chunk.WorldToChunk(nextPos), out Chunk neighbor))
                {
                    neighbor.MarkDirty();
                }
            }
        }
    }

    /// <summary>
    /// Propogates the given update to all blocks adjacent to position
    /// </summary>
    /// <param name="position">The position the update occured at</param>
    /// <param name="causer">The block that caused this update (newest)</param>
    /// <param name="chunkCache">The cached chunk to use, if available</param>
    /// <param name="update">The update to propogate</param>
    private void PropogateUpdate(Vector3Int position, Block causer, Chunk chunkCache, BlockUpdate update)
    {
        for (int i = 0; i < _neighborPositions.Length; i++)
        {
            Vector3Int nextPos = position + _neighborPositions[i];
            Vector3Int nextChunkPos = Chunk.WorldToChunk(nextPos);

            if (chunkCache == null || chunkCache.Position != nextChunkPos)
            {
                Chunks.TryGetValue(Chunk.WorldToChunk(position), out chunkCache);
            }

            if (chunkCache != null)
            {
                Block block = chunkCache.GetBlock(Chunk.WorldToIndex(nextPos));
                block.BlockUpdateEvent(position, causer, update, nextPos, this);
            }
        }
    }
}
