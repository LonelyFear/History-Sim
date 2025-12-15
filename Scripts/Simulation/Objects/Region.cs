using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using MessagePack;
using System.Text.RegularExpressions;
[MessagePackObject]
public class Region : PopObject, ISaveable
{
    [IgnoreMember] public Tile[,] tiles { get; set; }
    [IgnoreMember] public Biome[,] biomes { get; set; }
    [IgnoreMember] public bool conquered;
    [Key(202)] public bool habitable { get; set; }
    [Key(3)] public bool coastal { get; set; }
    [Key(4)] public int tradeWeight { get; set; } = 0;
    [Key(5)] public int baseTradeWeight { get; set; } = 0;
    [Key(6)] public bool hasTradeWeight;
    [Key(7)] public bool hasBaseTradeWeight;
    [Key(8)] public float lastWealth { get; set; } = 0;
    [Key(9)] public float lastBaseWealth { get; set; } = 0;
    [Key(11)] public float baseWealth { get; set; }
    [Key(12)] public float wealth { get; set; }
    [Key(13)] public int linkUpdateCountdown { get; set; } = 4;

    // trade
    [IgnoreMember] public TradeZone tradeZone { get; set; }
    [Key(14)] public ulong tradeZoneID { get; set; }
    [Key(15)] public bool isCoT { get; set; } = false;    
    [Key(16)] public float tradeIncome = 0;
    [Key(17)] public float taxIncome = 0;
    [Key(18)] public int zoneSize = 1;
    [IgnoreMember] public Region tradeLink { get; set; } = null;
    [Key(19)] public ulong tradeLinkID { get; set; }

    [Key(20)] public Vector2I pos { get; set; }
    [IgnoreMember] public float navigability { get; set; }
    [IgnoreMember] public float avgTemperature { get; set; }
    [IgnoreMember] public float avgRainfall { get; set; }
    [IgnoreMember] public float avgElevation { get; set; }
    [Key(25)] public int landCount { get; set; }
    [IgnoreMember] public State occupier { get; set; } = null;
    [Key(27)] public ulong occupierID { get; set; }
    [IgnoreMember] public State owner { get; set; } = null;
    [Key(28)] public ulong ownerID { get; set; }

    // Demographics
    [IgnoreMember] public Dictionary<Direction, ulong> borderingRegionIds { get; set; } = new Dictionary<Direction, ulong>();

    // Settlements
    //[Key(31)] public Settlement settlement = new Settlement();
    //[Key(32)] public ulong[] borderingRegionsIDs { get; set; }

    [Key(35)] public bool border { get; set; }
    [Key(36)] public bool frontier { get; set; }
    [IgnoreMember] public float arableLand { get; set; }

    [Key(37)] public int populationDensity = 500;
    [Key(38)] public long maxPopulation = Pop.ToNativePopulation(500) * 16;
    public void UpdateMaxPopulation()
    {
        maxPopulation = Pop.ToNativePopulation(populationDensity) * landCount;
    }
    public void PrepareForSave()
    {
        PreparePopObjectForSave();
        tradeZoneID = tradeZone != null ? tradeZone.id : 0;
        //borderingRegionsIDs = borderingRegions.Select(r => r.id).ToArray();
        ownerID = owner != null ? owner.id : 0;
        occupierID = occupier != null ? occupier.id : 0;
        tradeLinkID = tradeLink != null ? tradeLink.id : 0;
    }
    public void LoadFromSave()
    {
        //GD.Print(id);
        LoadPopObjectFromSave();
        tradeZone = tradeZoneID == 0 ? null : simManager.tradeZonesIds[tradeZoneID];
        //borderingRegions = borderingRegionsIDs.Select(r => simManager.regionIds[r]).ToArray();
        owner = ownerID == 0 ? null : simManager.statesIds[ownerID];
        occupier = occupierID == 0 ? null : simManager.statesIds[occupierID];
        tradeLink = tradeLinkID == 0 ? null : simManager.regionIds[tradeLinkID];
        //settlement.Init();
    }
    public void CalcAverages()
    {
        name = NameGenerator.GenerateRegionName();
        landCount = 0;
        for (int x = 0; x < SimManager.tilesPerRegion; x++)
        {
            for (int y = 0; y < SimManager.tilesPerRegion; y++)
            {
                Tile tile = tiles[x, y];

                avgTemperature += tile.temperature;
                avgRainfall += tile.moisture;
                avgElevation += tile.elevation;

                if (tile.IsLand())
                {
                    landCount++;
                    arableLand += tile.arability;
                    navigability += tile.navigability;

                }
                else if (tile.coastal)
                {
                    coastal = true;
                }
            }
        }

        navigability /= landCount;
        avgTemperature /= tiles.Length;
        avgRainfall /= tiles.Length;
        avgElevation /= tiles.Length;
    }

