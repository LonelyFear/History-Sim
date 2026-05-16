using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;

[Union(0, typeof(Culture))]
[Union(1, typeof(Region))]
[Union(2, typeof(Polity))]
[MessagePackObject(keyAsPropertyName: true)]
public abstract class PopObject : NamedObject
{
    [Key(7)] public long population { get; set; } = 0;
    [Key(9)] public long dependents { get; set; } = 0;
    [Key(10)] public long workforce { get; set; } = 0;
    [Key(11)] public Color color { get; set; }

    [IgnoreMember] public HashSet<Pop> pops = [];
    [Key(12)] public HashSet<ulong> popsIds { get; set; }
    [IgnoreMember] public static TimeManager timeManager;
    [Key(13)] public Dictionary<string, long> professions { get; set; } = new Dictionary<string, long>();
    [Key(14)] public Dictionary<ulong, long> cultureIds { get; set; } = [];
    [Key(15)] public ulong? largestCultureId { get; set; } = null;
    [Key(16)] public Tech averageTech { get; set; }
    [IgnoreMember] public static Random rng = new Random();
    public PopObject()
    {
        foreach (string professionId in AssetManager.professions.Keys)
        {
            professions[professionId] = 0;
        }
    }
    public override void PrepareForSave()
    {
        popsIds = pops.Count > 0 ? [.. pops.Select(p => p.id)] : null;
    }
    public override void LoadFromSave()
    {
        pops = popsIds == null ? [] : [.. popsIds.Select(p => objectManager.GetPop(p))];
    }

    public virtual void GetAverageTech()
    {
        Tech newAvg = new();
        //Culture currentLargest = null;
        foreach (Pop pop in pops)
        {
            newAvg.militaryLevel += pop.tech.militaryLevel;
            newAvg.societyLevel += pop.tech.societyLevel;
            newAvg.industryLevel += pop.tech.industryLevel;
        }
        int popCount = Mathf.Max(pops.Count, 1);
        newAvg.militaryLevel /= popCount;
        newAvg.societyLevel /= popCount;
        newAvg.industryLevel /= popCount;
        
        averageTech = newAvg;
    }
    public virtual void CountPopulation()
    {
        long countedPopulation = 0;
        long countedDependents = 0;
        long countedWorkforce = 0;

        Dictionary<Culture, long> countedCultures = [];
        Dictionary<string, long> countedProfessions = [];

        Tech newAvg = new();
        //Culture currentLargest = null;
        foreach (Pop pop in pops)
        {
            newAvg = new()
            {
                militaryLevel = 20,
                societyLevel = newAvg.societyLevel + pop.tech.societyLevel,
                industryLevel = newAvg.industryLevel + pop.tech.industryLevel              
            };

            countedPopulation += pop.population;
            countedWorkforce += pop.workforce;
            countedDependents += pop.dependents;

            countedProfessions[pop.profession.id] += pop.workforce;
            if (!countedCultures.ContainsKey(pop.culture))
            {
                countedCultures.Add(pop.culture, pop.population);
            }
            else
            {
                countedCultures[pop.culture] += pop.population;
            }
        }
        averageTech = new()
        {
            militaryLevel = newAvg.militaryLevel / pops.Count,
            societyLevel = newAvg.societyLevel / pops.Count,
            industryLevel = newAvg.industryLevel / pops.Count              
        };
        
        foreach (var pair in countedProfessions)
        {
            professions[pair.Key] = pair.Value;
        }
        population = countedPopulation;
        dependents = countedDependents;
        workforce = countedWorkforce;
    }

    public void AddPop(Pop pop, PopObject popObject)
    {
        if (!pops.Contains(pop))
        {
            pops.Add(pop);
            switch (popObject)
            {
                case Culture:
                    if (pop.culture != null)
                    {
                        pop.culture.RemovePop(pop, popObject);
                    }                    
                    pop.culture = (Culture)popObject;
                    break;
                case Region:
                    if (pop.region != null)
                    {
                        pop.region.RemovePop(pop, popObject);
                    }
                    pop.region = (Region)popObject;
                    break;
            }
            ChangePopulation(pop.workforce, pop.dependents, pop.profession.id, pop.culture);
        }
    }
    public void RemovePop(Pop pop, PopObject popObject)
    {
        pops.Remove(pop);
        ChangePopulation(-pop.workforce, -pop.dependents, pop.profession.id, pop.culture);
        switch (popObject)
        {     
            case Culture:
                pop.culture = null;
                break;
            case Region:
                pop.region = null;
                break;    
        } 
    }
    public void ChangePopulation(long workforceChange, long dependentChange, string professionId, Culture culture)
    {
        // Updates numbers
        lock (this)
        {
            workforce += workforceChange;
            dependents += dependentChange;
            population += workforceChange + dependentChange;            
        }
        // Updates Demographics
        lock (professionId)
        {
            if (!professions.ContainsKey(professionId))
            {
                professions.Add(professionId, workforceChange);
            } else
            {
                professions[professionId] += workforceChange;
            }            
        }

        // Cultures
        lock (cultureIds)
        {
            if (!cultureIds.ContainsKey(culture.id))
            {
                cultureIds.Add(culture.id, workforceChange + dependentChange);
            } else
            {
                cultureIds[culture.id] += workforceChange + dependentChange;
            }            
        }

        // Updates largest culture
        if (largestCultureId == null || cultureIds[culture.id] > cultureIds[(ulong)largestCultureId])
        {
            largestCultureId = culture.id;
        }
    }
}