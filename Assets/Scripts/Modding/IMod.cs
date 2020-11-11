namespace Modding
{
    public interface IMod
    {
        ModVersion Version { get; }
        string Name { get; }
        string[] LoadBeforeDependencies { get; }
        string[] LoadAfterDependencies { get; }

        void Init();
        Block[] LoadBlocks(TextureManager textures, BlockManager alreadyLoadedBlocks, ModelManager models, int startingId);
        Biome[] LoadBiomes(BlockManager alreadyLoadedBlocks);
        FloralEntry[] LoadFloral(BlockManager alreadyLoadedBlocks, BiomeManager biomes);
        Structure[] LoadStructures(BlockManager blocks, BiomeManager biomes);
    }
}
