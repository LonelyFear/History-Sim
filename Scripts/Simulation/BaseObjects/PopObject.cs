using System.Collections.Concurrent;
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
    [Key(13)] public Dictionary<string, long> professions { get; set; } = [];
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
    [IgnoreMember] protected readonly Dictionary<Culture, long> countedCultures = [];
    [IgnoreMember] protected readonly Dictionary<string, long> countedProfessions = [];
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

        countedCultures.Clear();
        countedProfessions.Clear();

        Tech newAvg = new();
        //Culture currentLargest = null;
        foreach (Pop pop in pops)
        {
            countedPopulation += pop.population;
            countedWorkforce += pop.workforce;
            countedDependents += pop.dependents;

            countedProfessions.TryGetValue(pop.socialClass.id, out long classSize);
            countedProfessions[pop.socialClass.id] = classSize + pop.workforce;

            countedCultures.TryGetValue(pop.culture, out long cultureSize);
            countedCultures[pop.culture] = cultureSize + pop.population;
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
        bool popAdded = false;
        lock (locker)
        {
            popAdded = pops.Add(pop);           
        }

        if (popAdded)
        {
            ChangePopulation(pop.workforce, pop.dependents, pop.socialClass.id, pop.culture);
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
        }              
    }
    public virtual void RemovePop(Pop pop)
    {
        bool popRemoved = false;
        lock (locker)
        {
            popRemoved = pops.Remove(pop);           
        }

        if (popRemoved)
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
    public virtual void ChangePopulation(long workforceChange, long dependentChange, string professionId, Culture culture)
    {
        // Updates numbers
        lock (locker)
        {
            workforce += workforceChange;
            dependents += dependentChange;
            population += workforceChange + dependentChange;  
            // Updates Demographics
            professions.TryGetValue(professionId, out long profSize);
            professions[professionId] = profSize + workforceChange;    

            // Cultures
            if (culture == null) return;

            cultures.TryGetValue(culture, out long cultureSize);
            cultures[culture] = cultureSize + workforceChange + dependentChange;            

            // Updates largest culture
            if (largestCulture == null || cultures[culture] > cultures[largestCulture])
            {
                largestCulture = culture;                     
            }
        }
    }
}