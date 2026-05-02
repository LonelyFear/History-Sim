using Godot;
using System.Collections.Generic;
public static class AssetManager
{
    // Saved Stuff
    public static Dictionary<string, Biome> biomes = [];
    public static Dictionary<string, Building> buildings = [];
    public static Dictionary<BuildingType, List<string>> buildingTypes = [];
    public static Dictionary<string, Item> items = [];

    public static void LoadResources<ResType>(string resPath, Dictionary<string, ResType> output, bool deepSearch = true) where ResType : SimResource
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
                GD.Print(resPath.PathJoin(fileName));
                ResType res = GD.Load<ResType>(resPath.PathJoin(fileName));
                res.id = fileName[..^5];
                output.Add(res.id, res);                
            }
            fileName = dir.GetNext();
        }     
    }
    public static void LoadAssets()
    {
        biomes = [];
        buildings = [];
        buildingTypes = [];
        items = [];


        LoadResources("Data/Biomes", biomes);
        LoadResources("Data/Buildings", buildings);

        foreach (var pair in buildings)
        {
            if (!buildingTypes.ContainsKey(pair.Value.type)) buildingTypes[pair.Value.type] = [];
            buildingTypes[pair.Value.type].Add(pair.Key);
        }

        LoadResources("Data/Items", items);
    }
    public static Biome GetBiome(string id)
    {
        return biomes[id];
    }
    public static Item GetItem(string id)
    {
        return items[id];
    }
    public static Building GetBuilding(string id)
    {
        return buildings[id];
    }
}