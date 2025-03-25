using Godot;
using System;
using System.IO;

public class NameGenerator
{
    public static string GenerateNationName(){
        Random rng = new Random();

        string name = "";
        string[] prefixes = File.ReadAllLines("Resources/Names/NationPrefixes.txt");
        string[] suffixes = File.ReadAllLines("Resources/Names/NationSuffixes.txt");
        string[] syllables = File.ReadAllLines("Resources/Names/NationSyllables.txt");

        name += prefixes[rng.Next(0, prefixes.Length - 1)];
        for (int i = 0; i < rng.Next(0, 3); i++){
            name += syllables[rng.Next(0, syllables.Length - 1)];
        }
        name += suffixes[rng.Next(0, suffixes.Length - 1)];

        return name;
    }
}
