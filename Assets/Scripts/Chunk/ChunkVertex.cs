using System.Runtime.InteropServices;
using UnityEngine.Rendering;

[StructLayout(LayoutKind.Sequential)]
public struct ChunkVertex
{
    // Set 1:
    // Position (30 bits): 10 bits per axis (precision of 32 points per block, value / 32 is position)
    // Remaining Bits: 2 / 32

    // Set 2:
    // TexCoord (10 bits):  5 bits per axis (precision of 32 points per block, value / 32 is coordinate)
    // Normals  (15 bits):  5 bits per axis (precision of 32 points per block, value / 32 is normal)
    // Remaining Bits: 7 / 32

    // Set 3:
    // Texture Index (32 bits): YES possible textures
    // Remaining Bits: 0 / 32

    public uint Position;
    public uint TextureIndex;
    public uint TextureCoordsAndNormal;

    public static VertexAttributeDescriptor[] Attributes = new VertexAttributeDescriptor[]
    {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.UInt32, 1),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UInt32, 1),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.UInt32, 1)
    };
}