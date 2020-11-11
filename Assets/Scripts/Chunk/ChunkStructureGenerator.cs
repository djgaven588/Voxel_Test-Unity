using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public static class ChunkStructureGenerator
{

    private static Structure[] PotentialStructures;

    public static void Init()
    {
        PotentialStructures = StructureManager.Inst.GetAll();
    }

    private static volatile int _concurrentCounter = 0;

    public static void GenerateStructures(Chunk chunk, in Chunk[] neighbors)
    {
        var biomeLocker = chunk.GetBiomeLock();
        biomeLocker.EnterReadLock();

        var mainLocker = chunk.GetLock();
        mainLocker.EnterWriteLock();

        Interlocked.Increment(ref _concurrentCounter);

        System.Random random = new System.Random(chunk.GetDeterministicHashcode());
        Vector3Int worldPos = new Vector3Int(chunk.Position.x, chunk.Position.y, chunk.Position.z) * Chunk.CHUNK_SIZE;
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            worldPos.x++;
            worldPos.z = chunk.Position.z * Chunk.CHUNK_SIZE;
            for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
            {
                worldPos.z++;
                worldPos.y = chunk.Position.y * Chunk.CHUNK_SIZE;
                if (random.Next(0, 2) == 0)
                {
                    Structure structure = PotentialStructures[random.Next(PotentialStructures.Length)];
                    Biome biome = chunk.GetBiome(x, z, false);
                    Block below = null;
                    
                    for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                    {
                        worldPos.y++;
                        int location = x + y * Chunk.CHUNK_SIZE + z * Chunk.CHUNK_SIZE_SQR;
                        
                        Block currentBlock = chunk.GetBlock(location, false);
                        
                        if (structure.ConditionsMet(random, below, currentBlock, biome, worldPos, out Structure.Varient varient))
                        {
                            for (int i = 0; i < varient.Entries.Length; i++)
                            {
                                Vector3Int blockPos = varient.Entries[i].Offset;
                                blockPos.x += x;
                                blockPos.y += y;
                                blockPos.z += z;
                                AttemptPlaceBlock(varient.Entries[i].DestroyBlocks, varient.Entries[i].Block, blockPos.x, blockPos.y, blockPos.z, neighbors, chunk);//, modifications);
                            }
                            break;
                        }

                        below = currentBlock;
                    }
                }
            }
        }

        int others = Interlocked.Decrement(ref _concurrentCounter);
        mainLocker.ExitWriteLock();
        biomeLocker.ExitReadLock();
    }

    private static void AttemptPlaceBlock(bool destructive, Block block, int x, int y, int z, Chunk[] neighbors, Chunk self)//, Dictionary<Chunk, List<(int, bool, Block)>> modifications)
    {
        CorrectCoordinates(ref x, ref y, ref z, out int neighbor);

        Chunk chunkToModify = neighbor == 13 ? self : neighbors[neighbor];

        if (chunkToModify == null)
        {
            return;
        }

        int index = x + y * Chunk.CHUNK_SIZE + z * Chunk.CHUNK_SIZE_SQR;

        if (chunkToModify == self)
        {
            if (destructive || (!destructive && self.GetBlock(index, false).CanPlaceOver))
            {
                self.SetBlock(block, index, true, false);
            }
        }
        else
        {
            chunkToModify.PushedChanges.Enqueue((index, destructive, block));
        }
    }

    private static void CorrectCoordinates(ref int x, ref int y, ref int z, out int neighborIndex)
    {
        Vector3Int chunkOffset = new Vector3Int(1, 1, 1);
        if (x < 0)
        {
            x += Chunk.CHUNK_SIZE;
            chunkOffset.x--;
        }
        else if (x > Chunk.CHUNK_SIZE_MINUS_ONE)
        {
            x -= Chunk.CHUNK_SIZE;
            chunkOffset.x++;
        }

        if (y < 0)
        {
            y += Chunk.CHUNK_SIZE;
            chunkOffset.y--;
        }
        else if (y > Chunk.CHUNK_SIZE_MINUS_ONE)
        {
            y -= Chunk.CHUNK_SIZE;
            chunkOffset.y++;
        }

        if (z < 0)
        {
            z += Chunk.CHUNK_SIZE;
            chunkOffset.z--;
        }
        else if (z > Chunk.CHUNK_SIZE_MINUS_ONE)
        {
            z -= Chunk.CHUNK_SIZE;
            chunkOffset.z++;
        }

        neighborIndex = chunkOffset.x + chunkOffset.y * 3 + chunkOffset.z * 9;
    }
}