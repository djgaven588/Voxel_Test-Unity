using Modding;
using UnityEngine;

public class Initialization : MonoBehaviour
{
    public static Initialization Inst;

    public Material ChunkMaterial;

    public Transform Outline;
    public Transform OutlineVisual;
    public Transform OutlineSelected;

    public bool SavingEnabled;

    public string[] ModFolders;

    public PlayerController Player;

    public Material BiomeMap;

    public void Awake()
    {
        Inst = this;
    }

    public void OnApplicationQuit()
    {
        WorldManager.OnApplicationQuit();
    }

    public void Start()
    {
        TextureManager.Inst.Init(ChunkMaterial, ModFolders);
        ModelManager.Inst.Init();
        BlockManager.Inst.Init();
        StructureManager.Inst.Init();

        ModManager.Inst.LoadAllMods(ModFolders);

        ChunkStructureGenerator.Init();

        WorldManager.Init(SavingEnabled);
    }

    public void Update()
    {
        WorldManager.Update();
    }
}
