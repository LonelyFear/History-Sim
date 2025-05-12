using System;
using Godot;
using System.Collections.Generic;

public class Culture : PopObject
{
    public Color color;

    // Increases the chance states of this culture declare wars
    public TraitLevel agression = TraitLevel.MEDIUM;
    
    // Decreases descrimination by units of this culture
    public TraitLevel acceptance = TraitLevel.MEDIUM;

    // Determines if female characters can become leaders
    public TraitLevel equity = TraitLevel.MEDIUM;
    public List<Culture> hatedCultures = new List<Culture>();

    public string name = "Culturism";

    // public void AddPop(Pop pop){
    //     if (!pops.Contains(pop)){
    //         if (pop.culture != null){
    //             pop.culture.RemovePop(pop);
    //         }
    //         pops.Add(pop);
    //         pop.culture = this;
    //         population += pop.population;
    //     }
    // }
    // public void RemovePop(Pop pop){
    //     if (pops.Contains(pop)){
    //         pop.culture = null;
    //         population -= pop.population;            
    //         pops.Remove(pop);
    //     }
    // }
    public static bool CheckCultureSimilarity(Culture a, Culture b){
        return a == b;
    }
}

public enum TraitLevel {
    VERY_HIGH = 2,
    HIGH = 1,
    MEDIUM = 0,
    LOW = -1,
    VERY_LOW = -2,
}
