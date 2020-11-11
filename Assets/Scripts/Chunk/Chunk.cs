using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

public class Chunk
{
    public const byte CHUNK_SIZE = 32;
    public const byte CHUNK_SIZE_MINUS_ONE = CHUNK_SIZE - 1;

    public const ushort CHUNK_SIZE_SQR = CHUNK_SIZE * CHUNK_SIZE;
    public const ushort CHUNK_SIZE_CUBE = CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE;

    public const byte CHUNK_LOG_SIZE = 5;
    public const byte CHUNK_LOG_SIZE_2 = CHUNK_LOG_SIZE * 2;

    public const byte BIOME_BLEND_DISTANCE = 10;

    public Vector3Int Position;

    private DataPalette<Block> _blocks;
    private DataPalette<Biome> _biomes;

    public bool Dirty { get; private set; }
    public bool NewSaveData { get; private set; }

    public void ResetDirty()
    {
        Dirty = false;
    }

    public void ResetSaveable()
    {
        NewSaveData = false;
    }

    public void MarkDirty(bool triggerSave = false)
    {
        Dirty = true;
        _chunks.MarkForMeshGeneration(this);

        if (triggerSave)
        {
            NewSaveData = true;
            _chunks.MarkForSaving(this);
        }
    }

    private readonly Mesh _generatedMesh;
    private bool _meshReady = false;
    private bool _meshing = false;

    private List<ChunkVertex> verts = new List<ChunkVertex>();
    private List<int> indicies = new List<int>();

    private ChunkVertex[] _readyVerts;
    private int[] _readyIndicies;

    private readonly ChunkManager _chunks;

    public ConcurrentQueue<(int, bool, Block)> PushedChanges = new ConcurrentQueue<(int, bool, Block)>();

    public Chunk(Vector3Int position, ChunkManager chunks, Block defaultBlock)
    {
        Position = position;
        _blocks = new DataPalette<Block>(CHUNK_SIZE_CUBE, 6, defaultBlock);
        _biomes = new DataPalette<Biome>((CHUNK_SIZE + BIOME_BLEND_DISTANCE * 2) * (CHUNK_SIZE + BIOME_BLEND_DISTANCE * 2), 2, new Biome());
        _chunks = chunks;

        _generatedMesh = new Mesh
        {
            name = $"Chunk Mesh {Position}"
        };
    }

    public void SetBlock(Block block, int index, bool ignoreUpdate, bool shouldLock = true)
    {
        if (ignoreUpdate)
        {
            _blocks.SetEntry(index, block, shouldLock);
            return;
        }

        Block lastBlock = _blocks.GetEntry(index, shouldLock);
        _blocks.SetEntry(index, block, shouldLock);

        if (block != lastBlock)
        {
            MarkDirty(true);
        }
    }

    public Block GetBlock(int index, bool shouldLock = true)
    {
        return _blocks.GetEntry(index, shouldLock);
    }

    public int GetBlockPaletteIndex(int index)
    {
        return _blocks.GetPaletteIndex(index);
    }

    public ReaderWriterLockSlim GetLock()
    {
        return _blocks.Lock;
    }

    public ReaderWriterLockSlim GetBiomeLock()
    {
        return _biomes.Lock;
    }

    public void CompressToMaxiumum()
    {
        _blocks.CompressToMaximum();
    }

    public string GetSaveFileName()
    {
        return $"Chunk_{Position.x}_{Position.y}_{Position.z}.chunkData";
    }

    public void Draw()
    {
        if (_meshReady && !_meshing)
        {
            _meshReady = false;

            _generatedMesh.SetVertexBufferParams(_readyVerts.Length, ChunkVertex.Attributes);
            _generatedMesh.SetVertexBufferData(_readyVerts, 0, 0, _readyVerts.Length, 0, MeshUpdateFlags.DontValidateIndices);

            _generatedMesh.SetIndexBufferParams(_readyIndicies.Length, IndexFormat.UInt32);
            _generatedMesh.SetIndexBufferData(_readyIndicies, 0, 0, _readyIndicies.Length, MeshUpdateFlags.DontValidateIndices);

            _generatedMesh.subMeshCount = 1;
            _generatedMesh.SetSubMesh(0, new SubMeshDescriptor(0, _readyIndicies.Length, MeshTopology.Triangles), MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontResetBoneBounds);

            _generatedMesh.bounds = new Bounds(Vector3.one * CHUNK_SIZE / 2, Vector3.one * CHUNK_SIZE);

            _readyVerts = null;
            _readyIndicies = null;
        }

        if (_generatedMesh != null)
            Graphics.DrawMesh(_generatedMesh, Position * CHUNK_SIZE, Quaternion.identity, Initialization.Inst.ChunkMaterial, 0);
    }

