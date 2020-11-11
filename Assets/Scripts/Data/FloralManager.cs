public class FloralManager : ItemTypeManager<FloralEntry>
{
    public static FloralManager Inst = new FloralManager();

    public void AddFloral(FloralEntry[] biomes)
    {
        for (int i = 0; i < biomes.Length; i++)
        {
            _data.Add(biomes[i].Floral.InternalName, biomes[i]);
        }
    }
}
