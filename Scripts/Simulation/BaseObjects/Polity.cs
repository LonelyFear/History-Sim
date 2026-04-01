using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;

public abstract class Polity : PopObject
{
    // Military
    [Key(710)] public int manpower;
    [Key(720)] public int armyPower;
    // Political
    [Key(10)] public int occupiedLand { get; set; }
    [IgnoreMember] public HashSet<ulong> borderingAllianceIds { get; set; } = [];
    [IgnoreMember] public HashSet<ulong> borderingStateIds { get; set; } = [];
    [IgnoreMember] public HashSet<ulong> independentBorderIds { get; set; } = [];
    // Economy
    [Key(11)] public float totalWealth { get; set; }
    [Key(700)] public HashSet<ulong> regionIds { get; set; } = [];
    
    public override void CountPopulation()
    {
        long countedP = 0;
        long countedW = 0;

        HashSet<ulong> allianceBorders = [];
        HashSet<ulong> borders = [];
        HashSet<ulong> independentBorders = [];
        Dictionary<SocialClass, long> countedSocialClasses = [];

        foreach (SocialClass profession in Enum.GetValues(typeof(SocialClass)))
        {
            countedSocialClasses.Add(profession, 0);
        }

        Dictionary<ulong, long> cCultures = [];
        float countedWealth = 0;
        int occRegions = 0;
        Tech newAvg = new();

        foreach (ulong regionId in regionIds.ToArray())
        {
            Region region = objectManager.GetRegion(regionId);

            if (region == null) {
                regionIds.Remove(regionId);
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

            if (region.frontier || region.border)
            {
                // Counts up occupied regions
                if (region.occupier != null)
                {
                    occRegions++;
                }

                //List<State> checkedBordersForRegion = new List<State>();
                // Gets the states bordering this region
                foreach (ulong borderId in region.borderingRegionIds)
                {
                    Region border = objectManager.GetRegion(borderId); 

                    // Makes sure the state is real and not us (If this org is alliance we make sure the state doesnt have membership)
                    if (border.owner == null || border.owner == this/* || (this is Alliance alliance && alliance.HasMember(borderState))*/)
                    {
                        continue;
                    }

                    // Extends our border with the state
                    State[] statesToEvaluate = [border.owner, border.owner.diplomacy.GetOverlord()];
                    foreach (State state in statesToEvaluate)
                    {
                        borders.Add(state.id);  
                        if (border.owner.sovereignty == Sovereignty.INDEPENDENT)
                        {
                            independentBorders.Add(state.id);                   
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
                        allianceBorders.Add(borderingAlliance.id);                                    
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
        borderingStateIds = borders;
        independentBorderIds = independentBorders;
        borderingAllianceIds = allianceBorders;
        totalWealth = countedWealth;
        professions = countedSocialClasses;
        cultureIds = cCultures;
        population = countedP;
        workforce = countedW;

        manpower = GetManpower();
        armyPower = GetArmyPower();

        // Tech
        newAvg.militaryLevel /= Mathf.Max(regionIds.Count, 1);
        newAvg.societyLevel /= Mathf.Max(regionIds.Count, 1);
        newAvg.industryLevel /= Mathf.Max(regionIds.Count, 1);
        averageTech = newAvg;
    }
    public int GetArmyPower()
    {
        return (int)(GetManpower() * (totalWealth/workforce) * (averageTech.militaryLevel + 1));
    }
    public abstract int GetManpower();
}