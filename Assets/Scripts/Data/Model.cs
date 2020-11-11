using System.Collections.Generic;
using UnityEngine;

public class Model
{
    public bool IsDrawn { get; private set; }
    public bool Opaque { get; private set; }
    public bool FullCube { get; private set; }

    public Face[] Faces { get; protected set; }

    public struct Face
    {
        public ushort[] Verticies;
        public byte[] TextureCoords;
        public byte[] Normals;
        public int[] Indicies;
        public uint TextureIndex;
    }

    public Model()
    {

    }

    public Model(bool isDrawn, bool fullCube, bool opaque, Face[] faces)
    {
        IsDrawn = isDrawn;
        FullCube = fullCube;
        Opaque = opaque;
        Faces = faces;
    }

    public Model(Model m, uint[] newTextures, bool opaque = true)
    {
        IsDrawn = m.IsDrawn;
        FullCube = m.FullCube;
        Opaque = opaque;

        Faces = new Face[m.Faces.Length];
        for (int i = 0; i < m.Faces.Length; i++)
        {
            Faces[i].Verticies = new ushort[m.Faces[i].Verticies.Length];
            Faces[i].TextureCoords = new byte[m.Faces[i].TextureCoords.Length];
            Faces[i].Normals = new byte[m.Faces[i].Normals.Length];
            Faces[i].Indicies = new int[m.Faces[i].Indicies.Length];

            m.Faces[i].Verticies.CopyTo(Faces[i].Verticies, 0);
            m.Faces[i].TextureCoords.CopyTo(Faces[i].TextureCoords, 0);
            m.Faces[i].Normals.CopyTo(Faces[i].Normals, 0);
            m.Faces[i].Indicies.CopyTo(Faces[i].Indicies, 0);

            Faces[i].TextureIndex = newTextures[i];
        }
    }

    public virtual void AddModel(List<ChunkVertex> verts, List<int> indicies, Vector3Int blockOffset, Block[] neighbors)
    {
        if (FullCube)
        {
            for (int i = 0; i < neighbors.Length; i++)
            {
                if ((neighbors[i]?.WillBlockRendering() ?? false) == false)
                {
                    AddFace(i, verts, indicies, blockOffset);
                }
            }
        }
        else
        {
            int count = 0;
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i]?.WillBlockRendering() ?? false)
                {
                    count++;
                }
            }

            if (count < neighbors.Length)
            {
                for (int i = 0; i < Faces.Length; i++)
                {
                    AddFace(i, verts, indicies, blockOffset);
                }
            }
        }
    }

    protected void AddFace(int face, List<ChunkVertex> verts, List<int> indicies, Vector3Int blockOffset)
    {
        int startingVert = verts.Count;
        Face workingFace = Faces[face];

        int vertI = 0;
        int texI = 0;
        int normI = 0;
        for (int i = 0; i < workingFace.Verticies.Length; i += 3)
        {
            verts.Add(new ChunkVertex()
            {
                Position = (uint)((workingFace.Verticies[vertI++] + blockOffset.x * 31) | (workingFace.Verticies[vertI++] + blockOffset.y * 31) << 10 | (workingFace.Verticies[vertI++] + blockOffset.z * 31) << 20),
                TextureCoordsAndNormal = (uint)(workingFace.TextureCoords[texI++] | workingFace.TextureCoords[texI++] << 5 |
                                            workingFace.Normals[normI++] << 10 | workingFace.Normals[normI++] << 15 | workingFace.Normals[normI++] << 20),
                TextureIndex = workingFace.TextureIndex
            });
        }

        for (int i = 0; i < workingFace.Indicies.Length; i++)
        {
            indicies.Add(workingFace.Indicies[i] + startingVert);
        }
    }
}
