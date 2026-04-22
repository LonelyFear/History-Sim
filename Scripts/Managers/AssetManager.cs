using Godot;
using System.Collections.Generic;
public static class AssetManager
{
    // Saved Stuff
    public static Dictionary<string, Biome> biomes = [];

    public static void LoadResources<ResType>(string resPath, Dictionary<string, ResType> output, bool deepSearch = true) where ResType : Resource
    {
        DirAccess dir = DirAccess.Open(resPath);

        dir.ListDirBegin();
        string fileName = dir.GetNext();

        while (fileName != "")
        {
            if (dir.CurrentIsDir() && deepSearch)
            {
                LoadResources(resPath.PathJoin(fileName), output, true);
            } else
            {
                ResType res = GD.Load<ResType>(resPath.PathJoin(fileName));
                output.Add(fileName[..^5], res);                
            }
            fileName = dir.GetNext();
        }     
    }
    public static void LoadAssets()
    {
        biomes = [];

        LoadResources("res://Data/Biomes", biomes);
    }
    public static Biome GetBiome(string id)
    {
        return biomes[id];
    }
}