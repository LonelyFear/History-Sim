using System;
using Godot;
using Godot.Collections;

public partial class Culture : GodotObject
{
    public Color color;

    // Increases the chance states of this culture declare wars
    public TraitLevel agression = TraitLevel.MEDIUM;
    
    // Decreases descrimination by units of this culture
    public TraitLevel acceptance = TraitLevel.MEDIUM;
    public Array<Culture> hatedCultures = new Array<Culture>();

    public string name = "Culturism";
    public long population = 0;
    public Array<Pop> pops = new Array<Pop>();

    public void ChangePopulation(long amount){
        population += amount;
    }
    public void AddPop(Pop pop){
        if (!pops.Contains(pop)){
            if (pop.culture != null){
                pop.culture.RemovePop(pop);
            }
            pops.Add(pop);
            pop.culture = this;
            population += pop.population;
        }
    }
    public void RemovePop(Pop pop){
        if (pops.Contains(pop)){
            pop.culture = null;
            population -= pop.population;            
            pops.Remove(pop);
        }
    }
    public static bool CheckCultureSimilarity(Culture a, Culture b){
        return a == b;
    }
}

public enum TraitLevel {
    VERY_HIGH,
    HIGH,
    MEDIUM,
    LOW,
    VERY_LOW,
}
