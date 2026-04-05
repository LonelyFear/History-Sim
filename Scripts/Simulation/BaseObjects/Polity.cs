using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;

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
    [Key(20)] public HashSet<ulong> borderingAllianceIds { get; set; } = [];
    [Key(21)] public HashSet<ulong> borderingStateIds { get; set; } = [];
    [Key(22)] public HashSet<ulong> independentBorderIds { get; set; } = [];
    [IgnoreMember] public HashSet<Alliance> borderingAlliances { get; set; } = [];
    [IgnoreMember] public HashSet<State> borderingStates { get; set; } = [];
    [IgnoreMember] public HashSet<State> independentBorders { get; set; } = [];
    // Economy
    [Key(23)] public float totalWealth { get; set; }
    [Key(-1)] public float baseWealth { get; set; }
    [IgnoreMember] public HashSet<Region> regions = [];
    [Key(24)] public HashSet<ulong> regionIds { get; set; } = [];

    public override void PrepareForSave()
    {
        base.PrepareForSave();
        regionIds = [.. regions.Select(r => r.id)];
        borderingStateIds = [.. borderingStates.Select(r => r.id)];
        independentBorderIds = [.. independentBorders.Select(r => r.id)];
        borderingAllianceIds = [.. borderingAlliances.Select(r => r.id)];
    }
    public override void LoadFromSave()
    {
        base.LoadFromSave();
        regions = [.. regionIds.Select(p => objectManager.GetRegion(p))];
        borderingStates = [..borderingStateIds.Select(p => objectManager.GetState(p))];
        independentBorders = [..independentBorderIds.Select(p => objectManager.GetState(p))];
        borderingAlliances = [..borderingAllianceIds.Select(p => objectManager.GetAlliance(p))];
    }

    public override void CountPopulation()
    {
        long countedP = 0;
        long countedW = 0;

        HashSet<Alliance> allianceBorders = [];
        HashSet<State> borders = [];
        HashSet<State> independentBorders = [];
        Dictionary<SocialClass, long> countedSocialClasses = [];

        foreach (SocialClass profession in Enum.GetValues(typeof(SocialClass)))
        {
            countedSocialClasses.Add(profession, 0);
        }

        Dictionary<ulong, long> cCultures = [];
        float countedWealth = 0;
        float countedBaseWealth = 0;
        int occRegions = 0;
        Tech newAvg = new();

        foreach (Region region in regions)
        {
            //Region region = objectManager.GetRegion(regionId);

            if (region == null) {
                regions.Remove(region);
                continue;
            }

            region.GetAverageTech();
            newAvg.militaryLevel += region.averageTech.militaryLevel;
            newAvg.societyLevel += region.averageTech.societyLevel;
            newAvg.industryLevel += region.averageTech.industryLevel;
            
            // Adds up population to state total
            countedP += region.population;
            countedW += region.workforce;
            countedWealth += region.wealth;
            countedBaseWealth += region.baseWealth;

            if (region.frontier || region.border)
            {
                // Counts up occupied regions
                if (region.occupier != null)
                {
                    occRegions++;
                }

                //List<State> checkedBordersForRegion = new List<State>();
                // Gets the states bordering this region
                foreach (Region border in region.borderingRegions)
                {
                    // Makes sure the state is real and not us (If this org is alliance we make sure the state doesnt have membership)
                    if (border == null || border.owner == null || border.owner == this/* || (this is Alliance alliance && alliance.HasMember(borderState))*/)
                    {
                        continue;
                    }

                    // Extends our border with the state
                    State[] statesToEvaluate = [border.owner, border.owner.diplomacy.GetOverlord()];
                    foreach (State state in statesToEvaluate)
                    {
                        if (state == null) continue;
                        
                        borders.Add(state);  
                        if (border.owner.sovereignty == Sovereignty.INDEPENDENT)
                        {
                            independentBorders.Add(state);                   
                        }                          
                    }


                    // Gets the alliances this state is in
                    foreach (ulong allianceId in border.owner.diplomacy.allianceIds)
                    {
                        Alliance borderingAlliance = objectManager.GetAlliance(allianceId);

                        // Makes sure the alliance is real and not us (If this org is a state then we make sure we dont have membership)
                        if (borderingAlliance == null || borderingAlliance == this || (this is State && borderingAlliance.HasMember((State)this)))
                        {
                            continue;
                        }           
                        // Extends our border with the alliance
                        allianceBorders.Add(borderingAlliance);                                    
                    }
                }
            }

            // Counts up professions
            foreach (SocialClass profession in region.professions.Keys)
            {
                countedSocialClasses[profession] += region.professions[profession];
            }

            // Counts up cultures
            foreach (ulong cultureId in region.cultureIds.Keys)
            {
                // Adds regional culture population to state population
                if (!cCultures.TryAdd(cultureId, region.cultureIds[cultureId]))
                {
                    cCultures[cultureId] += region.cultureIds[cultureId];
                }
            }
        }
        
        // Updates values
        occupiedLand = occRegions;
        borderingStates = borders;
        this.independentBorders = independentBorders;
        borderingAlliances = allianceBorders;
        totalWealth = countedWealth;
        baseWealth = countedBaseWealth;
        professions = countedSocialClasses;
        cultureIds = cCultures;
        population = countedP;
        workforce = countedW;

        manpower = GetManpower();
        armyPower = GetArmyPower();

        // Tech
        newAvg.militaryLevel /= Mathf.Max(regions.Count, 1);
        newAvg.societyLevel /= Mathf.Max(regions.Count, 1);
        newAvg.industryLevel /= Mathf.Max(regions.Count, 1);
        averageTech = newAvg;
    }
    public int GetArmyPower()
    {
        return (int)(manpower * (totalWealth/workforce) * (averageTech.militaryLevel + 1));
    }
    public abstract int GetManpower();
}