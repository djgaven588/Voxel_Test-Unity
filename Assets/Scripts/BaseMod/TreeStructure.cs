using System.Collections.Generic;
using UnityEngine;

public class TreeStructure : Structure
{
    private Varient[] Varients;
    private VarientConditions[] Conditions;
    private Block _air;

    private struct VarientConditions
    {
        public Biome Biome;
        public Block Below;
    }

    public TreeStructure(BlockManager blocks, BiomeManager biomes)
    {
        Name = "Base/Structures/Trees";
        _air = blocks.GetBlockOrDefault("Base/Block/Air");
        Block grass = blocks.GetBlockOrDefault("Base/Block/Grass");

        VarientConditions deepForest = new VarientConditions() { Below = grass, Biome = biomes.GetBiomeOrDefault("Deep Forest") };
        Conditions = new VarientConditions[]
        {
            deepForest, deepForest
        };

        Block wood = blocks.GetBlockOrDefault("Base/Block/Oak Wood");
        Block leaves = blocks.GetBlockOrDefault("Base/Block/Oak Leaves");
        Varients = new Varient[]
        {
            new Varient() {
                Entries = StructureBuilder.Start()
                .Add(new StructureBuilder.Box(Vector3Int.zero, Vector3Int.up * 6), wood, true)
                .Add(new StructureBuilder.Box(new Vector3Int(-2, 4, -3), new Vector3Int(2, 6, 3)), leaves, false, false)
                .Add(new StructureBuilder.Box(new Vector3Int(-3, 4, -2), new Vector3Int(3, 6, 2)), leaves, false, false)
                .Add(new StructureBuilder.Box(new Vector3Int(-2, 7, -2), new Vector3Int(2, 7, 2)), leaves, false, false).Finish(),
                ChanceToSpawn = 0.025f
            },
            new Varient() {
                Entries = StructureBuilder.Start()
                .Add(new StructureBuilder.Box(Vector3Int.zero, Vector3Int.up * 3), wood, true)
                .Add(new StructureBuilder.Box(new Vector3Int(-1, 2, -2), new Vector3Int(1, 3, 2)), leaves, false, false)
                .Add(new StructureBuilder.Box(new Vector3Int(-2, 2, -1), new Vector3Int(2, 3, 1)), leaves, false, false)
                .Add(new StructureBuilder.Box(new Vector3Int(-1, 4, -1), new Vector3Int(1, 4, 1)), leaves, false, false).Finish(),
                ChanceToSpawn = 0.075f
            }
        };
    }

    public override bool ConditionsMet(System.Random rng, Block below, Block current, Biome biome, Vector3Int worldPosition, out Varient varient)
    {
        if (current != _air)
        {
            varient = null;
            return false;
        }

        if (DetermineClosestMatch(below, biome, out Varient[] varients))
        {
            varient = varients[rng.Next(varients.Length)];
            if (rng.NextDouble() < varient.ChanceToSpawn)
            {
                return true;
            }
            return false;
        }

        varient = null;
        return false;
    }

    private bool DetermineClosestMatch(Block below, Biome biome, out Varient[] varients)
    {
        List<Varient> vars = new List<Varient>();
        for (int i = 0; i < Conditions.Length; i++)
        {
            if (Conditions[i].Below != below || Conditions[i].Biome != biome)
            {
                continue;
            }

            vars.Add(Varients[i]);
        }

        varients = vars.ToArray();
        return vars.Count > 0;
    }
}
