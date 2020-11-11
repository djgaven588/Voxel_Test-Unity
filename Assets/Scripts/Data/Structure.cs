using UnityEngine;

public class Structure
{
    public string Name;

    public virtual bool ConditionsMet(System.Random rng, Block below, Block current, Biome biome, Vector3Int worldPosition, out Varient varient)
    {
        varient = null;
        return false;
    }

    public class Varient
    {
        public BlockEntry[] Entries;
        public float ChanceToSpawn;
    }

    public class BlockEntry
    {
        public Block Block;
        public Vector3Int Offset;
        public bool DestroyBlocks;

        public BlockEntry(Block block, Vector3Int position, bool destroy = true)
        {
            Block = block;
            Offset = position;
            DestroyBlocks = destroy;
        }
    }
}
