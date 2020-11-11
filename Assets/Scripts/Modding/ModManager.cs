using System.Collections.Generic;

namespace Modding
{
    public class ModManager
    {
        public static ModManager Inst = new ModManager();

        private Dictionary<string, IMod> _loadedMods = new Dictionary<string, IMod>();

        public void LoadAllMods(string[] mods)
        {
            IMod baseMod = new CoreMod();
            _loadedMods.Add("Base", baseMod);

            baseMod.Init();
            BlockManager.Inst.AddBlocks(baseMod.LoadBlocks(TextureManager.Inst, BlockManager.Inst, ModelManager.Inst, BlockManager.Inst.NextId()));
            BiomeManager.Inst.AddBiomes(baseMod.LoadBiomes(BlockManager.Inst));
            FloralManager.Inst.AddFloral(baseMod.LoadFloral(BlockManager.Inst, BiomeManager.Inst));
            StructureManager.Inst.AddStructures(baseMod.LoadStructures(BlockManager.Inst, BiomeManager.Inst));

            for (int i = 0; i < mods.Length; i++)
            {
                LoadMod(mods[i]);
            }
        }

        private void LoadMod(string location)
        {

        }

        public bool IsModLoaded(string name, ModVersion minimumVersion)
        {
            if (_loadedMods.ContainsKey(name))
            {
                return _loadedMods[name].Version.IsEqualOrAbove(minimumVersion);
            }
            else
            {
                return false;
            }
        }
    }
}
