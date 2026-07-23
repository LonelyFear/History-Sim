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
        string[] prefixes = FileAccess.Open(@"Data/Names/NationPrefixes.txt", FileAccess.ModeFlags.Read).GetAsArray();
        string[] roots = FileAccess.Open(@"Data/Names/NationRoots.txt", FileAccess.ModeFlags.Read).GetAsArray();
        string[] suffixes = FileAccess.Open(@"Data/Names/NationSuffixes.txt", FileAccess.ModeFlags.Read).GetAsArray();
        
        string InsertVowel(string root){
            if (!vowels.ToCharArray().Contains(name[name.Length - 1]) && !vowels.ToCharArray().Contains(root[0])){
                return vowels.ToCharArray()[rng.Next(0, vowels.Length - 1)] + root;
            }
            return root;
        }
        name += prefixes[rng.Next(0, prefixes.Length - 1)];
        for (int i = 0; i < rng.Next(0, 2); i++){
            if (i == 0){
                name += roots[rng.Next(0, roots.Length - 1)];
            } else {
                name += InsertVowel(roots[rng.Next(0, roots.Length - 1)]);
            }
            
        }
        name += suffixes[rng.Next(0, suffixes.Length - 1)];

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

        string consonants = "bcdfghjklmnpqrstvwxyz";
        if (feminine)
        {
            consonants = "bdfghjklmnprstvwyz";
        }    

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
        if (suffixes != null && suffixes.Length > 0 && (feminine || suffixesOnlyFem))
        {
            name += suffixes[rng.Next(0, suffixes.Length - 1)];
        }

        return name.Capitalize();    
    }
    public static string GenerateRegionName(Region region, Random rng)
    {
        string name = GenerateRandomName(2, 4, rng, ["a", "ia", "al", "ica", "en", "una", "eth", "ar", "or", "inia"]);
        // Location Specific Names
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
    public static void UpdateStateName(State state)
    {
        switch (state.government)
        {
            case GovernmentType.REPUBLIC:
                switch (state.sovereignty)
                {
                    case Sovereignty.COLONY:
                        state.govtName = "Colony";
                        state.leaderTitle = "Governor";
                        break;
                    case Sovereignty.PUPPET:
                        state.govtName = "Mandate";
                        state.leaderTitle = "Governor";
                        break;
                    case Sovereignty.PROVINCE:
                        state.govtName = "Department";
                        state.leaderTitle = "Governor";
                        break;
                    default:
                        state.govtName = "Free State";
                        state.leaderTitle = "Prime Minister";
                        if (state.vassals.Count > 0)
                        {
                            state.govtName = "Republic";
                            state.leaderTitle = "President";
                        }
                        else if (state.vassals.Count > 3)
                        {
                            state.govtName = "Commonwealth";
                            state.leaderTitle = "Chancellor";
                        }
                        break;
                }
                break;
            case GovernmentType.MONARCHY:
                switch (state.sovereignty)
                {
                    case Sovereignty.COLONY:
                        state.govtName = "Crown Colony";
                        state.leaderTitle = "Viceroy";
                        break;
                    case Sovereignty.PUPPET:
                        state.govtName = "Protectorate";
                        state.leaderTitle = "Regent";
                        break;
                    case Sovereignty.PROVINCE:
                        state.govtName = "Duchy";
                        state.leaderTitle = "Duke";
                        break;
                    default:
                        state.govtName = "Principality";
                        state.leaderTitle = "Prince";
                        if (state.vassals.Count > 0)
                        {
                            state.govtName = "Kingdom";
                            state.leaderTitle = "King";
                        }
                        else if (state.vassals.Count > 3)
                        {
                            state.govtName = "Empire";
                            state.leaderTitle = "Emperor";
                        }
                        break;
                }
                break;
            case GovernmentType.AUTOCRACY:
                switch (state.sovereignty)
                {
                    case Sovereignty.COLONY:
                        state.govtName = "Colony";
                        state.leaderTitle = "Governor";
                        break;
                    case Sovereignty.PUPPET:
                        state.govtName = "Puppet";
                        state.leaderTitle = "Administrator";
                        break;
                    case Sovereignty.PROVINCE:
                        state.govtName = "Province";
                        state.leaderTitle = "Governor";
                        break;
                    default:
                        state.govtName = "State";
                        state.leaderTitle = "Despot";
                        if (state.vassals.Count > 0)
                        {
                            state.govtName = "Autocracy";
                            state.leaderTitle = "Archon";
                        }
                        else if (state.vassals.Count > 3)
                        {
                            state.govtName = "Imperium";
                            state.leaderTitle = "Emperor";
                        }
                        break;
                }
                break;
            default:
                state.govtName = "State";
                break;
        }
        state.name = $"{state.govtName} of {state.baseName}";
    }
    static string GenerateSyllable(string pattern, Random rng, string consonants = "bcdfghjklmnpqrstvwxyz")
    {
        string syllable = "";
        foreach (char c in pattern)
        {
            switch (c)
            {
                case 'C':
                    syllable += consonants.ToCharArray()[rng.Next(0, consonants.Length - 1)];
                    break;
                case 'V':
                    syllable += vowels.ToCharArray()[rng.Next(0, vowels.Length - 1)];
                    break;
            }
        }
        return syllable;
    }
}
