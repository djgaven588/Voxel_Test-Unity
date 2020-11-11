using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BiomeManager
{
    private readonly Dictionary<string, Biome> Biomes = new Dictionary<string, Biome>();

    public static BiomeManager Inst = new BiomeManager();

    private Biome DefaultBiome;

    public void Init(BlockManager blocks)
    {
        DefaultBiome = new Biome()
        {
            BiomeName = "Default Biome",
            HumidityRange = Vector2.zero,
            TemperatureRange = Vector2.zero,
            NoiseInformation = new Biome.NoiseSet[]
            {
                new Biome.NoiseSet()
                {
                    Scale = 0.025,
                    Amplitude = 4,
                    Exponential = 2,
                    ExponentialDownscale = 2,
                    OctaveMultiplier = 1,
                    OctaveOffset = 35
                }
            },
            SurfaceBlock = blocks.GetBlockOrDefault("Base/Block/Stone"),
            SubsurfaceBlock = blocks.GetBlockOrDefault("Base/Block/Dirt"),
            SubsurfaceDepth = 3,
            UndergroundBlock = blocks.GetBlockOrDefault("Base/Block/Stone")
        };

        Biomes.Add(DefaultBiome.BiomeName, DefaultBiome);
    }

    public Biome GetBiomeOrDefault(string name)
    {
        if (Biomes.TryGetValue(name, out Biome value))
        {
            return value;
        }

        return DefaultBiome;
    }

    public void AddBiomes(Biome[] biomes)
    {
        for (int i = 0; i < biomes.Length; i++)
        {
            Biomes.Add(biomes[i].BiomeName, biomes[i]);
        }
    }

    public Biome[] GetAllBiomes()
    {
        return Biomes.Values.ToArray();
    }
}
