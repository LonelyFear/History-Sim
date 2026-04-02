using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;

public abstract class PopObject : NamedObject
{
    [Key(102)] public long population { get; set; } = 0;
    [Key(103)] public long highestPossiblePopulation { get; set; } = 0;
    [Key(104)] public long dependents { get; set; } = 0;
    [Key(105)] public long workforce { get; set; } = 0;
    [Key(1070)] public Color color { get; set; }

    [IgnoreMember] public HashSet<Pop> pops = [];
    [Key(106)] public HashSet<ulong> popsIds;
    [IgnoreMember] public static TimeManager timeManager;
    [Key(107)] public Dictionary<SocialClass, long> professions = new Dictionary<SocialClass, long>()
    {
        {SocialClass.FARMER, 0},
        {SocialClass.MERCHANT, 0},
        {SocialClass.ARISTOCRAT, 0},
        {SocialClass.LABOURER, 0},
        {SocialClass.SOLDIER, 0},
    };
    
    [Key(108)] public Dictionary<ulong, long> cultureIds = [];
    [Key(109)] public ulong? largestCultureId = null;
    [Key(1090)] public Tech averageTech;
    [IgnoreMember] public static Random rng = new Random();
    
    public override void PrepareForSave()
    {
        PopObjectSave();
    }
    public override void LoadFromSave()
    {
        PopObjectLoad();
    }

    public void PopObjectSave()
    {
        popsIds = pops.Count > 0 ? [.. pops.Select(p => p.id)] : null;
    }
    public void PopObjectLoad()
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
        long maxPopulation = 0;
        long countedPopulation = 0;
        long countedDependents = 0;
        long countedWorkforce = 0;

        Dictionary<Culture, long> countedCultures = [];
        Dictionary<SocialClass, long> countedSocialClasss = new Dictionary<SocialClass, long>()
        {
            {SocialClass.FARMER, 0},
            {SocialClass.MERCHANT, 0},
            {SocialClass.LABOURER, 0},
            { SocialClass.SOLDIER, 0},
            { SocialClass.ARISTOCRAT, 0},
        };
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

            countedSocialClasss[pop.profession] += pop.workforce;
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
        
        highestPossiblePopulation = maxPopulation;
        professions = countedSocialClasss;
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
            ChangePopulation(pop.workforce, pop.dependents, pop.profession, pop.culture);
        }
    }
    public void RemovePop(Pop pop, PopObject popObject)
    {
        pops.Remove(pop);
        ChangePopulation(-pop.workforce, -pop.dependents, pop.profession, pop.culture);
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
    public void ChangePopulation(long workforceChange, long dependentChange, SocialClass socialClass, Culture culture)
    {
        // Updates numbers
        lock (this)
        {
            workforce += workforceChange;
            dependents += dependentChange;
            population += workforceChange + dependentChange;            
        }
        // Updates Demographics
        lock (professions)
        {
            if (!professions.ContainsKey(socialClass))
            {
                professions.Add(socialClass, workforceChange);
            } else
            {
                professions[socialClass] += workforceChange;
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