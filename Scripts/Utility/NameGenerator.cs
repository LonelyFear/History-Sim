using Godot;
using System;
using System.IO;

public class NameGenerator
{
    public static string GenerateNationName(){
        Random rng = new Random();

        string name = "";
        string[] prefixes = File.ReadAllLines("Data/Names/NationPrefixes.txt");
        string[] suffixes = File.ReadAllLines("Data/Names/NationSuffixes.txt");
        string[] syllables = File.ReadAllLines("Data/Names/NationSyllables.txt");

        name += prefixes[rng.Next(0, prefixes.Length - 1)];
        for (int i = 0; i < rng.Next(0, 3); i++){
            name += syllables[rng.Next(0, syllables.Length - 1)];
        }
        name += suffixes[rng.Next(0, suffixes.Length - 1)];

        return name;
    }

    public static string GenerateFirstName(){
        Random rng = new Random();

        string name = "First";
        return name;
    }
    public static string GenerateLastName(){
        Random rng = new Random();

        string name = "Last";
        return name;   
    }
}
