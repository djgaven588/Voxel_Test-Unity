using UnityEngine;

public class PlantBlock : Block
{
    private Block[] _canGrowOn;

    public PlantBlock(int internalId, string internalName, string name, Model model, Bounds bounds, bool canCollide, bool canPlaceOver, Block[] canGrowOn) : base(internalId, internalName, name, model, bounds, true, canCollide, canPlaceOver)
    {
        _canGrowOn = canGrowOn;
    }

    public bool CanGrowOn(Block block)
    {
        for (int i = 0; i < _canGrowOn.Length; i++)
        {
            if (_canGrowOn[i] == block)
            {
                return true;
            }
        }

        return false;
    }

    public override void BlockUpdateEvent(Vector3Int causingPos, Block causingBlock, BlockUpdate type, Vector3Int affectedPos, ChunkManager chunks)
    {
        if ((type == BlockUpdate.BlockBroke || type == BlockUpdate.BlockChanged) && causingPos.x == affectedPos.x && causingPos.z == affectedPos.z && causingPos.y + 1 == affectedPos.y)
        {
            for (int i = 0; i < _canGrowOn.Length; i++)
            {
                if (_canGrowOn[i] == causingBlock)
                {
                    return;
                }
            }

            chunks.BreakBlock(affectedPos);
        }
    }
}