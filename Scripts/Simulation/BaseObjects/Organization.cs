using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;

[MessagePackObject]
public class Organization : PopObject
{
    // Military
    [Key(9)] public long manpower { get; set; }
    
    // Political
    [Key(10)] public int occupiedLand { get; set; }
    [IgnoreMember] public Dictionary<ulong, int> borderingAllianceIds { get; set; } = [];
    [IgnoreMember] public Dictionary<ulong, int> borderingStateIds { get; set; } = [];
    [IgnoreMember] public Dictionary<ulong, int> independentBorderIds { get; set; } = [];
    // Economy
    [Key(11)] public float totalWealth { get; set; }
    [IgnoreMember] public Dictionary<SocialClass, long> requiredWorkers = [];
    [IgnoreMember] public Dictionary<SocialClass, long> maxJobs = [];
    [Key(700)] public HashSet<ulong> regionIds { get; set; } = [];
    
    public override void CountPopulation()
    {
        long countedP = 0;
        long countedW = 0;

        Dictionary<ulong, int> allianceBorders = new Dictionary<ulong, int>();
        Dictionary<ulong, int> borders = new Dictionary<ulong, int>();
        Dictionary<ulong, int> independentBorders = new Dictionary<ulong, int>();
        Dictionary<SocialClass, long> countedSocialClasses = new Dictionary<SocialClass, long>();
        Dictionary<SocialClass, long> countedRequiredWorkers = new Dictionary<SocialClass, long>();
        Dictionary<SocialClass, long> countedJobs = new Dictionary<SocialClass, long>();
        foreach (SocialClass profession in Enum.GetValues(typeof(SocialClass)))
        {
            countedSocialClasses.Add(profession, 0);
            countedRequiredWorkers.Add(profession, 0);
            countedJobs.Add(profession, 0);
        }

        Dictionary<ulong, long> cCultures = new Dictionary<ulong, long>();
        float countedWealth = 0;
        int occRegions = 0;
        // If realm leader uses realm stats
        foreach (ulong regionId in regionIds)
        {
            Region region = objectManager.GetRegion(regionId);
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
                foreach (ulong? borderId in region.borderingRegionIds)
                {
                    Region border = objectManager.GetRegion(borderId); 
                    State borderState = border.owner;

                    // Makes sure the state is real and not us (If this org is alliance we make sure the state doesnt have membership)
                    if (borderState == null || borderState == this || (this is Alliance && ((Alliance)this).HasMember(borderState)))
                    {
                        continue;
                    }

                    // Extends our border with the state
                    if (!borders.TryAdd(borderState.id, 1))
                    {
                        borders[borderState.id]++;
                    } 

                    if (borderState.sovereignty == Sovereignty.INDEPENDENT)
                    {
                        if (!independentBorders.TryAdd(borderState.id, 1))
                        {
                            independentBorders[borderState.id]++;
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
                        if (!allianceBorders.TryAdd(borderingAlliance.id, 1))
                        {
                            allianceBorders[borderingAlliance.id]++;
                        }                                     
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
        requiredWorkers = countedRequiredWorkers;
        maxJobs = countedJobs;
        cultureIds = cCultures;
        population = countedP;
        workforce = countedW;
    }
}