using Godot;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using FileAccess = Godot.FileAccess;

public static class NameGenerator
{
    public static string vowels = "aeiou";
    static Random rng = new Random();
    public static string GenerateNationName(){
        Random rng = new Random();

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
    public static string GenerateRegionName()
    {
        Random rng = new Random();

        //string[] syllables = File.ReadAllLines("Data/Names/NameSyllables.txt");
        string[] patterns = ["CV", "CVC", "VC"];
        string[] suffixes = ["", "a", "ia", "al", "ica", "en", "una", "eth", "ar", "or"];
        string consonants = "bcdfghjklmnpqrstvwxyz";

        string name = "";
        for (int i = 0; i < rng.Next(2, 4); i++)
        {
            name += GenerateSyllable(patterns[rng.Next(0, patterns.Length - 1)], consonants);
        }
        name += suffixes[rng.Next(0, suffixes.Length - 1)];

        for (int c = 0; c < name.Length; c++)
        {
            if (c >= name.Length - 1 || name[c] != name[c + 1] ) continue;
            name = name.Remove(c, 1);
        }
        name = name.Capitalize();

        return name;        
    }
    public static string GenerateCharacterName(bool feminine = false)
    {
        Random rng = new Random();

        //string[] syllables = File.ReadAllLines("Data/Names/NameSyllables.txt");
        string[] patterns = ["CV", "CVC", "VC"];
        string[] feminineSuffixes = ["a", "ia", "ina", "elle", "ara", "essa", "ora", "ina", "ette"];
        string consonants = "bcdfghjklmnpqrstvwxyz";
        if (feminine)
        {
            consonants = "bdfghjklmnprstvwyz";
        }
        string name = "";
        for (int i = 0; i < rng.Next(2, 3); i++)
        {
            name += GenerateSyllable(patterns[rng.Next(0, patterns.Length - 1)], consonants);
        }
        if (feminine)
        {
            name += feminineSuffixes[rng.Next(0, feminineSuffixes.Length - 1)];
        }
        name = name.Capitalize();

        return name;
    }
    static string GenerateSyllable(string pattern, string consonants = "bcdfghjklmnpqrstvwxyz")
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
