public class ModelManager : ItemTypeManager<Model>
{
    public static ModelManager Inst = new ModelManager();

    public override void Init()
    {
        _data.Add("Base/Model/None", new Model(false, false, false, null));

        byte[] standardTexCoords = new byte[]
        {
            0, 0,
            0, 31,
            31, 31,
            31, 0
        };

        _data.Add("Base/Model/Cross", new Model(true, false, false, new Model.Face[]
        {
            // Only one face needed
            new Model.Face()
            {
                Verticies = new ushort[]
                {
                    5, 0, 26,
                    5, 31, 26,
                    26, 31, 5,
                    26, 0, 5,

                    5, 0, 5,
                    5, 31, 5,
                    26, 31, 26,
                    26, 0, 26,

                    26, 0, 5,
                    26, 31, 5,
                    5, 31, 26,
                    5, 0, 26,

                    26, 0, 26,
                    26, 31, 26,
                    5, 31, 5,
                    5, 0, 5,
                },
                TextureCoords = new byte[]
                {
                    0, 0,
                    0, 31,
                    31, 31,
                    31, 0,

                    0, 0,
                    0, 31,
                    31, 31,
                    31, 0,

                    0, 0,
                    0, 31,
                    31, 31,
                    31, 0,

                    0, 0,
                    0, 31,
                    31, 31,
                    31, 0
                },
                Normals = new byte[]
                {
                    7, 15, 7,
                    7, 15, 7,
                    7, 15, 7,
                    7, 15, 7,

                    22, 15, 7,
                    22, 15, 7,
                    22, 15, 7,
                    22, 15, 7,

                    22, 15, 22,
                    22, 15, 22,
                    22, 15, 22,
                    22, 15, 22,

                    7, 15, 22,
                    7, 15, 22,
                    7, 15, 22,
                    7, 15, 22,
                },
                Indicies = new int[]
                {
                    0, 1, 2, 2, 3, 0,
                    4, 5, 6, 6, 7, 4,
                    8, 9, 10, 10, 11, 8,
                    12, 13, 14, 14, 15, 12
                }
            }
        }));

        _data.Add("Base/Model/Solid", new Model(true, true, true, new Model.Face[]
            {
                // Front
                new Model.Face()
                {
                    Verticies = new ushort[]
                    {
                        31, 0, 31,
                        31, 31, 31,
                        0, 31, 31,
                        0, 0, 31
                    },
                    TextureCoords = standardTexCoords,
                    Normals = new byte[]
                    {
                        15, 15, 30,
                        15, 15, 30,
                        15, 15, 30,
                        15, 15, 30
                    },
                    Indicies = new int[]
                    {
                        0, 1, 2, 2, 3, 0
                    }
                },
                // Back
                new Model.Face()
                {
                    Verticies = new ushort[]
                    {
                        0, 0, 0,
                        0, 31, 0,
                        31, 31, 0,
                        31, 0, 0,
                    },
                    TextureCoords = standardTexCoords,
                    Normals = new byte[]
                    {
                        15, 15, 0,
                        15, 15, 0,
                        15, 15, 0,
                        15, 15, 0,
                    },
                    Indicies = new int[]
                    {
                        0, 1, 2, 2, 3, 0
                    }
                },
                // Left
                new Model.Face()
                {
                    Verticies = new ushort[]
                    {
                        0, 0, 31,
                        0, 31, 31,
                        0, 31, 0,
                        0, 0, 0
                    },
                    TextureCoords = standardTexCoords,
                    Normals = new byte[]
                    {
                        0, 15, 15,
                        0, 15, 15,
                        0, 15, 15,
                        0, 15, 15,
                    },
                    Indicies = new int[]
                    {
                        0, 1, 2, 2, 3, 0
                    }
                },
                // Right
                new Model.Face()
                {
                    Verticies = new ushort[]
                    {
                        31, 0, 0,
                        31, 31, 0,
                        31, 31, 31,
                        31, 0, 31
                    },
                    TextureCoords = standardTexCoords,
                    Normals = new byte[]
                    {
                        30, 15, 15,
                        30, 15, 15,
                        30, 15, 15,
                        30, 15, 15
                    },
                    Indicies = new int[]
                    {
                        0, 1, 2, 2, 3, 0
                    }
                },
                // Top
                new Model.Face()
                {
                    Verticies = new ushort[]
                    {
                        0, 31, 0,
                        0, 31, 31,
                        31, 31, 31,
                        31, 31, 0,
                    },
                    TextureCoords = standardTexCoords,
                    Normals = new byte[]
                    {
                        15, 30, 15,
                        15, 30, 15,
                        15, 30, 15,
                        15, 30, 15
                    },
                    Indicies = new int[]
                    {
                        0, 1, 2, 2, 3, 0
                    }
                },
                // Bottom
                new Model.Face()
                {
                    Verticies = new ushort[]
                    {
                        0, 0, 31,
                        0, 0, 0,
                        31, 0, 0,
                        31, 0, 31
                    },
                    TextureCoords = standardTexCoords,
                    Normals = new byte[]
                    {
                        15, 0, 15,
                        15, 0, 15,
                        15, 0, 15,
                        15, 0, 15
                    },
                    Indicies = new int[]
                    {
                        0, 1, 2, 2, 3, 0
                    }
                },
            }));
    }
}
