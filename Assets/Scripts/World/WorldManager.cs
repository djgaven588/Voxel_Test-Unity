using System.Collections.Generic;

/// <summary>
/// In case you are wondering, yes this class is useless when
/// there is only 1 possible active world. Perhaps this could
/// be used for dimensions?
/// </summary>
public static class WorldManager
{
    private static readonly Dictionary<int, World> _worlds = new Dictionary<int, World>();
    private static int _activeWorld = 0;

    public static void Init(bool savingEnabled)
    {
        _worlds.Add(0, new World(0, "Default", "Overworld", savingEnabled));
    }

    public static void OnApplicationQuit()
    {
        foreach (World world in _worlds.Values)
        {
            world.OnApplicationQuit();
        }
    }

    public static void Update()
    {
        _worlds[_activeWorld].Update();
    }
}
