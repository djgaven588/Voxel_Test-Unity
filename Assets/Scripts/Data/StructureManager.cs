public class StructureManager : ItemTypeManager<Structure>
{
    public static StructureManager Inst = new StructureManager();

    public void AddStructures(Structure[] structures)
    {
        for (int i = 0; i < structures.Length; i++)
        {
            _data.Add(structures[i].Name, structures[i]);
        }
    }
}
