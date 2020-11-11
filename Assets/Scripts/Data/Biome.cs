using Noise;
using UnityEngine;

public class Biome
{
    public string BiomeName;

    public Block SurfaceBlock;
    public Block SubsurfaceBlock;
    public Block UndergroundBlock;

    public int SubsurfaceDepth;

    public Vector2 HumidityRange;
    public Vector2 TemperatureRange;

    public Color Color = Color.white;

    public NoiseSet[] NoiseInformation;
    public bool GeneratesAboveGroundLevel = false;

    public struct NoiseSet
    {
        public double Scale;
        public double Amplitude;
        public double Exponential;
        public double ExponentialDownscale;

        public double OctaveMultiplier;
        public double OctaveOffset;
    }

    private OpenSimplexNoise Noise;

    public Biome()
    {
        Noise = new OpenSimplexNoise(0);
    }

    public virtual double GetHeightValue(double x, double z)
    {
        return GetValue(x, z);
    }

    public virtual Block GetBlock(int groundHeight, int currentHeight, System.Random rng)
    {
        if (groundHeight == currentHeight)
        {
            return SurfaceBlock;
        }
        else if (currentHeight >= groundHeight - SubsurfaceDepth)
        {
            return SubsurfaceBlock;
        }
        else
        {
            return UndergroundBlock;
        }
    }

    private double GetNoise(double x, double z, double scale, double amplitude, double exponential, double exponentialDownscale)
    {
        double value = (Noise.Evaluate(x * scale, z * scale) + 1) / 2 * amplitude;

        value += (value * exponential * value * exponential) / exponentialDownscale;

        return value;
    }

    private double GetValue(double x, double z)
    {
        double totalOctaveValue = 0;
        for (int i = 0; i < NoiseInformation.Length; i++)
        {
            NoiseSet set = NoiseInformation[i];
            totalOctaveValue += GetNoise(x, z, set.Scale, set.Amplitude, set.Exponential, set.ExponentialDownscale) * set.OctaveMultiplier + set.OctaveOffset;
        }

        return totalOctaveValue;
    }
}
