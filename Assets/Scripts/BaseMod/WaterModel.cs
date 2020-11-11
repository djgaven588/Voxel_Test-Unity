using System.Collections.Generic;
using UnityEngine;

public class WaterModel : Model
{
    private Block _water;

    public WaterModel(Model solidModel, uint[] textures) : base(true, false, false, null)
    {
        Face[] faces = new Face[12];
        int faceIndex = 0;
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < solidModel.Faces.Length; j++)
            {
                faces[faceIndex].Verticies = new ushort[solidModel.Faces[j].Verticies.Length];
                faces[faceIndex].TextureCoords = new byte[solidModel.Faces[j].TextureCoords.Length];
                faces[faceIndex].Normals = new byte[solidModel.Faces[j].Normals.Length];
                faces[faceIndex].Indicies = new int[solidModel.Faces[j].Indicies.Length];

                solidModel.Faces[j].Verticies.CopyTo(faces[faceIndex].Verticies, 0);
                solidModel.Faces[j].TextureCoords.CopyTo(faces[faceIndex].TextureCoords, 0);
                solidModel.Faces[j].Normals.CopyTo(faces[faceIndex].Normals, 0);
                solidModel.Faces[j].Indicies.CopyTo(faces[faceIndex].Indicies, 0);

                faces[faceIndex].TextureIndex = textures[faceIndex++];
            }
        }

        byte waterHeightOffset = 4;
        for (int i = 6; i < 12; i++)
        {
            for (int j = 1; j < faces[i].Verticies.Length; j+=3)
            {
                if (faces[i].Verticies[j] > 6)
                {
                    faces[i].Verticies[j] -= waterHeightOffset;
                }
            }

            for (int j = 0; j < faces[i].TextureCoords.Length; j+=2)
            {
                if (faces[i].TextureCoords[j] > 6)
                {
                    faces[i].TextureCoords[j] -= waterHeightOffset;
                }
            }
        }

        Faces = faces;
    }

    public void SetWater(Block block)
    {
        _water = block;
    }

    public override void AddModel(List<ChunkVertex> verts, List<int> indicies, Vector3Int blockOffset, Block[] neighbors)
    {
        int faceOffset = 0;
        if(neighbors[4] != _water)
        {
            faceOffset = 6;
        }

        for (int i = 0; i < 4; i++)
        {
            if (i != 4 && !((neighbors[i]?.WillBlockRendering() ?? false) || neighbors[i] == _water))
            {
                AddFace(i + faceOffset, verts, indicies, blockOffset);
            }
        }

        if(faceOffset == 6)
        {
            AddFace(4 + faceOffset, verts, indicies, blockOffset);
        }
    }
}
