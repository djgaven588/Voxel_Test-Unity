using System;
using UnityEngine;

public class World
{
    public ChunkManager Chunks;
    public PhysicsEngine PhysicsEngine;

    public World(int seed, string saveName, string worldName, bool savingEnabled)
    {
        Chunks = new ChunkManager(seed, saveName, worldName, savingEnabled);
        PhysicsEngine = new PhysicsEngine();
        PhysicsEngine.Init(this);

        foreach (Entity item in UnityEngine.Object.FindObjectsOfType<Entity>())
        {
            item.OnDisable();
            item.Init(this);
        }
    }

    public void OnApplicationQuit()
    {
        Chunks.OnApplicationQuit();
    }

    public void Update()
    {
        Chunks.Update();

        if (!Chunks.ChunksReady)
        {
            return;
        }

        (bool hit, Vector3Int blockPos, Vector3Int face) = BlockRaycast.Raycast(Camera.main.transform.position, ForwardVector(Camera.main.transform.eulerAngles), 8, Chunks);

        if (hit && face != Vector3Int.zero && Chunks.Chunks.TryGetValue(Chunk.WorldToChunk(blockPos), out Chunk chunk))
        {
            Initialization.Inst.Outline.localPosition = blockPos;
            if (face.sqrMagnitude > 0.01f)
                Initialization.Inst.OutlineSelected.forward = face;

            int blockIndex = Chunk.WorldToIndex(blockPos);
            Block block = chunk.GetBlock(blockIndex);
            Initialization.Inst.OutlineVisual.localPosition = block.Bounds.center;
            Initialization.Inst.OutlineVisual.localScale = block.Bounds.size + Vector3.one / 100;

            Initialization.Inst.Outline.gameObject.SetActive(true);

            if (Input.GetMouseButtonDown(0))
            {
                Chunks.BreakBlock(blockPos, chunk);
            }
            else if (Input.GetMouseButtonDown(1))
            {
                Block b = BlockManager.Inst.GetBlockOrDefault("Base/Block/Stone");
                Block placeOn = chunk.GetBlock(Chunk.WorldToIndex(blockPos));

                Vector3Int newBlockPos = blockPos + (placeOn.CanPlaceOver ? Vector3Int.zero : face);
                Vector3Int newBlockChunk = Chunk.WorldToChunk(newBlockPos);

                Chunk placeChunk = chunk;

                if (newBlockChunk != chunk.Position)
                {
                    Chunks.Chunks.TryGetValue(newBlockChunk, out placeChunk);
                }

                Block existing = placeChunk.GetBlock(Chunk.WorldToIndex(newBlockPos));
                if (placeChunk != null && existing.CanPlaceOver)
                {
                    if (b.CanCollide)
                    {
                        if (!PhysicsEngine.CheckIfEntityWithinBounds(new Bounds(newBlockPos + b.Bounds.center, b.Bounds.size)))
                        {
                            Chunks.PlaceBlock(newBlockPos, b, placeChunk);
                        }
                    }
                    else
                    {
                        Chunks.PlaceBlock(newBlockPos, b, placeChunk);
                    }
                }
            }
        }
        else
        {
            Initialization.Inst.Outline.gameObject.SetActive(false);
        }


        PhysicsEngine.Update();
    }

    public static Vector3 ForwardVector(Vector3 euler)
    {
        euler.x = ConvertToRadians(euler.x);
        euler.y = ConvertToRadians(euler.y);
        return new Vector3((float)Math.Sin(euler.y) * (float)Math.Cos(euler.x), -(float)Math.Sin(euler.x), (float)Math.Cos(euler.y) * (float)Math.Cos(euler.x));
    }

    public const float PI = 3.1415926535897931f;
    /// <summary>
    /// Converts degrees into radians
    /// </summary>
    /// <param name="degrees"></param>
    /// <returns></returns>
    public static float ConvertToRadians(float degrees)
    {
        return (PI / 180) * degrees;
    }
}