    public void CheckHabitability()
    {
        if (landCount > 0)
        {
            habitable = true;
        }
        else
        {
            habitable = false;
        }
    }

    public void UpdateOccupation()
    {
        if (owner == null || occupier == null || !owner.diplomacy.enemyIds.Contains(occupier.id))
        {
            occupier = null;
        }
    }
    public void RandomStateFormation()
    {
        if (rng.NextDouble() < 0.0001 * navigability && population > Pop.ToNativePopulation(1000))
        {
            objectManager.CreateState(this);

            owner.population = population;
            owner.workforce = workforce;
            Pop rulingPop = null;
            foreach (Pop pop in pops)
            {
                rulingPop = pop;
                break;
            }
            owner.rulingPop = rulingPop;
            owner.tech = rulingPop.tech;

            // Sets Leader
            objectManager.CreateCharacter(NameGenerator.GenerateCharacterName(), NameGenerator.GenerateCharacterName(), TimeManager.YearsToTicks(rng.Next(18, 25)), owner, CharacterRole.LEADER);
            StateNamer.UpdateStateNames(owner);           
        }
    }
    public void StateBordering()
    {
        border = false;
        frontier = false;
        foreach (ulong regionId in borderingRegionIds.Values)
        {     
            Region region = objectManager.GetRegion(regionId);
            if (region.owner == null)
            {
                frontier = true;
            }
            if (region.owner != null && region.owner != owner)
            {
                border = true;
            }
        }  
    }
    public State GetController()
    {
        if (occupier != null)
        {
            return occupier;
        }
        if (owner != null)
        {
            return owner.vassalManager.GetOverlord(true);
        }
        return null;
    }    

    public void NeutralConquest()
    {
        Region region = PickRandomBorder();
        bool checks = !region.conquered && occupier == null && region != null && region.pops.Count != 0 && region.owner == null;
        //float overSizeExpandChance = owner.GetMaxRegionsCount()/(float)owner.regions.Count * 0.01f;
        if (!checks || owner.regions.Count >= owner.GetMaxRegionsCount()) return;

        long attackerPower;
        attackerPower = owner.GetArmyPower(false);

        bool attackerVictory = Battle.CalcBattle(region, attackerPower, Pop.ToNativePopulation(200000));

        if (attackerVictory)
        {
            owner.AddRegion(region);
        }
    }

    public void MilitaryConquest()
    {
        Region region = PickRandomBorder();
        if (region == null || region.conquered || region.GetController() == null || GetController() == null || !GetController().diplomacy.enemyIds.Contains(region.GetController().id))
        {
            return;
        }                 

        long attackerPower = 0;
        long defenderPower = 0;
        lock (GetController())
        {
            if (GetController() == null)
            {
                return;
            }
            attackerPower = GetController().GetArmyPower(true);
        }
        lock (region.GetController())
        {
            if (region.GetController() == null)
            {
                return;
            }
            defenderPower = region.GetController().GetArmyPower(true);
        }
        
        bool attackerVictory = Battle.CalcBattle(region, attackerPower, defenderPower);
        if (attackerVictory)
        {
            lock (region)
            {
                region.occupier = GetController();
            }
        }         
    }
    
    public double GetStability()
    {
        double totalPoliticalPower = 0;
        double totalHappiness = 0;
        foreach (Pop pop in pops)
        {
            totalPoliticalPower += pop.politicalPower;
            totalHappiness += pop.politicalPower * pop.happiness;
        }
        return totalHappiness / totalPoliticalPower;
    }
    public double GetLoyalty()
    {
        double totalPoliticalPower = 0;
        double totalHappiness = 0;
        foreach (Pop pop in pops)
        {
            totalPoliticalPower += pop.politicalPower;
            totalHappiness += pop.politicalPower * pop.happiness;
        }
        return totalHappiness / totalPoliticalPower;
    }
    public void CheckPopulation()
    {
        CountPopulation();
        if (population < Pop.ToNativePopulation(1) && owner != null)
        {
            owner.RemoveRegion(this);
        }
    }

