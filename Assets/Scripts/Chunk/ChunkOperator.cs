using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using UnityEngine;
using System.Threading.Channels;
using System.Threading.Tasks;
using UnityEngine.Profiling;

public class ChunkOperator
{
    public bool ContinueUsingThreads = true;

    private SemaphoreSlim _threadHolder = new SemaphoreSlim(0, int.MaxValue);

    private DateTime _lastSaveTime = DateTime.UtcNow;

    private readonly ChunkManager _chunks;

    private readonly string _toSaveFolder = Path.Combine(Application.streamingAssetsPath, "Saves~");
    private readonly string _toWorldSave;

    private readonly Block Air;
    private readonly int _seed;

    public readonly ChunkTerrainGenerator _terrainGenerator;

    public enum GlobalChunkState
    {
        Loading,
        Structure,
        StructurePush,
        InitialMeshing,
        Ready
    }

    public GlobalChunkState ChunkState { get { return _state; } private set { _state = value; UpdateStage(); } }
    private GlobalChunkState _state = GlobalChunkState.Loading;

    public bool ChunksReady { get { return ChunkState == GlobalChunkState.Ready; } }

    private int _chunksLoaded = 0;
    private int _chunksStructured = 0;
    private int _chunksStructurePushed = 0;
    private int _chunksMeshed = 0;

    private Chunk[] _chunkRefs;

    public ChunkOperator(ChunkManager chunks, string saveName, string worldName, int seed)
    {
        _terrainGenerator = new ChunkTerrainGenerator();
        _terrainGenerator.Init();

        _chunks = chunks;
        _toWorldSave = Path.Combine(_toSaveFolder, saveName, worldName);

        _seed = seed;

        Air = BlockManager.Inst.GetBlockOrDefault("Base/Block/Air");

        int threadCount = 6;//Mathf.Max(Environment.ProcessorCount / 2 - 1, 2);
        Debug.Log($"Worker thread count: {threadCount}");
        _threadHolder.Release(threadCount);
    }

    public void Start()
    {
        ChunkState = GlobalChunkState.Loading;
    }

    private void UpdateStage()
    {
        if(ChunkState == GlobalChunkState.Loading)
        {
            Debug.Log("Loading...");
            _chunkRefs = new Chunk[_chunks.WorldSize.x * _chunks.WorldSize.y * _chunks.WorldSize.z];

            int index = 0;
            for (int x = 0; x < _chunks.WorldSize.x; x++)
            {
                for (int z = 0; z < _chunks.WorldSize.z; z++)
                {
                    for (int y = 0; y < _chunks.WorldSize.y; y++)
                    {
                        Chunk chunk = new Chunk(new Vector3Int(x, y, z), _chunks, Air);

                        _chunks.Chunks.TryAdd(chunk.Position, chunk);
                        _chunkRefs[index] = chunk;
                        index++;
                        MarkForLoading(chunk);
                    }
                }
            }
            Debug.Log("All loading started.");
        }
        else
        {
            switch (ChunkState)
            {
                case GlobalChunkState.Structure:
                    Debug.Log("Structuring...");
                    for (int i = 0; i < _chunkRefs.Length; i++)
                    {
                        MarkForStructureGeneration(_chunkRefs[i]);
                    }
                    Debug.Log("All structuring started.");
                    break;
                case GlobalChunkState.StructurePush:
                    Debug.Log("Pushing...");
                    for (int i = 0; i < _chunkRefs.Length; i++)
                    {
                        MarkForStructurePush(_chunkRefs[i]);
                    }
                    Debug.Log("All pushing started.");
                    break;
                case GlobalChunkState.InitialMeshing:
                    Debug.Log("Meshing...");
                    for (int i = 0; i < _chunkRefs.Length; i++)
                    {
                        MarkForMeshGeneration(_chunkRefs[i]);
                    }
                    Debug.Log("All meshing started.");
                    break;
                case GlobalChunkState.Ready:
                    Debug.Log("Complete!");
                    break;
                default:
                    break;
            }
        }
    }

