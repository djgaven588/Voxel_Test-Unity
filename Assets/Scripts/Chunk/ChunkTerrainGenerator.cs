using Noise;
using System.Collections.Generic;
using UnityEngine;

public class ChunkTerrainGenerator
{
    private OpenSimplexNoise Noise;

    private Block Air;
    private Block Water;

    private Biome DefaultBiome;

    private Biome[] Biomes;
    private FloralEntry[] PotentialFloral;

    private const int WATER_LEVEL = 55;
    private const float BIOME_SIZE = 5f;

    public virtual void Init()
    {
        Noise = new OpenSimplexNoise(0);

        DefaultBiome = BiomeManager.Inst.GetBiomeOrDefault("Default Biome");

        Biomes = BiomeManager.Inst.GetAllBiomes();

        PotentialFloral = FloralManager.Inst.GetAll();

        int biomeMapResolution = 1024;
        Texture2D texture = new Texture2D(biomeMapResolution, biomeMapResolution, TextureFormat.ARGB32, false);
        for (int x = 0; x < biomeMapResolution; x++)
        {
            for (int y = 0; y < biomeMapResolution; y++)
            {
                Biome biome = GetBiomeData(x / (float)biomeMapResolution, 1 - y / (float)biomeMapResolution);
                texture.SetPixel(x, y, biome.Color);
            }
        }
        texture.filterMode = FilterMode.Point;
        texture.Apply();

        Initialization.Inst.BiomeMap.SetTexture("_MainTex", texture);

        Air = BlockManager.Inst.GetBlockOrDefault("Base/Block/Air");
        Water = BlockManager.Inst.GetBlockOrDefault("Base/Block/Water");
    }

    public virtual void GenerateBiomes(Chunk chunk, int seed)
    {
        Biome[] biomes = new Biome[(Chunk.CHUNK_SIZE + Chunk.BIOME_BLEND_DISTANCE * 2) * (Chunk.CHUNK_SIZE + Chunk.BIOME_BLEND_DISTANCE * 2)];

        for (int x = -Chunk.BIOME_BLEND_DISTANCE; x < Chunk.CHUNK_SIZE + Chunk.BIOME_BLEND_DISTANCE; x++)
        {
            for (int z = -Chunk.BIOME_BLEND_DISTANCE; z < Chunk.CHUNK_SIZE + Chunk.BIOME_BLEND_DISTANCE; z++)
            {
                double xPos = x + chunk.Position.x * Chunk.CHUNK_SIZE;
                double zPos = z + chunk.Position.z * Chunk.CHUNK_SIZE;
                (double temperature, double humidity) = GetBiomeValues(xPos, zPos);
                Biome biome = GetBiomeData(temperature, humidity);
                biomes[x + Chunk.BIOME_BLEND_DISTANCE + (z + Chunk.BIOME_BLEND_DISTANCE) * (Chunk.CHUNK_SIZE + Chunk.BIOME_BLEND_DISTANCE * 2)] = biome;
            }
        }

        chunk.SetBiomeData(biomes);
    }

    public string GetDebugData(double xPos, double zPos)
    {
        string data = "";
        (double temperature, double humidity) = GetBiomeValues(xPos, zPos);
        Biome biome = GetBiomeData(temperature, humidity);

        data += $"Temp: {temperature} - Humid: {humidity} ";
        data += $"Biome: {biome.BiomeName}";
        return data;
    }

