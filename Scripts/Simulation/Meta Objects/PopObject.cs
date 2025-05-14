using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Godot;

public class PopObject {
    public string name;
    public long population = 0;
    public long dependents = 0;    
    public long workforce = 0;
    public List<Pop> pops = new List<Pop>();
    public Dictionary<Profession, long> professions = new Dictionary<Profession, long>();
    public Dictionary<Culture, long> cultures = new Dictionary<Culture, long>();
    public static Random rng = new Random();

    public void CountPopulation(){
        long countedPopulation = 0;
        long countedDependents = 0;
        long countedWorkforce = 0;

        Dictionary<Culture, long> countedCultures = new Dictionary<Culture, long>();
        Dictionary<Profession, long> countedProfessions = new Dictionary<Profession, long>();
        foreach (Profession profession in Enum.GetValues(typeof(Profession))){
            countedProfessions.Add(profession, 0);
        }

        foreach (Pop pop in pops.ToArray()){
            countedPopulation += pop.population;
            countedWorkforce += pop.workforce;
            countedDependents += pop.dependents;

            countedProfessions[pop.profession] += pop.workforce;
            if (!countedCultures.ContainsKey(pop.culture)){
                countedCultures.Add(pop.culture, pop.population);
            } else {
                countedCultures[pop.culture] += pop.population;
            }
        }
        cultures = countedCultures.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        professions = countedProfessions;
        population = countedPopulation;
        dependents = countedDependents;
        workforce = countedWorkforce;
    }

    public void AddPop(Pop pop, PopObject popObject){
        if (popObject.GetType() == typeof(Culture)){
            // Adding Pop to Culture
            if (!pops.Contains(pop)){
                if (pop.culture != null){
                    pop.culture.RemovePop(pop, popObject);
                }
                pops.Add(pop);
                pop.culture = (Culture)popObject;
                population += pop.population;
            }
        } else if (popObject.GetType() == typeof(Region)){
            if (!pops.Contains(pop)){
                if (pop.region != null){
                    pop.region.RemovePop(pop, popObject);
                }
                pops.Add(pop);
                pop.region = (Region)popObject;            
                ChangePopulation(pop.workforce, pop.dependents);
            }
        } else if (popObject.GetType() == typeof(State)){
            if (!pops.Contains(pop)){
                pops.Add(pop);
            }
        }
    }
    public void RemovePop(Pop pop, PopObject popObject){
        if (popObject.GetType() == typeof(Culture)){
            // Adding Pop to Culture
            if (pops.Contains(pop)){
                pops.Remove(pop);
                pop.culture = null;            
                ChangePopulation(-pop.workforce, -pop.dependents);
            }
        } else if (popObject.GetType() == typeof(Region)){
            if (pops.Contains(pop)){
                pops.Remove(pop);
                pop.region = null;            
                ChangePopulation(-pop.workforce, -pop.dependents);
            }
        } else if (popObject.GetType() == typeof(State)){
            if (pops.Contains(pop)){
                pops.Remove(pop);
            }
        }
    }
    public void ChangePopulation(long workforceChange, long dependentChange){
        workforce += workforceChange;
        dependents += dependentChange;
        population += workforceChange + dependentChange;
    }
    public void TakeLosses(long amount, State state = null){
        pops.Shuffle();
        foreach (Pop pop in pops){
            if (pop.profession != Profession.ARISTOCRAT){
                if (pop.workforce >= amount){
                    amount = 0;
                    pop.ChangeWorkforce(-amount);
                } else {
                    amount -= pop.workforce;
                    pop.ChangeWorkforce(-pop.workforce);
                }
            }    
            if (amount < 1){
                break;
            } 
        }
        if (state != null){
            state.manpower -= amount;
        }
    }
}