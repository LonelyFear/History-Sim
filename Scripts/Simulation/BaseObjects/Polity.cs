using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.States.Diplomacy;

[Union(0, typeof(State))]
[Union(1, typeof(Alliance))]
[MessagePackObject(AllowPrivate = true)]
public abstract partial class Polity : PopObject
{
    // Military
    [Key(17)] public int manpower { get; set; }
    [Key(18)] public int armyPower { get; set; }
    // Political
    [Key(19)] public int occupiedLand { get; set; }
    [Key(21)] public HashSet<ulong> borderingStateIds { get; set; } = [];
    [IgnoreMember] public HashSet<State> borderingStates { get; set; } = [];
    // Economy
    [Key(23)] public float totalWealth { get; set; }
    [Key(-1)] public float baseWealth { get; set; }
    [IgnoreMember] public HashSet<Region> regions = [];
    [Key(24)] public HashSet<ulong> regionIds { get; set; } = [];
    [Key(-2)] public Tech tech;
    
    public override void PrepareForSave()
    {
        base.PrepareForSave();
        regionIds = [.. regions.Select(r => r.id)];
        borderingStateIds = [.. borderingStates.Select(r => r.id)];
    }
    public override void LoadFromSave()
    {
        base.LoadFromSave();
        regions = [.. regionIds.Select(p => ObjectManager.GetRegion(p))];
        borderingStates = [..borderingStateIds.Select(p => ObjectManager.GetState(p))];
    }

    public override void CountPopulation()
    {
        long countedP = 0;
        long countedW = 0;
        Dictionary<string, long> countedProfessions = [];
        Dictionary<Culture, long> cCultures = [];

        HashSet<State> borders = [];
    
        float countedWealth = 0;
        float countedBaseWealth = 0;
        int occRegions = 0;

        foreach (Region region in regions)
        {       
            // Adds up population to state total
            countedP += region.population;
            countedW += region.workforce;
            countedWealth += region.wealth;
            countedBaseWealth += region.baseWealth;

            if (region.border)
            {
                // Gets the states bordering this region
                foreach (Region border in region.borderingRegions)
                {
                    // Makes sure the state is real and not us
                    if (border == null || border.owner == null || border.owner == this)
                    {
                        continue;
                    }

                    // Extends our border with the state
                    State[] statesToEvaluate = [border.owner, border.owner.GetLiege()];
                    foreach (State state in statesToEvaluate)
                    {
                        if (state == null) continue;
                        borders.Add(state);
                    }
                }
            }

            // Counts up socialClasses
            CountClasses(region, countedProfessions);
            CountCultures(region, cCultures);
        }
        
        // Updates values
        occupiedLand = occRegions;
        borderingStates = borders;
        
        totalWealth = countedWealth;
        baseWealth = countedBaseWealth;
        
        foreach (var pair in countedProfessions)
        {
            professions[pair.Key] = pair.Value;
        }
        
        cultures = cCultures;
        largestCulture = cCultures.MaxBy(cc => cc.Value).Key;

        population = countedP;
        workforce = countedW;
        dependents = population - workforce;

        manpower = GetManpower();
        armyPower = GetArmyPower();
    }
    public abstract int GetArmyPower();
    public abstract int GetManpower();
    public virtual int GetSize()
    {
        return regions.Count;
    }
    protected void CountClasses(PopObject obj, Dictionary<string, long> output)
    {
        foreach (string professionId in obj.professions.Keys)
        {
            // Adds objal profession population to state population
            if (!output.TryAdd(professionId, obj.professions[professionId]))
            {
                output[professionId] += obj.professions[professionId];
            }
        }    
    }
    protected void CountCultures(PopObject obj, Dictionary<Culture, long> output)
    {
        // Counts up cultures
        foreach (Culture culture in obj.cultures.Keys)
        {
            // Adds obj culture population to state population
            if (!output.TryAdd(culture, obj.cultures[culture]))
            {
                output[culture] += obj.cultures[culture];
            }
        }        
    }
}