    private void LoadChunk(Chunk chunk)
    {
        _terrainGenerator.GenerateBiomes(chunk, _seed);
        string file = Path.Combine(_toWorldSave, chunk.GetSaveFileName());
        if (File.Exists(file))
        {
            using FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new BinaryReader(stream);

            SavedChunk chunkData = SavedChunk.ReadChunk(reader);

            reader.Close();

            chunk.OverrideChunkData(chunkData.ChunkData);

            int structuredChunks = Interlocked.Increment(ref _chunksStructured);
            if (structuredChunks >= _chunks.ChunkCount)
            {
                ChunkState = GlobalChunkState.InitialMeshing;
            }
        }
        else
        {
            _terrainGenerator.GenerateTerrain(chunk, _seed);

            int loadedChunks = Interlocked.Increment(ref _chunksLoaded);

            if (loadedChunks >= _chunks.ChunkCount)
            {
                ChunkState = GlobalChunkState.Structure;
            }
        }
    }

    private void SaveChunk(Chunk chunk)
    {
        if (chunk.NewSaveData == false)
        {
            return;
        }

        chunk.ResetSaveable();
        string file = Path.Combine(_toWorldSave, chunk.GetSaveFileName());

        if (!Directory.Exists(_toWorldSave))
        {
            Directory.CreateDirectory(_toWorldSave);
        }

        using FileStream stream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None);
        using BinaryWriter writer = new BinaryWriter(stream);

        chunk.CompressToMaxiumum();
        SavedChunk.WriteChunk(writer, chunk);

        writer.Close();
    }

    private void StructureChunk(Chunk chunk)
    {
        if (_chunks.GetSurroundingNeighbors(chunk.Position, out Chunk[] structureNeighbors))
        {
            ChunkStructureGenerator.GenerateStructures(chunk, structureNeighbors);

            int structured = Interlocked.Increment(ref _chunksStructured);

            if (structured >= _chunks.ChunkCount)
            {
                ChunkState = GlobalChunkState.StructurePush;
            }
        }
        else
        {
            MarkForStructureGeneration(chunk);
        }
    }

    private void StructurePushChunk(Chunk chunk)
    {
        chunk.PushedChangesUpdate();

        int chunksPushed = Interlocked.Increment(ref _chunksStructurePushed);

        if (chunksPushed >= _chunks.ChunkCount)
        {
            ChunkState = GlobalChunkState.InitialMeshing;
        }
    }

    private void MeshChunk(Chunk chunk)
    {
        if (chunk.Dirty == false)
        {
            return;
        }

        if (_chunks.GetChunkNeighbors(chunk.Position, out Chunk[] neighbors))
        {
            chunk.MeshGenerationUpdate(neighbors);

            if (ChunkState == GlobalChunkState.InitialMeshing)
            {
                int meshedCount = Interlocked.Increment(ref _chunksMeshed);
                if (meshedCount >= _chunks.ChunkCount)
                {
                    ChunkState = GlobalChunkState.Ready;
                }
            }
        }
        else
        {
            MarkForMeshGeneration(chunk);
        }
    }

    public void MarkForMeshGeneration(Chunk chunk)
    {
        DispatchJob(() => MeshChunk(chunk));
    }

    public void MarkForSaving(Chunk chunk)
    {
        DispatchJob(() => SaveChunk(chunk));
    }

    private void MarkForLoading(Chunk chunk)
    {
        DispatchJob(() => LoadChunk(chunk));
    }

    private void MarkForStructureGeneration(Chunk chunk)
    {
        DispatchJob(() => StructureChunk(chunk));
    }

    private void MarkForStructurePush(Chunk chunk)
    {
        DispatchJob(() => StructurePushChunk(chunk));
    }

    private volatile int _threadCounter = 0;
    private void DispatchJob(Action job)
    {
        async Task Dispatch()
        {
            try
            {
                if (ContinueUsingThreads == false)
                    return;

                await _threadHolder.WaitAsync().ConfigureAwait(false);
                //Profiler.BeginThreadProfiling("Background Job", $"Thread: {_threadCounter}");
                job();
                _threadHolder.Release(1);
                //Profiler.EndThreadProfiling();
                _threadCounter = (_threadCounter + 1) % 2;
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
            }
        }

        Task.Run(Dispatch);
    }
}
