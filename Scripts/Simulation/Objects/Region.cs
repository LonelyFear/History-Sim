using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using MessagePack;
[MessagePackObject]
public class Region : PopObject
{
    [IgnoreMember] public Tile[,] tiles { get; set; }
    [IgnoreMember] public Biome[,] biomes { get; set; }
    [Key(202)] public bool habitable { get; set; }
    [Key(3)] public bool coastal { get; set; }
    [Key(4)] public int tradeWeight { get; set; } = 0;
    [Key(5)] public int baseTradeWeight { get; set; } = 0;
    [Key(6)] public bool hasTradeWeight;
    [Key(7)] public bool hasBaseTradeWeight;
    [Key(8)] public float lastWealth { get; set; } = 0;
    [Key(9)] public float lastBaseWealth { get; set; } = 0;
    [Key(10)] public float control { get; set; } = 1f;
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
    [Key(26)] public int freeLand { get; set; }
    [IgnoreMember] public State occupier { get; set; } = null;
    [Key(27)] public ulong occupierID { get; set; }
    [IgnoreMember] public State owner { get; set; } = null;
    [Key(28)] public ulong ownerID { get; set; }

    // Demographics
    [Key(29)] public long maxFarmers { get; set; } = 0;
    [Key(30)] public long maxSoldiers { get; set; } = 0;
    [IgnoreMember] public Region[] borderingRegions { get; set; } = new Region[4];
    [Key(32)] public ulong[] borderingRegionsIDs { get; set; }
    [IgnoreMember] public Region[] habitableBorderingRegions { get; set; } = new Region[4];
    [Key(34)] public ulong[] habitableBorderingRegionsIDs { get; set; }

    [Key(35)] public bool border { get; set; }
    [Key(36)] public bool frontier { get; set; }
    [IgnoreMember] public float arableLand { get; set; }

