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
    
    [IgnoreMember] public Dictionary<Culture, long> cultures = new Dictionary<Culture, long>();
    [Key(108)] public Dictionary<ulong, long> culturesIds;
    [IgnoreMember] public static Random rng = new Random();
    [IgnoreMember] public Culture largestCulture = null;
    [Key(109)] public ulong largestCultureId;

    public void PreparePopObjectForSave()
    {
        popsIds = pops.Count > 0 ? pops.Select(p => p.id).ToList() : null;
        largestCultureId = largestCulture == null ? 0 : largestCulture.id;
        culturesIds = cultures.Count > 0 ? cultures.ToDictionary(kv => kv.Key.id, kv => kv.Value) : null;
    }
    public void LoadPopObjectFromSave()
    {
        pops = popsIds == null ? new List<Pop>() : popsIds.Select(p => objectManager.GetPop(p)).ToList();
        largestCulture = largestCultureId == 0 ? null : objectManager.GetCulture(largestCultureId);
        cultures = culturesIds == null ? new Dictionary<Culture, long>() : culturesIds.ToDictionary(kv => objectManager.GetCulture(kv.Key), kv => kv.Value);
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
        Culture currentLargest = null;
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

        cultures = countedCultures;
        foreach (Culture culture in cultures.Keys)
        {
            if (currentLargest == null || cultures[culture] > cultures[currentLargest])
            {
                currentLargest = culture;
            }
        }
        largestCulture = currentLargest;

        highestPossiblePopulation = maxPopulation;
        professions = countedSocialClasss;
        population = countedPopulation;
        dependents = countedDependents;
        workforce = countedWorkforce;
    }

    public void AddPop(Pop pop, PopObject popObject)
    {
        if (popObject.GetType() == typeof(Culture))
        {
            // Adding Pop to Culture
            if (!pops.Contains(pop))
            {
                if (pop.culture != null)
                {
                    pop.culture.RemovePop(pop, popObject);
                }
                pops.Add(pop);
                pop.culture = (Culture)popObject;
                population += pop.population;
            }
        }
        else if (popObject.GetType() == typeof(Region))
        {
            if (!pops.Contains(pop))
            {
                if (pop.region != null)
                {
                    pop.region.RemovePop(pop, popObject);
                }
                pops.Add(pop);
                pop.region = (Region)popObject;
                ChangePopulation(pop.workforce, pop.dependents);
            }
        }
        else if (popObject.GetType() == typeof(State))
        {
            if (!pops.Contains(pop))
            {
                pops.Add(pop);
            }
        }
    }
    public void RemovePop(Pop pop, PopObject popObject)
    {
        if (popObject.GetType() == typeof(Culture))
        {
            // Adding Pop to Culture
            pops.Remove(pop);
            ChangePopulation(-pop.workforce, -pop.dependents);
            pop.culture = null;
        }
        else if (popObject.GetType() == typeof(Region))
        {
            pops.Remove(pop);
            ChangePopulation(-pop.workforce, -pop.dependents);
            pop.region = null;
        }
        else if (popObject.GetType() == typeof(State))
        {
            pops.Remove(pop);
        }
    }
    public void ChangePopulation(long workforceChange, long dependentChange)
    {
        workforce += workforceChange;
        dependents += dependentChange;
        population += workforceChange + dependentChange;
    }
    public void TakeLosses(long amount, State state = null, bool includeCivilians = false)
    {
        pops.Shuffle();
        long lossesTaken = amount;
        SocialClass[] combatants = { SocialClass.SOLDIER };
        if (includeCivilians)
        {
            combatants = [SocialClass.SOLDIER, SocialClass.FARMER, SocialClass.MERCHANT];
        }
        foreach (Pop pop in pops)
        {
            if (combatants.Contains(pop.profession))
            {
                if (pop.workforce >= lossesTaken)
                {
                    lossesTaken = 0;
                    pop.ChangeWorkforce(-lossesTaken);
                }
                else
                {
                    lossesTaken -= pop.workforce;
                    pop.ChangeWorkforce(-pop.workforce);
                }
            }
            if (lossesTaken < 1)
            {
                lossesTaken = 0;
                break;
            }
        }

        if (state != null)
        {
            state.manpower -= amount - lossesTaken;
        }
    }
}