    public void PushedChangesUpdate()
    {
        var locker = GetLock();
        locker.EnterWriteLock();
        while (PushedChanges.TryDequeue(out (int, bool, Block) result))
        {
            (int index, bool destructive, Block block) = result;
            if (destructive || (!destructive && GetBlock(index, false).CanPlaceOver))
            {
                SetBlock(block, index, true, false);
            }
        }

        locker.ExitWriteLock();
        Dirty = true;
        NewSaveData = true;
    }

    public void MeshGenerationUpdate(Chunk[] meshingNeighbors)
    {
        if (_generatedMesh == null)
            return;

        if (!Dirty || _meshing)
            return;

        Dirty = false;
        _meshing = true;
        _meshReady = false;

        var locker = GetLock();
        locker.EnterReadLock();
        for (int i = 0; i < _blocks.Length; i++)
        {
            Block blockI = _blocks.GetEntry(i, false);

            if (blockI.Model.IsDrawn)
            {
                Vector3Int blockOffset = new Vector3Int(i % CHUNK_SIZE, i / CHUNK_SIZE % CHUNK_SIZE, i / CHUNK_SIZE / CHUNK_SIZE % CHUNK_SIZE);

                Block[] neighbors = new Block[]
                {
                        NeighborBlockRender(0, blockOffset, meshingNeighbors),
                        NeighborBlockRender(1, blockOffset, meshingNeighbors),
                        NeighborBlockRender(2, blockOffset, meshingNeighbors),
                        NeighborBlockRender(3, blockOffset, meshingNeighbors),
                        NeighborBlockRender(4, blockOffset, meshingNeighbors),
                        NeighborBlockRender(5, blockOffset, meshingNeighbors),
                };

                blockI.Model.AddModel(verts, indicies, blockOffset, neighbors);
            }
        }
        locker.ExitReadLock();

        _readyVerts = null;
        _readyIndicies = null;

        _readyVerts = verts.ToArray();
        _readyIndicies = indicies.ToArray();

        verts.Clear();
        indicies.Clear();

        _meshReady = true;
        _meshing = false;
    }

    public void OverrideChunkData(DataPalette<Block> data)
    {
        _blocks = data;
    }

    public string[] GetPaletteNames()
    {
        string[] blockNames = new string[_blocks.PaletteEntries.Length];
        for (int i = 0; i < blockNames.Length; i++)
        {
            if (_blocks.PaletteReferences[i] > 0)
            {
                blockNames[i] = _blocks.PaletteEntries[i].InternalName;
            }
            else
            {
                blockNames[i] = "";
            }
        }

        return blockNames;
    }

    public byte[] GetPaletteData()
    {
        byte[] data = new byte[DataPalette<Block>.BytesToContainBits(_blocks.Data.Length)];
        _blocks.Data.CopyTo(data, 0);

        return data;
    }

    private Block NeighborBlockRender(int side, Vector3Int currentPosition, Chunk[] meshingNeighbors)
    {
        Chunk checkingChunk = this;
        // Handle missing data
        if (side == 0 && currentPosition.z == CHUNK_SIZE_MINUS_ONE)
        {
            if (meshingNeighbors[side] == null) return null;

            checkingChunk = meshingNeighbors[side];
            currentPosition.z = -1;
        }
        else if (side == 1 && currentPosition.z == 0)
        {
            if (meshingNeighbors[side] == null) return null;

            checkingChunk = meshingNeighbors[side];
            currentPosition.z = CHUNK_SIZE;
        }
        else if (side == 2 && currentPosition.x == 0)
        {
            if (meshingNeighbors[side] == null) return null;

            checkingChunk = meshingNeighbors[side];
            currentPosition.x = CHUNK_SIZE;
        }
        else if (side == 3 && currentPosition.x == CHUNK_SIZE_MINUS_ONE)
        {
            if (meshingNeighbors[side] == null) return null;

            checkingChunk = meshingNeighbors[side];
            currentPosition.x = -1;
        }
        else if (side == 4 && currentPosition.y == CHUNK_SIZE_MINUS_ONE)
        {
            if (meshingNeighbors[side] == null) return null;

            checkingChunk = meshingNeighbors[side];
            currentPosition.y = -1;
        }
        else if (side == 5 && currentPosition.y == 0)
        {
            if (meshingNeighbors[side] == null) return null;

            checkingChunk = meshingNeighbors[side];
            currentPosition.z = CHUNK_SIZE;
        }

        Block block = checkingChunk.GetBlock(GetIndex(side, currentPosition), checkingChunk != this);

        return block;//.WillBlockRendering();
    }