    [IgnoreMember] public static int populationPerLand = 500;
    [IgnoreMember] public static int farmersPerLand = 115;
    #region Data
    public void PrepareForSave()
    {
        PreparePopObjectForSave();
        tradeZoneID = tradeZone != null ? tradeZone.id : 0;
        habitableBorderingRegionsIDs = habitableBorderingRegions.Select(r => r.id).ToArray();
        borderingRegionsIDs = borderingRegions.Select(r => r.id).ToArray();
        ownerID = owner != null ? owner.id : 0;
        occupierID = occupier != null ? occupier.id : 0;
        tradeLinkID = tradeLink != null ? tradeLink.id : 0;
    }
    public void LoadFromSave()
    {
        //GD.Print(id);
        LoadPopObjectFromSave();
        tradeZone = tradeZoneID == 0 ? null : simManager.tradeZonesIds[tradeZoneID];
        habitableBorderingRegions = habitableBorderingRegionsIDs.Select(r => simManager.regionIds[r]).ToArray();
        borderingRegions = borderingRegionsIDs.Select(r => simManager.regionIds[r]).ToArray();
        owner = ownerID == 0 ? null : simManager.statesIds[ownerID];
        occupier = occupierID == 0 ? null : simManager.statesIds[occupierID];
        tradeLink = tradeLinkID == 0 ? null : simManager.regionIds[tradeLinkID];
    }
    #endregion
    #region Init
    public void CalcAverages()
    {
        name = NameGenerator.GenerateRegionName();
        landCount = 0;
        for (int x = 0; x < simManager.tilesPerRegion; x++)
        {
            for (int y = 0; y < simManager.tilesPerRegion; y++)
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
        freeLand = landCount;

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
    public void CalcSocialClassRequirements()
    {
        maxFarmers = (long)(Pop.ToNativePopulation(farmersPerLand) * arableLand);
        maxSoldiers = 0;
        if (owner != null)
        {
            maxSoldiers = (long)(workforce * owner.mobilizationRate);
        }
    }
    #endregion
    #region Nations
    /// <summary>
    /// Function to be used with professions
    /// Disabled to increase the speed of development
    /// </summary>

    public void UpdateOccupation()
    {
        if (owner == null || (occupier != null && !owner.GetHighestLiege().enemyIds.Contains(occupier.id)))
        {
            occupier = null;
        }
    }
    public void TryFormState()
    {
        if (professions[SocialClass.ARISTOCRAT] > 0 && rng.NextSingle() < wealth * 0.001f && owner == null)
        {
            objectManager.CreateState(this);

            owner.population = population;
            owner.workforce = workforce;
            Pop rulingPop = null;
            foreach (Pop pop in pops)
            {
                if (pop.profession == SocialClass.ARISTOCRAT)
                {
                    rulingPop = pop;
                    break;
                }
            }

            owner.rulingPop = rulingPop;
            owner.tech = rulingPop.Tech;
            owner.UpdateDisplayName();
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
            owner.tech = rulingPop.Tech;
            // Sets Leader
            objectManager.CreateCharacter(NameGenerator.GenerateCharacterName(), NameGenerator.GenerateCharacterName(), TimeManager.YearsToTicks(rng.Next(18, 25)), owner, CharacterRole.LEADER);

            owner.UpdateDisplayName();            
        }
    }
    public void StateBordering()
    {
        border = false;
        frontier = false;
        foreach (Region region in borderingRegions)
        {     
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
    public bool DrawBorder(Region r, ref Color color)
    {
        color = new Color(0, 0, 0);
        if (r == null)
        {
            return false;
        }
        bool hasPops = pops.Count > 0;
        bool targetHasPops = r.pops.Count > 0;
        if (hasPops != targetHasPops || (hasPops && r.owner != owner))
        {
            if (r.owner == null || owner == null) {
                return true;
            }
            if (owner.vassals.Contains(r.owner) || owner.liege == r.owner || (owner.liege != null && owner.liege.vassals.Contains(r.owner)))
            {
                color = new Color(0.5f, 0.5f, 0.5f);
            }
            return true;
        }
        return false;
    }
    public State GetController()
    {
        if (occupier != null)
        {
            return occupier;
        }
        if (owner != null)
        {
            return owner.GetHighestLiege();
        }
        return null;
    }    
    #region Conquest
    public void NeutralConquest()
    {
        Region region = borderingRegions[rng.Next(0, borderingRegions.Length)];
        if (region == null)
        {
            return;
        }
        bool checks = GetController() == owner && region != null && region.pops.Count != 0 && region.owner == null;
        //float overSizeExpandChance = owner.GetMaxRegionsCount()/(float)owner.regions.Count * 0.01f;

        if (checks && owner.regions.Count() < owner.GetMaxRegionsCount())
        {
            //long defendingCivilians = region.workforce - region.professions[SocialClass.ARISTOCRAT];
            //double distanceFactor = 1 - Mathf.Min(pos.DistanceTo(owner.capital.pos)/10f, 0.9);
            Battle result = Battle.CalcBattle(region, owner, null, owner.GetArmyPower(false), Pop.ToNativePopulation(200000));

            if (result.attackSuccessful)
            {
                owner.AddRegion(region);
            }
        }
    }

    public void MilitaryConquest()
    {
        Region region = borderingRegions[rng.Next(0, borderingRegions.Length)];

        if (region != null && GetController() != null && region.GetController() != null && GetController().enemyIds.Contains(region.GetController().id))
        {
            Battle result = Battle.CalcBattle(region, GetController(), region.GetController(), GetController().GetArmyPower(), region.GetController().GetArmyPower());

            if (result.attackSuccessful)
            {
                region.occupier = GetController();
            }
            if (region.occupier == region.owner)
            {
                region.occupier = null;
            }
        }
    }
    #endregion
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
    #endregion
    #region Economy & Checks
    public void CheckPopulation()
    {
        CountPopulation();
        if (population < Pop.ToNativePopulation(1) && owner != null)
        {
            owner.RemoveRegion(this);
        }
        if (owner != null && GetController() != owner.GetHighestLiege())
        {
            control = 0f;
        }
        else
        {
            control = Mathf.Clamp(control + 0.005f, 0f, 1f);
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
        foreach (Region region in borderingRegions)
        {
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
        if (isCoT && (tradeZone == null || tradeZone.CoT != this))
        {
            tradeZone = new TradeZone(this);
        }
        if (!isCoT && tradeZone != null && tradeZone.CoT == this)
        {
            tradeZone.DestroyZone();
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

    #endregion
    #region PopActions

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
                    pop.ChangePopulation(merger.workforce, merger.dependents);
                    pop.wealth += merger.wealth;
                    merger.ChangePopulation(-merger.workforce, -merger.dependents);
                    merger.wealth -= merger.wealth;
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

    #endregion
    #region Utility
    public static List<Region> GetPathToRegion(Region start, Region goal, int maxDist)
    {
        List<Region> path = null;
        bool validPath = false;

        PriorityQueue<Vector2I, float> frontier = new PriorityQueue<Vector2I, float>();
        PriorityQueue<int, float> distFrontier = new PriorityQueue<int, float>();
        distFrontier.Enqueue(0, 0);
        frontier.Enqueue(start.pos, 0);
        Dictionary<Vector2I, Vector2I> flow = new Dictionary<Vector2I, Vector2I>();
        flow[start.pos] = new Vector2I(0, 0);
        Dictionary<Vector2I, float> flowCost = new Dictionary<Vector2I, float>();
        flowCost[start.pos] = 0;

        uint attempts = 0;

        while (attempts < 10000 && frontier.Count > 0)
        {
            attempts++;
            int currentDist = distFrontier.Dequeue();
            Vector2I current = frontier.Dequeue();
            if (current == goal.pos)
            {
                validPath = true;
                break;
            }
            if (maxDist != 0 && currentDist > maxDist)
            {
                break;
            }
            for (int dx = -1; dx < 2; dx++)
            {
                for (int dy = -1; dy < 2; dy++)
                {
                    if ((dx != 0 && dy != 0) || (dx == 0 && dy == 0))
                    {
                        continue;
                    }
                    Vector2I next = new Vector2I(Mathf.PosMod(current.X + dx, SimManager.worldSize.X), Mathf.PosMod(current.Y + dy, SimManager.worldSize.Y));
                    float newCost = 1 - objectManager.GetRegion(next).navigability;
                    if ((!flowCost.ContainsKey(next) || newCost < flowCost[next]) && objectManager.GetRegion(next).habitable)
                    {
                        frontier.Enqueue(next, newCost);
                        distFrontier.Enqueue(currentDist + 1, newCost);
                        flowCost[next] = newCost;
                        flow[next] = current;
                    }
                }
            }
        }
        if (validPath)
        {
            path = new List<Region>();
            Vector2I pos = goal.pos;
            while (pos != start.pos)
            {
                path.Add(objectManager.GetRegion(pos));
                pos = flow[pos];
            }
        }
        return path;
    }
    public Region PickRandomBorder()
    {
        Region border;
        border = borderingRegions[rng.Next(0, habitableBorderingRegions.Length)];
        return border;
    }
    #endregion
    #region Named Object
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
                desc += $"{GenerateUrlText(GetController(), GetController().displayName)} as occupied territory. ";
            } else
            {
                desc += $"{GenerateUrlText(owner, owner.displayName)}. ";
            }
        }
        // If it is the capital add to description
        if (owner != null && owner.capital == this)
        {
            desc += $"It is the capital of the {GenerateUrlText(owner, owner.displayName)}";
        }
        return desc;        
    }
    #endregion
}   
