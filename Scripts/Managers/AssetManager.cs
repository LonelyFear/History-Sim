using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;
public static class AssetManager
{
    // Saved Stuff
    public static Dictionary<string, Biome> biomes = [];
    public static Dictionary<string, SocialClass> professions = [];
    public static Dictionary<string, Good> goods = [];
    public static Dictionary<string, NaturalResource> naturalResources = [];

    public static void LoadResources<ResType>(string resPath, Dictionary<string, ResType> output) where ResType : SimResource
    {    
        string[] resourceList = ResourceLoader.ListDirectory(resPath);
        GD.Print(resourceList);
        foreach (string fileName in resourceList)
        {
            ResType res = GD.Load<ResType>(resPath.PathJoin(fileName));
            string id = "";
            foreach (char c in fileName)
            {
                if (c == '.') break; 
                id += c;               
            }
            
            res.id = id;
            output.Add(id, res);   
            //GD.Print("Loaded " + id);             
        }                 
    }
    public static void LoadAssets()
    {
        biomes = [];
        goods = [];

        professions = [];

        naturalResources = [];

        LoadResources("res://Data/Biomes", biomes);
        LoadResources("res://Data/Professions", professions);
        LoadResources("res://Data/Goods", goods); 
        LoadResources("res://Data/Natural Resources", naturalResources);
    }
    public static Biome GetBiome(string id)
    {
        return biomes[id];
    }
    public static Good GetItem(string id)
    {
        return goods[id];
    }
    public static NaturalResource GetNaturalResource(string id)
    {
        return naturalResources[id];
    }
    public static SocialClass GetSocialClass(string id)
    {
        return professions[id];
    }
}