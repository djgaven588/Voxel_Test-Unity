using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TextureManager
{
    private Dictionary<string, uint> Textures = new Dictionary<string, uint>();

    public static TextureManager Inst = new TextureManager();

    public uint GetTextureOrDefault(string name)
    {
        if (Textures.TryGetValue(name, out uint value))
        {
            return value;
        }

        return 0;
    }

    public void Init(Material chunkMaterial, string[] modFolders)
    {
        List<string> allTextures = new List<string>();

        uint textureIndex = 0;
        string modData = Application.streamingAssetsPath;
        for (int i = 0; i < modFolders.Length; i++)
        {
            string textureRoot = Path.Combine(modData, modFolders[i], "Textures");
            string[] names = Directory.GetFiles(textureRoot, "*.png", SearchOption.AllDirectories);

            for (int j = 0; j < names.Length; j++)
            {
                string textureName = names[j].Substring(modData.Length + 1, names[j].Length - modData.Length - 5).Replace('\\', '/');
                Textures.Add(textureName, textureIndex++);
                allTextures.Add(names[j]);
            }
        }

        LoadTextures(allTextures.ToArray(), chunkMaterial);
    }

    private void LoadTextures(string[] texturePaths, Material chunkMaterial)
    {
        int mipMapCount = 1 + (int)Math.Floor(Math.Log(Math.Max(16, 16)));
        Texture2DArray textureArray = new Texture2DArray(16, 16, texturePaths.Length, TextureFormat.RGBA32, mipMapCount, false)
        {
            filterMode = FilterMode.Point
        };

        for (int i = 0; i < texturePaths.Length; i++)
        {
            string texturePath = texturePaths[i];
            if (!File.Exists(texturePath))
            {
                throw new FileNotFoundException("The provided path does not contain an image file!");
            }

            Texture2D textureData = new Texture2D(16, 16, TextureFormat.RGBA32, true, false);
            if (!textureData.LoadImage(File.ReadAllBytes(texturePath)))
            {
                throw new Exception("The provided image did not load correctly! Ensure that the file is a 32bit RGBA PNG, and that it isn't corrupted!");
            }

            textureData.Apply(true);

            for (int j = 0; j < mipMapCount; j++)
            {
                textureArray.SetPixelData(textureData.GetPixels32(j), j, i);
            }
        }

        textureArray.Apply(true);

        chunkMaterial.SetTexture("_TextureArray", textureArray);
    }
}
