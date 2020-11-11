using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StructureBuilder
{
    private List<Operation> _operations = new List<Operation>();

    public static StructureBuilder Start()
    {
        return new StructureBuilder();
    }

    public StructureBuilder Add(Shape shape, Block block, bool ignoreExisting = false, bool destructive = true)
    {
        _operations.Add(new Operation() { Shape = shape, Block = block, Add = true, IgnoreExisting = ignoreExisting, Destructive = destructive });

        return this;
    }

    public StructureBuilder Subtract(Shape shape)
    {
        _operations.Add(new Operation() { Shape = shape, Block = null, Add = false, IgnoreExisting = false, Destructive = false });

        return this;
    }

    public Structure.BlockEntry[] Finish()
    {
        Dictionary<Vector3Int, Structure.BlockEntry> result = new Dictionary<Vector3Int, Structure.BlockEntry>();
        for (int i = 0; i < _operations.Count; i++)
        {
            Operation op = _operations[i];
            Vector3Int[] positions = op.Shape.GetPositions();
            for (int j = 0; j < positions.Length; j++)
            {
                if (op.Add == false)
                {
                    result.Remove(positions[j]);
                }
                else
                {
                    bool contains = result.ContainsKey(positions[j]);
                    if (!contains || op.IgnoreExisting)
                    {
                        if (contains)
                        {
                            result.Remove(positions[j]);
                        }
                        result.Add(positions[j], new Structure.BlockEntry(op.Block, positions[j], op.Destructive));
                    }
                }
            }
        }

        return result.Values.ToArray();
    }

    private struct Operation
    {
        public bool Add;
        public bool IgnoreExisting;
        public bool Destructive;
        public Shape Shape;
        public Block Block;
    }

    public class Box : Shape
    {
        public Vector3Int BottomLeft;
        public Vector3Int TopRight;

        public Box(Vector3Int bottomLeft, Vector3Int topRight)
        {
            BottomLeft = bottomLeft;
            TopRight = topRight;
        }

        public override Vector3Int[] GetPositions()
        {
            Vector3Int[] positions = new Vector3Int[((TopRight.x - BottomLeft.x) + 1) * ((TopRight.y - BottomLeft.y) + 1) * ((TopRight.z - BottomLeft.z) + 1)];
            int index = 0;
            for (int x = BottomLeft.x; x <= TopRight.x; x++)
            {
                for (int y = BottomLeft.y; y <= TopRight.y; y++)
                {
                    for (int z = BottomLeft.z; z <= TopRight.z; z++)
                    {
                        positions[index] = new Vector3Int(x, y, z);
                        index++;
                    }
                }
            }

            return positions;
        }
    }

    public class Sphere : Shape
    {
        public Vector3Int Center;
        public int Size;

        public override Vector3Int[] GetPositions()
        {
            throw new System.NotImplementedException();
        }
    }

    public class BlockShape : Shape
    {
        public Vector3Int Position;

        public BlockShape(Vector3Int position)
        {
            Position = position;
        }

        public override Vector3Int[] GetPositions()
        {
            return new Vector3Int[] { Position };
        }
    }

    public abstract class Shape
    {
        public abstract Vector3Int[] GetPositions();
    }
}
