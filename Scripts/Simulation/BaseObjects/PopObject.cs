using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;

[Union(0, typeof(Culture))]
[Union(1, typeof(Region))]
[Union(2, typeof(Polity))]
[MessagePackObject]
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
    [Key(14)] protected Dictionary<ulong, long> cultureIds { get; set; } = [];
    [IgnoreMember] public Dictionary<Culture, long> cultures { get; set; } = [];
    [Key(15)] protected ulong? largestCultureId { get; set; } = null;
    [IgnoreMember] Culture _largestCulture;
    [IgnoreMember] public Culture largestCulture
    {
        get
        {
            if (_largestCulture == null && largestCultureId != null) 
                _largestCulture = ObjectManager.GetCulture(largestCultureId);
            return _largestCulture;
        } 
        set
        {
            largestCultureId = value?.id;
            _largestCulture = value;
        }         
    }
    [IgnoreMember] object locker = new();
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
        cultureIds = new Dictionary<ulong, long>(cultures.Select(pair => new KeyValuePair<ulong, long>(pair.Key.id, pair.Value)).ToDictionary());
    }
    public override void LoadFromSave()
    {
        pops = popsIds == null ? [] : [.. popsIds.Select(p => ObjectManager.GetPop(p))];
        cultures = new Dictionary<Culture, long>(cultureIds.Select(pair => new KeyValuePair<Culture, long>(ObjectManager.GetCulture(pair.Key), pair.Value)).ToDictionary());
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
            countedPopulation += pop.population;
            countedWorkforce += pop.workforce;
            countedDependents += pop.dependents;

            if (!countedProfessions.ContainsKey(pop.socialClass.id))
            {
                countedProfessions.Add(pop.socialClass.id, pop.workforce);
            } else
            {
                countedProfessions[pop.socialClass.id] += pop.workforce;
            } 
            if (!countedCultures.ContainsKey(pop.culture))
            {
                countedCultures.Add(pop.culture, pop.population);
            }
            else
            {
                countedCultures[pop.culture] += pop.population;
            }
        }
        
        professions = countedProfessions;
        cultures = countedCultures;
        largestCulture = countedCultures.MaxBy(cc => cc.Value).Key;

        population = countedPopulation;
        dependents = countedDependents;
        workforce = countedWorkforce;
    }

    public virtual void AddPop(Pop pop)
    {   
        lock (locker)
        {
            if (pops.Add(pop))
            {
                switch (this)
                {
                    case Culture:
                        pop.culture?.RemovePop(pop);                    
                        pop.culture = this as Culture;
                        break;
                    case Region:
                        pop.region?.RemovePop(pop);
                        pop.region = this as Region;
                        break;
                }
                ChangePopulation(pop.workforce, pop.dependents, pop.socialClass.id, pop.culture);
            }            
        }

    }
    public virtual void RemovePop(Pop pop)
    {
        lock (locker)
        {
            if (pops.Remove(pop))
            {
                ChangePopulation(-pop.workforce, -pop.dependents, pop.socialClass.id, pop.culture);
                switch (this)
                {     
                    case Culture:
                        pop.culture = null;
                        break;
                    case Region:
                        pop.region = null;
                        break;    
                }             
            }            
        }
    }
    public virtual void ChangePopulation(long workforceChange, long dependentChange, string professionId, Culture culture)
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
        lock (cultures)
        {
            if (!cultures.ContainsKey(culture))
            {
                cultures.Add(culture, workforceChange + dependentChange);
            } else
            {
                cultures[culture] += workforceChange + dependentChange;
            }            
        }

        // Updates largest culture
        if (largestCulture == null || cultures[culture] > cultures[largestCulture])
        {
            largestCulture = culture;
        }
    }
}