    /// <summary>
    /// Set the biome data for this chunk
    /// </summary>
    /// <param name="biomes"></param>
    public void SetBiomeData(Biome[] biomes)
    {
        var locker = GetLock();
        locker.EnterWriteLock();
        for (int i = 0; i < biomes.Length; i++)
        {
            _biomes.SetEntry(i, biomes[i], false);
        }
        locker.ExitWriteLock();
    }

    /// <summary>
    /// Gets the biome for this positon. Position can be +- BIOME_AVERAGING_DISTANCE
    /// of chunk bounds.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public Biome GetBiome(int x, int z, bool requireLock = true)
    {
        return _biomes.GetEntry(x + BIOME_BLEND_DISTANCE + (z + BIOME_BLEND_DISTANCE) * (CHUNK_SIZE + BIOME_BLEND_DISTANCE * 2), requireLock);
    }

    private int GetIndex(int side, Vector3Int currentPosition)
    {
        Vector3Int pos = currentPosition;
        if (side == 0)
        {
            pos.z++;
        }
        else if (side == 1)
        {
            pos.z--;
        }
        else if (side == 2)
        {
            pos.x--;
        }
        else if (side == 3)
        {
            pos.x++;
        }
        else if (side == 4)
        {
            pos.y++;
        }
        else
        {
            pos.y--;
        }

        return BlockToIndex(pos);
    }

    public static int BlockToIndex(Vector3Int pos)
    {
        return pos.x + pos.y * CHUNK_SIZE + pos.z * CHUNK_SIZE_SQR;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int WorldToGrid(Vector3 pos)
    {
        Vector3Int position = new Vector3Int((int)Mathf.Floor(pos.x), (int)Mathf.Floor(pos.y), (int)Mathf.Floor(pos.z));
        return position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int WorldToChunk(Vector3 pos)
    {
        Vector3Int position = new Vector3Int((int)Mathf.Floor(pos.x), (int)Mathf.Floor(pos.y), (int)Mathf.Floor(pos.z));
        return WorldToChunk(position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int WorldToBlock(Vector3 pos)
    {
        Vector3Int position = new Vector3Int((int)Mathf.Floor(pos.x), (int)Mathf.Floor(pos.y), (int)Mathf.Floor(pos.z));
        return WorldToBlock(position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int WorldToChunk(Vector3Int pos)
    {
        Vector3Int chunkPos = new Vector3Int(pos.x, pos.y, pos.z);
        chunkPos.x >>= CHUNK_LOG_SIZE;
        chunkPos.y >>= CHUNK_LOG_SIZE;
        chunkPos.z >>= CHUNK_LOG_SIZE;

        return chunkPos;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int WorldToBlock(Vector3Int pos)
    {
        Vector3Int block = new Vector3Int(pos.x, pos.y, pos.z);
        block.x &= CHUNK_SIZE_MINUS_ONE;
        block.y &= CHUNK_SIZE_MINUS_ONE;
        block.z &= CHUNK_SIZE_MINUS_ONE;
        return block;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WorldToIndex(Vector3Int pos)
    {
        return BlockToIndex(WorldToBlock(pos));
    }

    public int GetDeterministicHashcode()
    {
        unchecked
        {
            int hashCode = 107;
            hashCode = (hashCode * 397) ^ Position.x;
            hashCode = (hashCode * 359) ^ Position.y;
            hashCode = (hashCode * 563) ^ Position.z;
            return hashCode;
        }
    }
}