    public void CalcBaseWealth()
    {
        lastBaseWealth = baseWealth;
        lastWealth = wealth;

        long farmers = Pop.FromNativePopulation(professions[SocialClass.FARMER]);
        long nonFarmers = Pop.FromNativePopulation(workforce - professions[SocialClass.FARMER]);

        float baseProduction = (farmers * 0.04f) + (nonFarmers * 0.02f) + (Pop.FromNativePopulation(dependents) * 0.01f);
        baseWealth = baseProduction * (arableLand / landCount);
    }
    public void LinkTrade()
    {
        Region selectedLink = null;
        bool lowerLinks = true;
        foreach (ulong regionId in borderingRegionIds.Values)
        {
            Region region = objectManager.GetRegion(regionId);
            if (region.GetTradeWeight() >= GetTradeWeight())
                lowerLinks = false;
            if (region.GetTradeWeight() > GetTradeWeight())
            {

                if (selectedLink == null || region.GetTradeWeight() > selectedLink.GetTradeWeight())
                {
                    selectedLink = region;
                }
            }
        }

        if (selectedLink != null && selectedLink.tradeZone != null)
        {
            selectedLink.tradeZone.AddRegion(this);
        }

        isCoT = lowerLinks && selectedLink == null;
        if (isCoT && (tradeZone == null || tradeZone.CoTid != id))
        {
            tradeZone = objectManager.CreateTradeZone(this);
        }
        if (!isCoT && tradeZone != null && tradeZone.CoTid == id)
        {
            objectManager.DeleteTradeZone(tradeZone);
        }

        tradeLink = selectedLink;
        if (tradeLink != null)
        {
            tradeLink.tradeIncome += (baseWealth * 1) + tradeIncome;
        }
    }
    public int GetTradeWeight()
    {
        //return GetBaseTradeWeight();
        if (hasTradeWeight)
        {
            return tradeWeight;
        }
        if (!hasBaseTradeWeight)
        {
            GetBaseTradeWeight();
        }
        int depth = 0;
        int maxDepth = 7;
        List<float> tradeWeights = new List<float>();
        float multiplier = 1.0f;
        Region currentRegion = this;
        do
        {
            depth++;
            multiplier -= 0.1f;
            Region nextRegion = currentRegion.tradeLink;
            if (nextRegion != null)
            {
                tradeWeights.Add(nextRegion.baseTradeWeight * multiplier);
                currentRegion = nextRegion;
            }
            else
            {
                break;
            }
        } while (currentRegion != null && depth < maxDepth);

        if (tradeWeights.Count > 0)
        {
            tradeWeight = (int)Mathf.Max(tradeWeights.Max(), baseTradeWeight);
        }
        else
        {
            tradeWeight = (int)baseTradeWeight;
        }
        hasTradeWeight = true;
        return tradeWeight;
    }
    public int GetBaseTradeWeight()
    {
        
        //long notMerchants = Pop.FromNativePopulation(workforce - professions[SocialClass.MERCHANT]);
        //long merchants = Pop.FromNativePopulation(professions[SocialClass.MERCHANT]);
        float populationTradeWeight = Pop.FromNativePopulation(workforce) * 0.001f;
        float zoneSizeTradeWeight = 0;
        if (isCoT)
        {
            zoneSizeTradeWeight = tradeZone.GetZoneSize();
        }

        float politySizeTradeWeight = 0f;
        if (owner != null && owner.capital == this)
        {
            politySizeTradeWeight = owner.regions.Count * 0.5f;
        }
        // Add trade links
        hasBaseTradeWeight = true;

        baseTradeWeight = (int)(((navigability * 5f) + populationTradeWeight + politySizeTradeWeight + zoneSizeTradeWeight) * navigability);
        return baseTradeWeight;
    }    
    public void UpdateWealth()
    {
        wealth = baseWealth + taxIncome + tradeIncome;
    }

    public void DistributeWealth()
    {
        foreach (Pop pop in pops)
        {
            pop.wealth = (tradeIncome + taxIncome) * (pop.ownedLand / (float)landCount);
        }
    }

