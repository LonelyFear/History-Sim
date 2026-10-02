using Godot;
using PixelHistory.Objects.States.Base;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using FileAccess = Godot.FileAccess;

public static class NameGenerator
{
    public static string vowels = "aeiou";
    public static string GenerateNationName(Random rng){
        string name = "";
        //string[] prefixes = FileAccess.Open(@"Data/Names/NationPrefixes.txt", FileAccess.ModeFlags.Read).GetAsArray();
        //string[] roots = FileAccess.Open(@"Data/Names/NationRoots.txt", FileAccess.ModeFlags.Read).GetAsArray();
        string[] suffixes = FileAccess.Open(@"Data/Names/NationSuffixes.txt", FileAccess.ModeFlags.Read).GetAsArray();
        
        /*
        name += prefixes[rng.Next(0, prefixes.Length - 1)];
        for (int i = 0; i < rng.Next(0, 2); i++){
            if (i == 0){
                name += roots[rng.Next(0, roots.Length - 1)];
            } else {
                name += InsertVowel(roots[rng.Next(0, roots.Length - 1)]);
            }
            
        }
        */
        name = GenerateRandomName(2, 4, rng) + suffixes[rng.Next(0, suffixes.Length - 1)];

        return name.Capitalize();
    }

    public static string GetDemonym(string name)
    {
        string demonym = name + "ian";
        name = name.ToLower().Trim();
        if (name.EndsWith("a"))
        {
            demonym = name + "n";
        }
        else if (name.EndsWith("e") || name.EndsWith("y"))
        {
            demonym = name[..^1] + "ian";
        }
        else if (name.EndsWith("land"))
        {
            demonym = name[..^4];
        }
        else if (name.EndsWith("n") || name.EndsWith("r"))
        {
            demonym = name + "i";
        }
        else if (name.Length <= 4)
        {
            demonym = name[..^1] + "ish";
        }
        return demonym.Capitalize();
    }
    public static string GenerateRandomName(int minLength, int maxLength, Random rng, string[] suffixes = null, bool feminine = false, bool suffixesOnlyFem = false)
    {
        string[] patterns = ["CV", "CVC", "VC"];

        string[] consonants = ["b","c","d","f","g","h","j","k","l","m","n","p","q","r","s","t","v","w","x","y","z","ch","sh","th","ph"];   

        string name = "";
        for (int i = 0; i < rng.Next(minLength, maxLength); i++)
        {
            name += GenerateSyllable(patterns[rng.Next(0, patterns.Length - 1)], rng, consonants);
        }
        for (int c = 0; c < name.Length; c++)
        {
            if (c >= name.Length - 1 || name[c] != name[c + 1] ) continue;
            name = name.Remove(c, 1);
        }  
        if (suffixes != null && suffixes.Length > 0 && (!suffixesOnlyFem || feminine == suffixesOnlyFem))
        {
            name += suffixes[rng.Next(0, suffixes.Length - 1)];
        }

        return name.Capitalize();    
    }
    public static string GenerateRegionName(Region region, Random rng)
    {
        string name = GenerateRandomName(2, 4, rng, ["a", "ia", "al", "ica", "en", "una", "eth", "ar", "or", "inia"]);
        // Location Specific Names
        /*
        switch (region.terrainType)
        {
            case TerrainType.LAND:
                break;
            case TerrainType.HILLS:
                name = GetDemonym(name) + Utility.PickRandom([" Hills", " Highlands"]);
                break;
            case TerrainType.MOUNTAINS:
                name = "Mount " + name;
                break;
            case TerrainType.ICE:
                name = GetDemonym(name) + Utility.PickRandom([" Glaciers", " Sheet"]);
                break;
            case TerrainType.SHALLOW_WATER:
                name = GetDemonym(name) + " Waters";
                break;
            case TerrainType.DEEP_WATER:
                name = GetDemonym(name) + " Sea";
                break;
        }
        */
        return name;        
    }
    public static string GenerateCultureName(Random rng)
    {
        return GetDemonym(GenerateRandomName(2, 3, rng, [], rng.Next(2) == 1).Capitalize());
    }
    public static string GenerateCharacterName(Random rng, bool feminine = false)
    {
        return GenerateRandomName(2, 3, rng, ["a", "ia", "ina", "elle", "ara", "essa", "ora", "ina", "ette"], feminine, true);
    }
    public static void UpdateAllianceName(Alliance alliance)
    {
        switch (alliance.type)
        {
            case AllianceType.REALM:
                alliance.name = alliance.leadState.name;
                break;
            case AllianceType.ALLIANCE:
                if (alliance.memberStates.Count == 2)
                {
                    alliance.name = $"Alliance of {alliance.memberStates[0].baseName}-{alliance.memberStates[1].baseName}";
                } else
                {
                    alliance.name = $"{GetDemonym(alliance.leadState.baseName)} League";
                }
                break;
            case AllianceType.UNION:
                if (alliance.memberStates.Count == 2)
                {
                    alliance.name = $"Union of {alliance.memberStates[0].baseName}-{alliance.memberStates[1].baseName}";
                } else
                {
                    alliance.name = $"{GetDemonym(alliance.leadState.baseName)} Federation";
                }                
                break;
        }
    }
    public static string GetStateName(State state)
    {
        string name = state.baseName;
        switch (state.government)
        {
            case GovernmentType.REPUBLIC:
                switch (state.successionType)
                {
                    case SuccessionType.ARISTOCRATIC:
                        name = $"Sovereignty of {state.baseName}";
                        break;
                    case SuccessionType.MERITOCRATIC:
                        name = $"Republic of {state.baseName}";
                        break;
                }
                break;
            case GovernmentType.AUTOCRACY:
                switch (state.successionType)
                {
                    case SuccessionType.ARISTOCRATIC:
                        name = $"Kingdom of {state.baseName}";
                        break;
                    case SuccessionType.MERITOCRATIC:
                        name = $"State of {state.baseName}";
                        break;
                }
                break;
            case GovernmentType.TRIBAL:
                switch (state.successionType)
                {
                    case SuccessionType.ARISTOCRATIC:
                        name = $"{GetDemonym(state.baseName)} Horde";
                        break;
                    case SuccessionType.MERITOCRATIC:
                        name = $"{GetDemonym(state.baseName)} Clan";
                        break;
                }
                break;
        }
        return name;
    }
    static string GenerateSyllable(string pattern, Random rng, string[] consonants)
    {
        string syllable = "";
        foreach (char c in pattern)
        {
            switch (c)
            {
                case 'C':
                    syllable += consonants[rng.Next(0, consonants.Length - 1)];
                    break;
                case 'V':
                    syllable += vowels.ToCharArray()[rng.Next(0, vowels.Length - 1)];
                    break;
            }
        }
        return syllable;
    }
}
