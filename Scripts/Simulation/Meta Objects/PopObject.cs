using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Godot;

public class PopObject {
    public long population = 0;
    public long dependents = 0;    
    public long workforce = 0;
    public List<Pop> pops = new List<Pop>();
    public Dictionary<Profession, long> professions = new Dictionary<Profession, long>();
    public Dictionary<Culture, long> cultures = new Dictionary<Culture, long>();

    public void AddPop(Pop pop, PopObject popObject){
        if (popObject.GetType() == typeof(Culture)){
            // Adding Pop to Culture
            if (!pops.Contains(pop)){
                if (pop.culture != null){
                    pop.culture.RemovePop(pop);
                }
                pops.Add(pop);
                pop.culture = (Culture)popObject;
                population += pop.population;
            }
        } else if (popObject.GetType() == typeof(Region)){

        } else if (popObject.GetType() == typeof(State)){

        }
    }
}