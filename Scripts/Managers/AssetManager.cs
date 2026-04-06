using Godot;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
public static class AssetManager
{
    // Saved Stuff
    public const string modsFolderPath = "Mods/";
    public static List<string> loadedModIds;
    public static List<string> foundModPaths;
    public static Dictionary<string, Biome> biomes = [];
    public static void LoadBiomes()
    {
        string biomesPath = "Data/biomes.json";
        FileAccess bio = FileAccess.Open(biomesPath, FileAccess.ModeFlags.Read);
        if (bio != null)
        {
            string biomeData = bio.GetAsText();

            foreach (Biome biome in JsonSerializer.Deserialize<Biome[]>(biomeData))
            {
                biomes.Add(biome.id, biome);
            }
            GD.Print("Loaded " + biomes.Count + " biomes");
        }
        else
        {
            GD.PushError("biomes.json not found at path '" + biomesPath + "'");
        }
    }
    public static void GetLoadedMods()
    {
        GD.Print("Asset Loading Start");
        foundModPaths = [];
        DirAccess modsDir = DirAccess.Open(modsFolderPath);

        if (modsDir != null)
        {
            foreach (string localModPath in modsDir.GetDirectories())
            {
                string modPath = modsFolderPath + localModPath;
                if (DirAccess.Open(modPath).GetFiles().Contains("mod.json"))
                {
                    foreach (string dataPath in DirAccess.Open(modPath).GetFiles())
                    {
                        if (dataPath == "mod.json")
                        {
                            string modInfoJson = FileAccess.Open(modPath + "/" + dataPath, FileAccess.ModeFlags.Read).GetAsText();
                            Dictionary<string, string> modData = JsonSerializer.Deserialize<Dictionary<string, string>>(modInfoJson);

                            if (modData.ContainsKey("name") && modData.ContainsKey("description") && modData.ContainsKey("author") && modData.ContainsKey("version"))
                            {
                                GD.Print("Found Mod '" + modData["name"] + "' by " + modData["author"]);
                                foundModPaths.Add(modPath);
                            }
                            else
                            {
                                GD.Print("Mod at path '" + modPath + "' mod.json lacks information. Mod loading skipped");
                            }
                            break;
                        }
                    }
                }
            }
        }

        GD.Print(foundModPaths.Count + " Mod(s) Found");
    }
    public static void LoadAssets()
    {
        biomes = [];

        LoadBiomes();
    }
    public static Biome GetBiome(string id)
    {
        return biomes[id];
    }
}