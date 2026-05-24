using Godot;
using System.Collections.Generic;
public static class AssetManager
{
    // Saved Stuff
    public static Dictionary<string, Biome> biomes = [];
    public static Dictionary<string, Building> buildings = [];
    public static Dictionary<string, Profession> professions = [];
    public static Dictionary<BuildingType, List<Building>> buildingTypes = [];
    public static Dictionary<string, Item> items = [];
    public static Dictionary<string, List<Item>> itemTags = [];
    public static Dictionary<string, NaturalResource> naturalResources = [];

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
        itemTags = [];

        professions = [];

        naturalResources = [];

        LoadResources("Data/Biomes", biomes);
        LoadResources("Data/Buildings", buildings);
        LoadResources("Data/Professions", professions);
        LoadResources("Data/Items", items); 
        LoadResources("Data/Natural Resources", naturalResources);

        foreach (var pair in buildings)
        {
            if (!buildingTypes.ContainsKey(pair.Value.type)) buildingTypes[pair.Value.type] = [];
            buildingTypes[pair.Value.type].Add(pair.Value);
        }

        foreach (Item item in items.Values)
        {
            foreach (string tag in item.tags)
            {
                if (!itemTags.ContainsKey(tag)) itemTags[tag] = [];
                itemTags[tag].Add(item);                
            }
        }    
    }
    public static Biome GetBiome(string id)
    {
        return biomes[id];
    }
    public static Item GetItem(string id)
    {
        return items[id];
    }
    public static NaturalResource GetNaturalResource(string id)
    {
        return naturalResources[id];
    }
    public static Building GetBuilding(string id)
    {
        return buildings[id];
    }
    public static Profession GetProfession(string id)
    {
        return professions[id];
    }
}