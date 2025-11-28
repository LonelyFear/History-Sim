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
    
    [IgnoreMember] public List<Pop> pops = new List<Pop>();
    [Key(106)] public List<ulong> popsIds;
    [IgnoreMember] public static TimeManager timeManager;
    [Key(107)] public Dictionary<SocialClass, long> professions = new Dictionary<SocialClass, long>()
    {
        {SocialClass.FARMER, 0},
        {SocialClass.MERCHANT, 0},
        {SocialClass.ARISTOCRAT, 0},
        {SocialClass.LABOURER, 0},
        {SocialClass.SOLDIER, 0},
    };
    
    [Key(108)] public Dictionary<ulong, long> cultureIds = new Dictionary<ulong, long>();
    [Key(109)] public ulong? largestCultureId = null;
    
    [IgnoreMember] public static Random rng = new Random();
    

    public void PreparePopObjectForSave()
    {
        popsIds = pops.Count > 0 ? pops.Select(p => p.id).ToList() : null;
        //largestCultureId = largestCulture == null ? 0 : largestCulture.id;
        //culturesIds = cultures.Count > 0 ? cultures.ToDictionary(kv => kv.Key.id, kv => kv.Value) : null;
    }
    public void LoadPopObjectFromSave()
    {
        pops = popsIds == null ? new List<Pop>() : popsIds.Select(p => objectManager.GetPop(p)).ToList();
        //largestCulture = largestCultureId == 0 ? null : objectManager.GetCulture(largestCultureId);
        //cultures = culturesIds == null ? new Dictionary<Culture, long>() : culturesIds.ToDictionary(kv => objectManager.GetCulture(kv.Key), kv => kv.Value);
    }
    public virtual void CountPopulation()
    {
        long maxPopulation = 0;
        long countedPopulation = 0;
        long countedDependents = 0;
        long countedWorkforce = 0;

        Dictionary<Culture, long> countedCultures = new Dictionary<Culture, long>();
        Dictionary<SocialClass, long> countedSocialClasss = new Dictionary<SocialClass, long>()
        {
            {SocialClass.FARMER, 0},
            {SocialClass.MERCHANT, 0},
            {SocialClass.LABOURER, 0},
            { SocialClass.SOLDIER, 0},
            { SocialClass.ARISTOCRAT, 0},
        };
        //Culture currentLargest = null;
        foreach (Pop pop in pops)
        {
            maxPopulation += pop.GetMaxPopulation();
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
        /*
        cultures = countedCultures;
        foreach (Culture culture in cultures.Keys)
        {
            if (currentLargest == null || cultures[culture] > cultures[currentLargest])
            {
                currentLargest = culture;
            }
        }
        */
        //largestCulture = currentLargest;

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
        workforce += workforceChange;
        dependents += dependentChange;
        population += workforceChange + dependentChange;

        // Updates Demographics
        if (!professions.ContainsKey(socialClass))
        {
            professions.Add(socialClass, workforceChange);
        } else
        {
            professions[socialClass] += workforceChange;
        }
        
        // Cultures
        if (!cultureIds.ContainsKey(culture.id))
        {
            cultureIds.Add(culture.id, workforceChange + dependentChange);
        } else
        {
            cultureIds[culture.id] += workforceChange + dependentChange;
        }
        // Updates largest culture
        if (largestCultureId == null || cultureIds[culture.id] > cultureIds[(ulong)largestCultureId])
        {
            largestCultureId = culture.id;
        }
    }
}