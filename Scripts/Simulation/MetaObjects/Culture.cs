using System;
using Godot;
using System.Collections.Generic;
using MessagePack;
[MessagePackObject(keyAsPropertyName: true)]
public class Culture : PopObject
{
    public Color color { get; set; }

    //public List<Culture> hatedCultures { get; set; } = new List<Culture>();

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
