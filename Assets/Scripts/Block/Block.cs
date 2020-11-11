using UnityEngine;

public class Block
{
    public int InternalID { get; private set; }
    public string InternalName { get; private set; }
    public string Name { get; private set; }
    public Model Model { get; private set; }
    public bool CanTouch { get; private set; }
    public bool CanCollide { get; private set; }
    public bool HasExtraData { get; private set; }
    public bool CanPlaceOver { get; private set; }
    public Bounds Bounds { get; private set; }

    public Block() { }

    public Block(int internalId, string internalName, string name, Model model, Bounds bounds, bool canTouch = true, bool canCollide = true, bool canPlaceOver = false, bool hasExtraData = false)
    {
        InternalID = internalId;
        InternalName = internalName;
        Name = name;
        Model = model;
        CanTouch = canTouch;
        CanCollide = canCollide;
        Bounds = bounds;
        HasExtraData = hasExtraData;
        CanPlaceOver = canPlaceOver;
    }

    public Bounds GetBounds(Vector3Int offset)
    {
        return new Bounds(Bounds.center + offset, Bounds.size);
    }

    public bool WillBlockRendering()
    {
        return Model.Opaque && Model.FullCube;
    }

    public virtual void BlockUpdateEvent(Vector3Int causingPos, Block causingBlock, BlockUpdate type, Vector3Int affectedPos, ChunkManager chunks)
    {

    }

    public virtual void OnBlockBreak(Vector3Int position, ChunkManager chunks)
    {

    }

    public override bool Equals(object obj)
    {
        if (!(obj is Block b))
        {
            return false;
        }

        // Optimization for a common success case.
        if (ReferenceEquals(this, b))
        {
            return true;
        }

        return InternalID == b.InternalID;
    }

    public static bool operator ==(Block a, Block b)
    {
        if (a is null || b is null)
            return false;
        return a.InternalID == b.InternalID;
    }

    public static bool operator !=(Block a, Block b)
    {
        if (a is null || b is null)
            return true;
        return a.InternalID != b.InternalID;
    }

    public override int GetHashCode()
    {
        return InternalID;
    }
}
