using Modding;
using System.Collections.Generic;
using UnityEngine;

public class CoreMod : IMod
{
    public ModVersion Version => new ModVersion(1, 0, 0);

    public string Name => "Base";

    public string[] LoadBeforeDependencies => new string[0];

    public string[] LoadAfterDependencies => new string[0];

    public void Init()
    {

    }

    public Block[] LoadBlocks(TextureManager textures, BlockManager alreadyLoadedBlocks, ModelManager models, int startingId)
    {
        Bounds standardBounds = new Bounds(Vector3.one / 2, Vector3.one * 1.001f);

        List<Block> blockList = new List<Block>();

        uint dirtTexture = textures.GetTextureOrDefault("Base/Textures/Dirt");
        uint grassTopTexture = textures.GetTextureOrDefault("Base/Textures/GrassTop");
        uint grassSideTexture = textures.GetTextureOrDefault("Base/Textures/GrassSide");
        uint stoneTexture = textures.GetTextureOrDefault("Base/Textures/Stone");
        uint woodSideTexture = textures.GetTextureOrDefault("Base/Textures/Wood");
        uint woodTopTexture = textures.GetTextureOrDefault("Base/Textures/WoodTop");
        uint leavesTexture = textures.GetTextureOrDefault("Base/Textures/Leaf");

        blockList.Add(new Block(startingId++, "Base/Block/Dirt", "Dirt", new Model(models.GetEntryOrDefault("Base/Model/Solid"), new uint[] {
                dirtTexture, dirtTexture, dirtTexture, dirtTexture, dirtTexture, dirtTexture
        }), standardBounds));

        Block grass = new Block(startingId++, "Base/Block/Grass", "Grass", new Model(models.GetEntryOrDefault("Base/Model/Solid"), new uint[] {
            grassSideTexture, grassSideTexture, grassSideTexture, grassSideTexture, grassTopTexture, dirtTexture
        }), standardBounds);
        blockList.Add(grass);

        blockList.Add(new Block(startingId++, "Base/Block/Stone", "Stone", new Model(models.GetEntryOrDefault("Base/Model/Solid"), new uint[] {
                stoneTexture, stoneTexture, stoneTexture, stoneTexture, stoneTexture, stoneTexture
        }), standardBounds));

        blockList.Add(new Block(startingId++, "Base/Block/Oak Wood", "Oak Wood", new Model(models.GetEntryOrDefault("Base/Model/Solid"), new uint[] {
                woodSideTexture, woodSideTexture, woodSideTexture, woodSideTexture, woodTopTexture, woodTopTexture
        }), standardBounds));

        blockList.Add(new Block(startingId++, "Base/Block/Oak Leaves", "Oak Leaves", new Model(models.GetEntryOrDefault("Base/Model/Solid"), new uint[] {
                leavesTexture, leavesTexture, leavesTexture, leavesTexture, leavesTexture, leavesTexture
        }, false), standardBounds));

        uint sandTexture = TextureManager.Inst.GetTextureOrDefault("Base/Textures/Sand");
        blockList.Add(new Block(startingId++, "Base/Block/Sand", "Sand", new Model(models.GetEntryOrDefault("Base/Model/Solid"), new uint[] {
                sandTexture, sandTexture, sandTexture, sandTexture, sandTexture, sandTexture
        }), standardBounds));

        uint sandStoneTexture = TextureManager.Inst.GetTextureOrDefault("Base/Textures/Sandstone");
        blockList.Add(new Block(startingId++, "Base/Block/Sandstone", "Sandstone", new Model(models.GetEntryOrDefault("Base/Model/Solid"), new uint[] {
                sandStoneTexture, sandStoneTexture, sandStoneTexture, sandStoneTexture, sandStoneTexture, sandStoneTexture
        }), standardBounds));

        uint iceTexture = TextureManager.Inst.GetTextureOrDefault("Base/Textures/Ice");
        blockList.Add(new Block(startingId++, "Base/Block/Ice", "Ice", new Model(models.GetEntryOrDefault("Base/Model/Solid"), new uint[] {
                iceTexture, iceTexture, iceTexture, iceTexture, iceTexture, iceTexture
        }), standardBounds));

        blockList.Add(new PlantBlock(startingId++, "Base/Block/GrassPlant", "Grass", new Model(models.GetEntryOrDefault("Base/Model/Cross"), new uint[] {
            textures.GetTextureOrDefault("Base/Textures/GrassPlant")
        }), new Bounds(new Vector3(0.5f, 0.375f, 0.5f), new Vector3(0.75f, 0.75f, 0.75f)), false, true, new Block[] { grass }));

        blockList.Add(new PlantBlock(startingId++, "Base/Block/Poppy", "Poppy", new Model(models.GetEntryOrDefault("Base/Model/Cross"), new uint[]
        {
            textures.GetTextureOrDefault("Base/Textures/PoppyPlant")
        }, false), new Bounds(new Vector3(0.5f, 0.421875f, 0.5f), new Vector3(0.34375f, 0.78125f, 0.34375f)), false, false, new Block[] { grass }));

        blockList.Add(new PlantBlock(startingId++, "Base/Block/Dandelion", "Dandelion", new Model(models.GetEntryOrDefault("Base/Model/Cross"), new uint[]
        {
            textures.GetTextureOrDefault("Base/Textures/DandelionPlant")
        }, false), new Bounds(new Vector3(0.5f, 0.328125f, 0.5f), new Vector3(0.375f, 0.625f, 0.375f)), false, false, new Block[] { grass }));

        uint waterTexture = textures.GetTextureOrDefault("Base/Textures/Water");
        WaterModel waterModel = new WaterModel(models.GetEntryOrDefault("Base/Model/Solid"), new uint[] {
            waterTexture, waterTexture, waterTexture, waterTexture, waterTexture, waterTexture,
            waterTexture, waterTexture, waterTexture, waterTexture, waterTexture, waterTexture
        });
        Block waterBlock = new Block(startingId++, "Base/Block/Water", "Water", waterModel, standardBounds, true, true, true);
        waterModel.SetWater(waterBlock);
        blockList.Add(waterBlock);

        return blockList.ToArray();
    }

