using System;
using System.Collections.Generic;

public abstract class PopObject {
    public string name;
    public uint foundTick;
    public uint age;
    public float averageWealth;
    public long population = 0;
    public long dependents = 0;    
    public long workforce = 0;
    public List<Pop> pops = new List<Pop>();
    public Dictionary<Profession, long> professions = new Dictionary<Profession, long>()
    {
        {Profession.FARMER, 0},
        { Profession.MERCHANT, 0},
        {Profession.ARISTOCRAT, 0},
    };
    
    public Dictionary<Culture, long> cultures = new Dictionary<Culture, long>();
    public static Random rng = new Random();
    public static SimManager simManager;
    public Culture largestCulture = null;

    public void CountPopulation()
    {
        long countedPopulation = 0;
        long countedDependents = 0;
        long countedWorkforce = 0;
        averageWealth = 0f;

        Dictionary<Culture, long> countedCultures = new Dictionary<Culture, long>();
        Dictionary<Profession, long> countedProfessions = new Dictionary<Profession, long>()
        {
            {Profession.FARMER, 0},
            { Profession.MERCHANT, 0},
            {Profession.ARISTOCRAT, 0},
        };
        Culture currentLargest = null;
        foreach (Pop pop in pops)
        {
            countedPopulation += pop.population;
            countedWorkforce += pop.workforce;
            countedDependents += pop.dependents;
            averageWealth += pop.wealth;

            countedProfessions[pop.profession] += pop.workforce;
            if (!countedCultures.ContainsKey(pop.culture))
            {
                countedCultures.Add(pop.culture, pop.population);
            }
            else
            {
                countedCultures[pop.culture] += pop.population;
            }
        }
        averageWealth /= pops.Count;
        if (float.IsNaN(averageWealth))
        {
            averageWealth = 0f;
        }
        
        cultures = countedCultures;
        foreach (Culture culture in cultures.Keys)
        {
            if (currentLargest == null || cultures[culture] > cultures[currentLargest])
            {
                currentLargest = culture;
            }
        }
        largestCulture = currentLargest;


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
            pops.Remove(pop);
            ChangePopulation(-pop.workforce, -pop.dependents);
            pop.culture = null;
        } else if (popObject.GetType() == typeof(Region)){
            pops.Remove(pop);
            ChangePopulation(-pop.workforce, -pop.dependents);                
            pop.region = null;            
        } else if (popObject.GetType() == typeof(State)){
            pops.Remove(pop);
        }
    }
    public void ChangePopulation(long workforceChange, long dependentChange){
        workforce += workforceChange;
        dependents += dependentChange;
        population += workforceChange + dependentChange;
    }
    public void TakeLosses(long amount, State state = null){
        pops.Shuffle();
        long lossesTaken = amount;
        foreach (Pop pop in pops){
            if (pop.profession != Profession.ARISTOCRAT){
                if (pop.workforce >= lossesTaken){
                    lossesTaken = 0;
                    pop.ChangeWorkforce(-lossesTaken);
                } else {
                    lossesTaken -= pop.workforce;
                    pop.ChangeWorkforce(-pop.workforce);
                }
            }    
            if (lossesTaken < 1){
                lossesTaken = 0;
                break;
            } 
        }

        if (state != null){
            state.manpower -= amount - lossesTaken;
        }
    }


    public enum ObjectType{
        STATE,
        REGION,
        CULTURE,
        IDK
    }
}