    public virtual void GenerateTerrain(Chunk chunk, int seed)
    {
        System.Random rng = new System.Random(unchecked(seed + chunk.Position.GetHashCode()));
        Vector3Int worldPosition = chunk.Position * Chunk.CHUNK_SIZE;

        var biomeLocker = chunk.GetBiomeLock();
        biomeLocker.EnterReadLock();

        var locker = chunk.GetLock();
        locker.EnterWriteLock();
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
            {
                int globalX = x + worldPosition.x;
                int globalZ = z + worldPosition.z;

                (double temperature, double humidity) = GetBiomeValues(globalX, globalZ);

                Biome primaryBiome = GetPrimaryAndHeight(chunk, x, z, globalX, globalZ, out double height);
                int terrainHeight = (int)height;

                Block lastBlock = null;
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    int globalY = y + worldPosition.y;
                    int index = x + y * Chunk.CHUNK_SIZE + z * Chunk.CHUNK_SIZE_SQR;
                    Block newBlock = Air;

                    if (globalY > terrainHeight)
                    {
                        if (globalY <= WATER_LEVEL)
                        {
                            newBlock = Water;
                        }
                        else if (globalY - 1 == terrainHeight)
                        {
                            if (lastBlock != null && GetFloral(lastBlock, temperature, humidity, globalY, rng, out Block floral))
                            {
                                newBlock = floral;
                            }
                        }
                    }
                    else if (globalY <= terrainHeight)
                    {
                        newBlock = primaryBiome.GetBlock(terrainHeight, globalY, rng);
                    }

                    lastBlock = newBlock;

                    chunk.SetBlock(newBlock, index, true, false);
                }
            }
        }

        biomeLocker.ExitReadLock();

        locker.ExitWriteLock();
    }

    private Biome GetPrimaryAndHeight(Chunk chunk, int chunkPosX, int chunkPosZ, int x, int z, out double height)
    {
        Biome primaryBiome = chunk.GetBiome(chunkPosX, chunkPosZ, false);

        double x1, x2, y1, y2;
        x1 = x2 = y1 = y2 = primaryBiome.GetHeightValue(x, z);

        if (AttemptFindBiomeTransition(chunk, chunkPosX, chunkPosZ, primaryBiome, x, z, Chunk.BIOME_BLEND_DISTANCE, 1, 0, out int steps1, out double biomeHeight1))
        {
            x2 = biomeHeight1;
        }

        if (AttemptFindBiomeTransition(chunk, chunkPosX, chunkPosZ, primaryBiome, x, z, Chunk.BIOME_BLEND_DISTANCE, -1, 0, out int steps2, out double biomeHeight2))
        {
            x1 = biomeHeight2;
        }

        double biasX = (1.0 - steps1 / (double)(Chunk.BIOME_BLEND_DISTANCE + 1) + steps2 / (double)(Chunk.BIOME_BLEND_DISTANCE + 1)) / 2;

        if (AttemptFindBiomeTransition(chunk, chunkPosX, chunkPosZ, primaryBiome, x, z, Chunk.BIOME_BLEND_DISTANCE, 0, 1, out int steps3, out double biomeHeight3))
        {
            y2 = biomeHeight3;
        }

        if (AttemptFindBiomeTransition(chunk, chunkPosX, chunkPosZ, primaryBiome, x, z, Chunk.BIOME_BLEND_DISTANCE, 0, -1, out int steps4, out double biomeHeight4))
        {
            y1 = biomeHeight4;
        }

        double biasZ = (1.0 - steps3 / (double)(Chunk.BIOME_BLEND_DISTANCE + 1) + steps4 / (double)(Chunk.BIOME_BLEND_DISTANCE + 1)) / 2;

        height = Mathf.LerpUnclamped(Mathf.Lerp((float)x1, (float)x2, (float)biasX), Mathf.Lerp((float)y1, (float)y2, (float)biasZ), 0.5f);
        return primaryBiome;
    }

    private bool AttemptFindBiomeTransition(Chunk chunk, int chunkPosX, int chunkPosZ, Biome primaryBiome, int x, int z, int maxDistance, int xMove, int zMove, out int steps, out double height)
    {
        for (int dist = 1; dist <= maxDistance; dist++)
        {
            int newX = x + xMove * dist;
            int newZ = z + zMove * dist;

            Biome biome = chunk.GetBiome(chunkPosX + xMove * dist, chunkPosZ + zMove * dist, false);
            if (biome != primaryBiome)
            {
                steps = dist;
                height = biome.GetHeightValue(newX, newZ);

                return true;
            }
        }

        steps = 0;
        height = 0;

        return false;
    }

    private Biome GetBiomeData(double temperature, double humidity)
    {
        Biome chosenBiome = null;
        double highestSatisfaction = 0;
        for (int i = 0; i < Biomes.Length; i++)
        {
            Biome currentBiome = Biomes[i];
            if (RangeContains(currentBiome.TemperatureRange, temperature) && RangeContains(currentBiome.HumidityRange, humidity))
            {
                double satisfaction = BiomeSatisfaction(currentBiome, temperature, humidity);
                if (chosenBiome == null || highestSatisfaction < satisfaction)
                {
                    chosenBiome = currentBiome;
                    highestSatisfaction = satisfaction;
                }
            }
        }

        if (chosenBiome == null)
        {
            chosenBiome = DefaultBiome;
        }

        return chosenBiome;
    }

    private double BiomeSatisfaction(Biome biome, double temperature, double humidity)
    {
        double temperatureSatisfaction = (temperature - biome.TemperatureRange.x) / (biome.TemperatureRange.y - biome.TemperatureRange.x);

        if (temperatureSatisfaction >= 0.5f)
        {
            temperatureSatisfaction = 1 - temperatureSatisfaction;
        }

        double humiditySatisfaction = (humidity - biome.HumidityRange.x) / (biome.HumidityRange.y - biome.HumidityRange.x);

        if (humiditySatisfaction >= 0.5f)
        {
            humiditySatisfaction = 1 - humiditySatisfaction;
        }

        return temperatureSatisfaction + humiditySatisfaction;
    }

    private bool GetFloral(Block blockBelow, double temperature, double humidity, int y, System.Random rng, out Block floral)
    {
        List<(FloralEntry, float)> canSpawn = new List<(FloralEntry, float)>();

        for (int i = 0; i < PotentialFloral.Length; i++)
        {
            FloralEntry potentialFloral = PotentialFloral[i];
            if (RangeContains(potentialFloral.TemperatureRange, temperature) && RangeContains(potentialFloral.HumidityRange, humidity) &&
                RangeContains(potentialFloral.HeightRange, y) && potentialFloral.Floral.CanGrowOn(blockBelow))
            {
                canSpawn.Add((potentialFloral, (float)FloralSatisfaction(potentialFloral, temperature, humidity, y)));
            }
        }

        if (canSpawn.Count == 0)
        {
            floral = null;
            return false;
        }

        (FloralEntry choosenFlora, float satisfaction) = canSpawn[rng.Next(0, canSpawn.Count)];
        float spawnChance = choosenFlora.SpawnChance * satisfaction;
        if (spawnChance >= 1)
        {
            floral = choosenFlora.Floral;
            return true;
        }
        else if (rng.NextDouble() <= spawnChance)
        {
            floral = choosenFlora.Floral;
            return true;
        }

        floral = null;
        return false;
    }

    private bool RangeContains(Vector2 range, double value)
    {
        return range.x <= value && value <= range.y;
    }

    private bool RangeContains(Vector2 range, int value)
    {
        return range.x <= value && value <= range.y;
    }

    /// <summary>
    /// Returns the "satisfation" the given floral would have at the given temperature and humidity.
    /// Floral prefers to be in the center of its "satisfaction" range, and balances most factors.
    /// </summary>
    /// <param name="entry">The floral to check</param>
    /// <param name="temperature">The temperature to check</param>
    /// <param name="humidity">The humidity to check</param>
    /// /// <param name="height">The height to check</param>
    /// <returns></returns>
    private double FloralSatisfaction(FloralEntry entry, double temperature, double humidity, int height)
    {
        double temperatureSatisfaction = (temperature - entry.TemperatureRange.x) / (entry.TemperatureRange.y - entry.TemperatureRange.x);

        if (temperatureSatisfaction >= 0.5f)
        {
            temperatureSatisfaction = 1 - temperatureSatisfaction;
        }

        double humiditySatisfaction = (humidity - entry.HumidityRange.x) / (entry.HumidityRange.y - entry.HumidityRange.x);

        if (humiditySatisfaction >= 0.5f)
        {
            humiditySatisfaction = 1 - humiditySatisfaction;
        }

        double heightSatisfaction = (height - entry.HeightRange.x) / (entry.HeightRange.y - entry.HeightRange.x);

        if (heightSatisfaction >= 0.5f)
        {
            heightSatisfaction = 1 - heightSatisfaction;
        }

        return (temperatureSatisfaction + humiditySatisfaction + heightSatisfaction) / 1.5;
    }

    private (double temperature, double humidity) GetBiomeValues(double x, double z)
    {
        double octave1 = GetBiomeNoise(x, z, 0.001d / BIOME_SIZE, 1, 1, 1);
        double octave2 = GetBiomeNoise(x, -z, 0.01d / BIOME_SIZE, 1, 1, 1);

        double octave3 = GetBiomeNoise(x, -z, 0.002d / BIOME_SIZE, 1, 1, 1);
        double octave4 = GetBiomeNoise(-x, z, 0.02d / BIOME_SIZE, 1, 1, 1);

        return (Mathf.Clamp((float)octave1 * 0.75f + (float)octave2 * 0.25f, 0.01f, 0.99f), Mathf.Clamp((float)octave3 * 0.75f + (float)octave4 * 0.25f, 0.01f, 0.99f));
    }

    private double GetBiomeNoise(double x, double z, double scale, double amplitude, double exponential, double exponentialDownscale)
    {
        double value = ((Noise.Evaluate(x * scale, z * scale) + 1) / 2) * amplitude;

        value += (value * exponential * value * exponential) / exponentialDownscale;

        return value;
    }
}