    public Biome[] LoadBiomes(BlockManager alreadyLoadedBlocks)
    {
        List<Biome> biomes = new List<Biome>();

        Block grass = alreadyLoadedBlocks.GetBlockOrDefault("Base/Block/Grass");
        Block dirt = alreadyLoadedBlocks.GetBlockOrDefault("Base/Block/Dirt");
        Block stone = alreadyLoadedBlocks.GetBlockOrDefault("Base/Block/Stone");
        Block water = alreadyLoadedBlocks.GetBlockOrDefault("Base/Block/Water");
        Block air = alreadyLoadedBlocks.GetBlockOrDefault("Base/Block/Air");

        biomes.Add(new Biome()
        {
            BiomeName = "Mountains",
            HumidityRange = new Vector2(0.35f, 0.8f),
            TemperatureRange = new Vector2(0.15f, 0.6f),
            NoiseInformation = new Biome.NoiseSet[]
                {
                    new Biome.NoiseSet()
                    {
                        Scale = 0.05,
                        Amplitude = 3.75f,
                        Exponential = 2,
                        ExponentialDownscale = 4,
                        OctaveMultiplier = 1,
                        OctaveOffset = 50
                    }
                },
            SurfaceBlock = grass,
            SubsurfaceBlock = dirt,
            SubsurfaceDepth = 6,
            UndergroundBlock = stone,
            Color = new Color(0.1f, 0.1f, 0.1f)
        });
        biomes.Add(new Biome()
        {
            BiomeName = "Plains",
            HumidityRange = new Vector2(0.075f, 0.30f),
            TemperatureRange = new Vector2(0.20f, 0.7f),
            NoiseInformation = new Biome.NoiseSet[]
                {
                    new Biome.NoiseSet()
                    {
                        Scale = 0.01,
                        Amplitude = 7,
                        Exponential = 1,
                        ExponentialDownscale = 1,
                        OctaveMultiplier = 1,
                        OctaveOffset = 45
                    },
                    new Biome.NoiseSet()
                    {
                        Scale = 0.05,
                        Amplitude = 3,
                        Exponential = 2,
                        ExponentialDownscale = 1,
                        OctaveMultiplier = 0.1,
                        OctaveOffset = 0
                    }
                },
            SurfaceBlock = grass,
            SubsurfaceBlock = dirt,
            SubsurfaceDepth = 6,
            UndergroundBlock = stone,
            Color = new Color(0.1f, 0.3f, 0.1f)
        });
        biomes.Add(new Biome()
        {
            BiomeName = "Forest",
            HumidityRange = new Vector2(0.25f, 1f),
            TemperatureRange = new Vector2(-0.1f, 0.4f),
            NoiseInformation = new Biome.NoiseSet[]
                {
                    new Biome.NoiseSet()
                    {
                        Scale = 0.015,
                        Amplitude = 3,
                        Exponential = 5,
                        ExponentialDownscale = 6,
                        OctaveMultiplier = 0.5f
                    },
                    new Biome.NoiseSet()
                    {
                        Scale = 0.035,
                        Amplitude = 7,
                        Exponential = 1,
                        ExponentialDownscale = 1,
                        OctaveMultiplier = 0.5f
                    },
                    new Biome.NoiseSet()
                    {
                        Scale = 0.1,
                        Amplitude = 1,
                        Exponential = 2,
                        ExponentialDownscale = 1,
                        OctaveMultiplier = 1f
                    },
                    new Biome.NoiseSet()
                    {
                        Scale = 0.001,
                        Amplitude = 1,
                        Exponential = 3,
                        ExponentialDownscale = 1,
                        OctaveMultiplier = 1f,
                        OctaveOffset = 40
                    }
                },
            SurfaceBlock = grass,
            SubsurfaceBlock = dirt,
            SubsurfaceDepth = 6,
            UndergroundBlock = stone,
            Color = new Color(0.2f, 0.5f, 0.2f)
        });
        biomes.Add(new Biome()
        {
            BiomeName = "Deep Forest",
            HumidityRange = new Vector2(0.3125f, 1f),
            TemperatureRange = new Vector2(0.2f, 0.7f),
            NoiseInformation = new Biome.NoiseSet[]
                {
                    new Biome.NoiseSet()
                    {
                        Scale = 0.0075,
                        Amplitude = 3,
                        Exponential = 5,
                        ExponentialDownscale = 6,
                        OctaveMultiplier = 0.5f
                    },
                    new Biome.NoiseSet()
                    {
                        Scale = 0.02,
                        Amplitude = 7,
                        Exponential = 1,
                        ExponentialDownscale = 1,
                        OctaveMultiplier = 1f
                    },
                    new Biome.NoiseSet()
                    {
                        Scale = 0.05,
                        Amplitude = 2,
                        Exponential = 2,
                        ExponentialDownscale = 1,
                        OctaveMultiplier = 1f
                    },
                    new Biome.NoiseSet()
                    {
                        Scale = 0.001,
                        Amplitude = 2,
                        Exponential = 3,
                        ExponentialDownscale = 9,
                        OctaveMultiplier = 1f,
                        OctaveOffset = 40
                    }
                },
            SurfaceBlock = grass,
            SubsurfaceBlock = dirt,
            SubsurfaceDepth = 6,
            UndergroundBlock = stone,
            Color = new Color(0.5f, .75f, 0.5f)
        });
        biomes.Add(new Biome()
        {
            BiomeName = "Rainforest",
            HumidityRange = new Vector2(0.5125f, 1f),
            TemperatureRange = new Vector2(0.65f, 1f),
            NoiseInformation = new Biome.NoiseSet[]
                {
                    new Biome.NoiseSet()
                    {
                        Scale = 0.005,
                        Amplitude = 4,
                        Exponential = 2,
                        ExponentialDownscale = 5,
                        OctaveMultiplier = 1,
                        OctaveOffset = 72
                    }
                },
            SurfaceBlock = dirt,
            SubsurfaceBlock = dirt,
            SubsurfaceDepth = 6,
            UndergroundBlock = stone,
            Color = Color.red
        });
        biomes.Add(new Biome()
        {
            BiomeName = "Tundra",
            HumidityRange = new Vector2(-0.2f, 0.5f),
            TemperatureRange = new Vector2(-0.3f, 0.4f),
            NoiseInformation = new Biome.NoiseSet[]
                {
                    new Biome.NoiseSet()
                    {
                        Scale = 0.0025,
                        Amplitude = 5,
                        Exponential = 2,
                        ExponentialDownscale = 3,
                        OctaveMultiplier = 1,
                        OctaveOffset = 55
                    }
                },
            SurfaceBlock = BlockManager.Inst.GetBlockOrDefault("Base/Block/Ice"),
            SubsurfaceBlock = dirt,
            SubsurfaceDepth = 3,
            UndergroundBlock = stone,
            Color = Color.cyan
        });
        biomes.Add(new Biome()
        {
            BiomeName = "Desert",
            HumidityRange = new Vector2(-2f, 0.4f),
            TemperatureRange = new Vector2(0.35f, 1f),
            NoiseInformation = new Biome.NoiseSet[]
                {
                    new Biome.NoiseSet()
                    {
                        Scale = 0.02,
                        Amplitude = 5,
                        Exponential = 2,
                        ExponentialDownscale = 3,
                        OctaveMultiplier = 1,
                        OctaveOffset = 70
                    }
                },
            SurfaceBlock = BlockManager.Inst.GetBlockOrDefault("Base/Block/Sand"),
            SubsurfaceBlock = BlockManager.Inst.GetBlockOrDefault("Base/Block/Sand"),
            SubsurfaceDepth = 5,
            UndergroundBlock = BlockManager.Inst.GetBlockOrDefault("Base/Block/Sandstone"),
            Color = Color.yellow
        });
        biomes.Add(new Biome()
        {
            BiomeName = "Savanna",
            HumidityRange = new Vector2(0.1f, 0.325f),
            TemperatureRange = new Vector2(0.5f, 0.95f),
            NoiseInformation = new Biome.NoiseSet[]
                {
                    new Biome.NoiseSet()
                    {
                        Scale = 0.0125,
                        Amplitude = 5,
                        Exponential = 1,
                        ExponentialDownscale = 1,
                        OctaveMultiplier = 1,
                        OctaveOffset = 62
                    }
                },
            SurfaceBlock = dirt,
            SubsurfaceBlock = dirt,
            SubsurfaceDepth = 3,
            UndergroundBlock = stone,
            Color = new Color(0.5f, 0.5f, 0.1f)
        });
        biomes.Add(new Biome()
        {
            BiomeName = "Ocean",
            HumidityRange = new Vector2(0.20f, 0.525f),
            TemperatureRange = new Vector2(0.15f, 1f),
            NoiseInformation = new Biome.NoiseSet[]
                {
                    new Biome.NoiseSet()
                    {
                        Scale = 1,
                        Amplitude = 1,
                        Exponential = 1,
                        ExponentialDownscale = 1,
                        OctaveMultiplier = 0,
                        OctaveOffset = 40
                    }
                },
            SurfaceBlock = BlockManager.Inst.GetBlockOrDefault("Base/Block/Sand"),
            SubsurfaceBlock = dirt,
            SubsurfaceDepth = 3,
            UndergroundBlock = stone,
            Color = new Color(0.1f, 0.1f, 0.9f)
        });

        return biomes.ToArray();
    }

