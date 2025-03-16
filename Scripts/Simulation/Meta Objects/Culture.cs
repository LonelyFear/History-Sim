using System;
using Godot;
using Godot.Collections;

public partial class Culture : GodotObject
{
    public Color color;
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
        float minColorDiff = 0.05f;
        bool similarR = Math.Abs(a.color.R - a.color.B) < minColorDiff;
        bool similarG = Math.Abs(a.color.G - a.color.G) < minColorDiff;
        bool similarB = Math.Abs(a.color.B - a.color.B) < minColorDiff;
        return similarR && similarG && similarB;
    }
}