    public void MergePops()
    {
        if (pops.Count < 2)
        {
            return;
        }

        Queue<Pop> popsToCheck = new Queue<Pop>(pops);
        while (popsToCheck.Count > 0)
        {
            Pop pop = popsToCheck.Dequeue();
            if (pop.population < Pop.ToNativePopulation(1))
            {
                continue;
            }
            foreach (Pop merger in popsToCheck)
            {
                if (Pop.CanPopsMerge(pop, merger))
                {
                    lock (pop)
                    {
                        pop.ChangePopulation(merger.workforce, merger.dependents);
                        pop.wealth += merger.wealth;                        
                    }
                    lock (merger)
                    {
                        merger.ChangePopulation(-merger.workforce, -merger.dependents);
                        merger.wealth -= merger.wealth;                        
                    }
                    break;
                }
            }
        }
    }
    public bool Migrateable(Pop pop = null)
    {
        bool migrateable = true;
        if (arableLand / landCount < 0.2f)
        {
            migrateable = false;
        }
        return migrateable && habitable;
    }
    public Region PickRandomBorder(bool mustBeLiveable = false)
    {
        Region region = objectManager.GetRegion(borderingRegionIds.Values.ToArray()[rng.Next(0, borderingRegionIds.Count)]);
        if (mustBeLiveable)
        {
            while (!region.habitable)
            {
                region = objectManager.GetRegion(borderingRegionIds.Values.ToArray()[rng.Next(0, borderingRegionIds.Count)]);
            }
        }
        return region;
    }
    public override string GenerateDescription()
    {
        // Region position
        string desc = $"{name} is a region located at {pos.X}, {pos.Y}. The region ";
        desc += (Pop.FromNativePopulation(population) > 0) ?
        // If the region is populated
        $"has a population of {Pop.FromNativePopulation(population):#,###0}. "
        // Otherwise
        : "is uninhabited. ";
        // The rest is irrelevant if the region is unpopulated
        if (Pop.FromNativePopulation(population) == 0)
        {
            return desc;
        }
        // Region controller
        desc += "It is under the control of ";
        // Pretty straightforward
        if (GetController() == null)
        {
            desc += "no established factions.";
        } else
        {
            if (GetController() != owner)
            {
                desc += $"{GenerateUrlText(GetController(), GetController().name)} as occupied territory. ";
            } else
            {
                desc += $"{GenerateUrlText(owner, owner.name)}. ";
            }
        }
        // If it is the capital add to description
        if (owner != null && owner.capital == this)
        {
            desc += $"It is the capital of the {GenerateUrlText(owner, owner.name)}";
        }
        return desc;        
    }

    public override string GenerateStatsText()
    {
        string text = $"Name: {name}";
        text += $"\nPopulation: {Pop.FromNativePopulation(population):#,###0}\n";
        

        if (Pop.FromNativePopulation(population) > 0)
        {
            text += $"Cultures Breakdown:\n";

            foreach (var cultureSizePair in cultureIds.OrderByDescending(pair => pair.Value))
            {
                Culture culture = objectManager.GetCulture(cultureSizePair.Key);
                long localPopulation = cultureSizePair.Value;
                
                // Skips if the culture is too small
                if (Pop.FromNativePopulation(localPopulation) < 1) continue;

                text += GenerateUrlText(culture, culture.name) + ":\n";
                text += $"  Population: {Pop.FromNativePopulation(localPopulation):#,###0} ";

                float culturePercentage = localPopulation/(float)population;
                text += $"({culturePercentage:P0})\n";
            }     
            text += $"\nWorkforce: {Pop.FromNativePopulation(workforce):#,###0}\n";
            /*
            text += $"Professions Breakdown:\n";     

            foreach (var professionSizePair in professions.OrderByDescending(pair => pair.Key))
            {
                SocialClass socialClass = professionSizePair.Key;
                long localPopulation = professionSizePair.Value;
                
                // Skips if the culture is too small
                if (Pop.FromNativePopulation(localPopulation) < 1) continue;
                text += $"{socialClass.ToString().Capitalize()}\n";

                text += $"  Workers: {Pop.FromNativePopulation(localPopulation):#,###0} ";

                float percentage = localPopulation/(float)workforce;
                text += $"({percentage:P0})\n";
            } 
            */  
        }
        return text;
    }
}   
