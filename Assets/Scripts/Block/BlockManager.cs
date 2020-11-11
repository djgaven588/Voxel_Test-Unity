using System.Collections.Generic;
using UnityEngine;

public class BlockManager
{
    private Dictionary<string, Block> Blocks = new Dictionary<string, Block>();

    private int _nextId = 0;
    private Block _default;

    public static BlockManager Inst = new BlockManager();

    public void Init()
    {
        Bounds standardBounds = new Bounds(Vector3.one / 2, Vector3.one * 1.001f);
        Blocks.Add("Base/Block/Air", new Block(_nextId++, "Base/Block/Air", "Air", ModelManager.Inst.GetEntryOrDefault("Base/Model/None"), standardBounds, false, false, true)); ;

        _default = Blocks["Base/Block/Air"];
    }

    public void AddBlocks(Block[] blocks)
    {
        for (int i = 0; i < blocks.Length; i++)
        {
            Blocks.Add(blocks[i].InternalName, blocks[i]);
        }
        _nextId += blocks.Length;
    }

    public int NextId()
    {
        return _nextId;
    }

    public Block GetBlockOrDefault(string name)
    {
        if (Blocks.TryGetValue(name, out Block b))
        {
            return b;
        }

        return _default;
    }
}