    public FloralEntry[] LoadFloral(BlockManager alreadyLoadedBlocks, BiomeManager biomes)
    {
        List<FloralEntry> potentialFloral = new List<FloralEntry>();

        potentialFloral.Add(new FloralEntry()
        {
            Floral = (PlantBlock)alreadyLoadedBlocks.GetBlockOrDefault("Base/Block/GrassPlant"),
            HeightRange = new Vector2(55, 115),
            HumidityRange = new Vector2(0.3f, 1f),
            TemperatureRange = new Vector2(0.25f, 0.9f),
            SpawnChance = 0.9f
        });

        potentialFloral.Add(new FloralEntry()
        {
            Floral = (PlantBlock)alreadyLoadedBlocks.GetBlockOrDefault("Base/Block/Dandelion"),
            HeightRange = new Vector2(60, 85),
            HumidityRange = new Vector2(0.3f, 1f),
            TemperatureRange = new Vector2(0.3f, 0.8f),
            SpawnChance = 0.1f
        });

        potentialFloral.Add(new FloralEntry()
        {
            Floral = (PlantBlock)alreadyLoadedBlocks.GetBlockOrDefault("Base/Block/Poppy"),
            HeightRange = new Vector2(62, 80),
            HumidityRange = new Vector2(0.2f, 0.7f),
            TemperatureRange = new Vector2(0.45f, 0.9f),
            SpawnChance = 0.1f
        });

        return potentialFloral.ToArray();
    }

    public Structure[] LoadStructures(BlockManager blocks, BiomeManager biomes)
    {
        List<Structure> structures = new List<Structure>();

        structures.Add(new TreeStructure(blocks, biomes));

        return structures.ToArray();
    }
}
