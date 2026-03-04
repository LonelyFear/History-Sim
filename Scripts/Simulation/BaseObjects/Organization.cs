using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;

[MessagePackObject]
public class Organization : PopObject
{
    // Military
    [Key(9)] public long manpower { get; set; } = 0;
    
    // Political
    [Key(10)] public int occupiedLand { get; set; } = 0;
    [IgnoreMember] public Dictionary<ulong, int> borderingAllianceIds { get; set; } = new Dictionary<ulong, int>();
    [IgnoreMember] public Dictionary<ulong, int> borderingStateIds { get; set; } = new Dictionary<ulong, int>();

    // Economy
    [Key(11)] public float totalWealth { get; set; } = 0;
    [IgnoreMember] public Dictionary<SocialClass, long> requiredWorkers = new Dictionary<SocialClass, long>();
    [IgnoreMember] public Dictionary<SocialClass, long> maxJobs = new Dictionary<SocialClass, long>();
    
    public void CountOrgPopulation(Region[] regionsToCheck)
    {
        long countedP = 0;
        long countedW = 0;

        Dictionary<ulong, int> borders = new Dictionary<ulong, int>();
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
        foreach (Region region in regionsToCheck)
        {
            // Adds up population to state total
            countedP += region.population;
            countedW += region.workforce;
            countedWealth += region.wealth;

            if (region.frontier || region.border)
            {
                // Counts up occupied regions
                if (region.occupier != null && regionsToCheck.Contains(region))
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

                    // Gets the alliances this state is in
                    foreach (ulong allianceId in border.owner.allianceIds)
                    {
                        Alliance borderingAlliance = objectManager.GetAlliance(allianceId);

                        // Makes sure the alliance is real and not us (If this org is a state then we make sure we dont have membership)
                        if (borderingAlliance == null || borderingAlliance == this || (this is State && borderingAlliance.HasMember((State)this)))
                        {
                            continue;
                        }           

                        // Extends our border with the alliance
                        if (!borders.TryAdd(borderingAlliance.id, 1))
                        {
                            borders[borderingAlliance.id]++;
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
        totalWealth = countedWealth;
        professions = countedSocialClasses;
        requiredWorkers = countedRequiredWorkers;
        maxJobs = countedJobs;
        cultureIds = cCultures;
        population = countedP;
        workforce = countedW;
    }
}