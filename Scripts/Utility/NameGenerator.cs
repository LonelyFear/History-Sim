using Godot;
using System;
using System.IO;
using System.Linq;

public class NameGenerator
{
    public static string vowels = "aeiou";
    public static string GenerateNationName(){
        Random rng = new Random();

        string name = "";
        string[] prefixes = File.ReadAllLines("Data/Names/NationPrefixes.txt");
        string[] roots = File.ReadAllLines("Data/Names/NationRoots.txt");
        string[] suffixes = File.ReadAllLines("Data/Names/NationSuffixes.txt");
        
        string InsertVowel(string root){
            if (!vowels.ToCharArray().Contains(name[name.Length - 1]) && !vowels.ToCharArray().Contains(root[0])){
                return vowels.ToCharArray()[rng.Next(0, vowels.Length - 1)] + root;
            }
            return root;
        }
        name += prefixes[rng.Next(0, prefixes.Length - 1)];
        for (int i = 0; i < rng.Next(0, 3); i++){
            if (i == 0){
                name += roots[rng.Next(0, roots.Length - 1)];
            } else {
                name += InsertVowel(roots[rng.Next(0, roots.Length - 1)]);
            }
            
        }
        name += suffixes[rng.Next(0, suffixes.Length - 1)];

        return name.Capitalize();
    }

    public static string GenerateCharacterName(bool feminine = false){
        Random rng = new Random();

        //string[] syllables = File.ReadAllLines("Data/Names/NameSyllables.txt");
        string[] patterns = ["CV", "CVC", "VC"];
        string[] feminineSuffixes = ["a", "ia", "ina", "elle", "ara", "essa", "ora", "ina", "ette"];
        string consonants = "bcdfghjklmnpqrstvwxyz";
        if (feminine){
            consonants = "bdfghjklmnprstvwyz";
        }
        string GenerateSyllable(string pattern){
            string syllable = "";
            foreach (char c in pattern){
                switch (c){
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

        string name = "";
        for (int i = 0; i < rng.Next(2, 3); i++){
            name += GenerateSyllable(patterns[rng.Next(0, patterns.Length - 1)]);
        }
        if (feminine){
            name += feminineSuffixes[rng.Next(0, feminineSuffixes.Length - 1)];
        }
        name = name.Capitalize();

        return name;
    }
}
