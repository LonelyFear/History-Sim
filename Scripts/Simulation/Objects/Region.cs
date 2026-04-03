using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using MessagePack;
using System.Text.RegularExpressions;
[MessagePackObject(AllowPrivate = true)]
public class Region : PopObject, ISaveable
{
    [Key(203)] public List<Vector2I> tiles { get; set; } = [];
    [IgnoreMember] public bool conquered;
    [Key(202)] public bool habitable { get; set; }
    [Key(3)] public bool coastal { get; set; }
    [Key(300)] public bool isWater { get; set; }
    [Key(4)] public int tradeWeight { get; set; } = 0;
    //[Key(5)] public int baseTradeWeight { get; set; } = 0;
    [Key(8)] public float lastWealth { get; set; } = 0;
    [Key(9)] public float lastBaseWealth { get; set; } = 0;
    [Key(11)] public float baseWealth { get; set; }
    [Key(12)] public float wealth { get; set; }
    [Key(13)] public int linkUpdateCountdown { get; set; } = 12;
    [Key(20)] public Vector2I pos;

    // trade
    [Key(14)] public ulong? marketId { get; set; } = null;
    [Key(15)] public bool isMarketCenter { get; set; } = false;    
    [Key(16)] public float tradeIncome = 0;
    [Key(17)] public float taxIncome = 0;
    [Key(18)] public int zoneSize = 1;

    [Key(19)] public ulong? tradeLinkId { get; set; }
    [Key(360)] public bool tradedUp { get; set; }


    [IgnoreMember] public float navigability { get; set; }
    [IgnoreMember] public float avgTemperature { get; set; }
    [IgnoreMember] public float[] avgMonthlyTemps { get; set; } = new float[12];
    [IgnoreMember] public float avgRainfall { get; set; }
    [IgnoreMember] public float[] avgMonthlyRainfall { get; set; } = new float[12];
    [IgnoreMember] public float avgElevation { get; set; }
    [IgnoreMember] public Dictionary<Biome, int> biomes { get; set; }
    [Key(25)] public int landCount { get; set; }
    [Key(26)] public TerrainType terrainType { get; set; }
    
    [Key(27)] public ulong? occupierId { get; set; }
    
    [Key(28)] public ulong? ownerId { get; set; }

    // Demographics
    [IgnoreMember] public List<Region> borderingRegions { get; set; } = [];
    [IgnoreMember] Dictionary<Region, List<Region>> regionPaths = [];

    // References
    [IgnoreMember] Region _tradeLink;
    [IgnoreMember] public Region tradeLink { 
        get
        {
            if (_tradeLink == null && tradeLinkId != null) 
                _tradeLink = objectManager.GetRegion(tradeLinkId);
            return _tradeLink;
        } 
        set
        {
            tradeLinkId = value?.id;
            _tradeLink = value;
        } 
    }    
    [IgnoreMember] State _owner;
    [IgnoreMember] public State owner { 
        get
        {
            if (_owner == null && ownerId != null) 
                _owner = objectManager.GetState(ownerId);
            return _owner;
        } 
        set
        {
            ownerId = value?.id;
            _owner = value;
        } 
    } 
    [IgnoreMember] State _occupier;
    [IgnoreMember] public State occupier { 
        get
        {
            if (_occupier == null && occupierId != null) 
                _occupier = objectManager.GetState(occupierId);
            return _occupier;
        } 
        set
        {
            occupierId = value?.id;
            _occupier = value;
        } 
    } 

    [Key(35)] public bool border { get; set; }
    [Key(36)] public bool frontier { get; set; }
    [IgnoreMember] public float arableLand { get; set; }

    [Key(37)] public int populationDensity = 1000;
    [Key(38)] public long maxPopulation 
    {
        get
        {
            return (int)((populationDensity + (int)tradeIncome) * arableLand * Mathf.Max(averageTech.societyLevel, 1));
        }
    }

    public void AddTile(Tile tile)
    {
        if (tiles.Contains(tile.pos)) return;

        if (tile.regionId != null)
        {
            objectManager.GetRegion(tile.regionId).RemoveTile(tile);
        }

        tile.regionId = id;
        tiles.Add(tile.pos);
    }
    public void RemoveTile(Tile tile)
    {
        if (!tiles.Contains(tile.pos)) return;

        tiles.Remove(tile.pos);
        tile.regionId = null;
    }
    public void InitRegion()
    {
        CalcAverages();
        CheckHabitability();
        RemoveInvalidBorders();
    }
    public void NameRegion()
    {
        name = NameGenerator.GenerateRegionName(this);
    }
    void RemoveInvalidBorders()
    {
        foreach (Region border in borderingRegions.ToArray())
        {
            borderingRegions.Remove(border);
        }
    }
    void CalcAverages()
    {
        landCount = 0;
        int waterCount = 0;
        
        Dictionary<TerrainType, int> terrainTypes = [];
        biomes = [];
        foreach (Vector2I tilePos in tiles)
        {
            Tile tile = simManager.tiles[tilePos.X, tilePos.Y];
            avgTemperature += tile.GetAverageTemp();
            avgRainfall += tile.GetAnnualRainfall();
            avgElevation += tile.elevation;

            for (int month = 0; month < 12; month++)
            {
                avgMonthlyTemps[month] += tile.GetTempForMonth(month);
                avgMonthlyRainfall[month] +=tile.GetRainfallForMonth(month);
            }

            if (!terrainTypes.TryAdd(tile.terrainType, 1))
            {
                terrainTypes[tile.terrainType]++;
            }

            if (!biomes.ContainsKey(tile.GetBiome()))
            {
                biomes.Add(tile.GetBiome(), 0);
            }
            biomes[tile.GetBiome()]++;

            if (tile.IsWater())
            {
                waterCount++;
            }
            if (tile.IsLand())
            {
                landCount++;
                arableLand += tile.arability;
                navigability += tile.navigability;
            }
            if (tile.coastal)
            {
                coastal = true;
            }            
        }
        navigability /= landCount;
        navigability = Mathf.Clamp(navigability, 0, 1);

        avgTemperature /= tiles.Count;
        avgRainfall /= tiles.Count;
        avgElevation /= tiles.Count;

        for (int month = 0; month < 12; month++)
        {
            avgMonthlyTemps[month] /= tiles.Count;
            avgMonthlyRainfall[month] /= tiles.Count;
        }
    }

    void CheckHabitability()
    {
        if (landCount > 0)
        {
            habitable = true;
            if (!simManager.habitableRegions.Contains(this))
            {
                simManager.habitableRegions.Add(this);
            }
        }
        else
        {
            habitable = false;
        }
    }

    public void GetBorderingRegions()
    {
        borderingRegions = [];
        foreach (Vector2I tilePos in tiles)
        {
            Tile tile = simManager.tiles[tilePos.X, tilePos.Y];
            for (int dx = -1; dx < 2; dx++)
            {
                for (int dy = -1; dy < 2; dy++)
                {
                    if ((dx == 0 && dy == 0) || (dx != 0 && dy != 0)) continue;
                    Vector2I nPos = new Vector2I(Mathf.PosMod(tile.pos.X + dx, SimManager.worldSize.X), Mathf.PosMod(tile.pos.Y + dy, SimManager.worldSize.Y));
                    Tile border = simManager.tiles[nPos.X, nPos.Y];
                    Region borderRegion = objectManager.GetRegion(border.regionId);

                    if (border.regionId != null && border.regionId != id && !borderingRegions.Contains(borderRegion))
                    {
                        borderingRegions.Add(borderRegion);
                    }
                }
            }
        }
    }
    public void UpdateOccupation()
    {
        if (owner == null || occupier == null || !owner.diplomacy.GetOverlord().diplomacy.IsEnemyWithState(occupier))
        {
            occupier = null;
        }
    }
    public void RandomStateFormation()
    {
        if (rng.NextDouble() < 0.0001f * Mathf.Max(averageTech.societyLevel, 1) * navigability && population > 1000)
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
    public State GetController()
    {
        if (occupier != null) return occupier.diplomacy.GetOverlord();
        if (owner != null) return owner.diplomacy.GetOverlord();
        return null;
    }   
    public Polity GetPolity()
    {
        if (occupier != null) return occupier.diplomacy.GetPolity();
        if (owner != null) return owner.diplomacy.GetPolity();
        return null;
    }     

    public void NeutralConquest()
    {
        Region region = PickRandomBorder();
        bool checks = !region.conquered && occupier == null && region != null && region.pops.Count != 0 && region.owner == null;
        if (!checks || owner.regions.Count >= owner.GetMaxRegionsCount()) return;

        long attackerPower;
        attackerPower = owner.GetArmyPower();

        if (Battle.CalcBattle(region, attackerPower, 30000))
        {
            owner.AddRegion(region);
        }
    }

    public void MilitaryConquest()
    {
        Region targetRegion = PickRandomBorder();
        if (targetRegion == null || targetRegion.conquered || targetRegion.GetController() == null || GetController() == null || !GetController().diplomacy.IsEnemyWithState(targetRegion.GetController()))
        {
            return;
        }      

        if (GetController() == null || targetRegion.GetController() == null) return;
        
        long attackerPower = GetPolity().GetArmyPower();
        long defenderPower = targetRegion.GetPolity().GetArmyPower();

        if (Battle.CalcBattle(targetRegion, attackerPower, defenderPower))
        {
            targetRegion.occupier = GetController();
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
        if (population < 1 && owner != null)
        {
            owner.RemoveRegion(this);
        }
    }

    public void CalcBaseWealth()
    {
        lastBaseWealth = baseWealth;
        lastWealth = wealth;

        long farmers = professions[SocialClass.FARMER];
        long nonFarmers = workforce - professions[SocialClass.FARMER];

        float baseProduction = (farmers * 0.04f) + (nonFarmers * 0.02f) + (dependents * 0.01f);
        baseWealth = baseProduction * (arableLand / landCount);
    }
    public void LinkTrade()
    {
        // Default is that we are linked to nobody
        Region selectedLink = null;

        // Default is that we are not a market center
        bool newMarketCenterStatus = true;

        // Loops over borders
        foreach (Region region in borderingRegions)
        {

            // If we have an equal or lower weight to a region then we cant be market leader
            if (region.tradeWeight >= tradeWeight)
            {
                newMarketCenterStatus = false;

                // We can only link to regions with a HIGHER trade weight
                if (region.tradeWeight != tradeWeight)
                {    
                    // If this region has a greater trade weight than our current link, link to it
                    if (selectedLink == null || region.tradeWeight > selectedLink.tradeWeight)
                    {
                        selectedLink = region;
                    }
                }                
            }
        }

        // Joins market of region we linked to if it has a market
        if (selectedLink != null && selectedLink.marketId != marketId)
        {
            Market marketJoined = objectManager.GetMarket(selectedLink.marketId);
            if (marketJoined != null)
            {
                lock (marketJoined)
                {
                    marketJoined.AddRegion(this);
                }                 
            }
 
        }

        isMarketCenter = newMarketCenterStatus;

        // Gets the market we will be working with below
        Market market = objectManager.GetMarket(marketId);

        // If we are a market center and we dont have a market or are in someone elses market
        if (isMarketCenter && (market == null || market.centerId != id))
        {
            // Then create a new market
            market = objectManager.CreateTradeZone(this);
        }

        // Then, on whatever market we just created, if we are no longer a market center
        if (!isMarketCenter && market != null && market.centerId == id)
        {
            // Then delete the market
            objectManager.DeleteTradeZone(market);
        }
        
        // Finally updates our trade link
        tradeLink = selectedLink;
    }
    public void UpdateTradeIncome()
    {
        // If our link isnt to no one, give our trade link some extra income
        if (tradeLink != null)
        {               
            lock (tradeLink)
            {
                tradeLink.tradeIncome += (baseWealth * 0.1f) + tradeIncome;
            }
        }          
    }
    // Trade weight
    // Trade works almost like gravity, more trade weight means that more tiles link to you
    public void GetTradeWeight()
    {
        int baseTradeWeight = GetBaseTradeWeight();

        // Current depth in market expansion
        int depth = 0;

        // The maximum depth we will go before stopping, determines the growth range of markets
        // Region in markets will traverse up the link chain, ether reaching the maximum depth or market center
        // The further the chain goes the less impact the higher trade weights have
        int maxDepth = 5;

        List<float> tradeWeights = [];
        float multiplier = 1.0f;
        Region currentRegion = this;
        do // For each step
        {
            // Increases depth by one
            depth++;
            // Trade weight decay factor
            multiplier -= 0.1f;
            // We get the region that our current region is linked to
            Region nextRegion = currentRegion.tradeLink;

            // If the region is linked
            if (nextRegion != null)
            {
                // Adds the weight to the chain multiplied by multiplier
                // Note that this uses base trade weight so markets dont expand forever
                tradeWeights.Add(nextRegion.GetBaseTradeWeight() * multiplier);
                // Then continues
                currentRegion = nextRegion;
            }
        // This goes on until we have reached the maximum depth or we have reached the trade center
        } while (currentRegion != null && depth < maxDepth);

        
        tradeWeight = baseTradeWeight;
        // If we have anything in the chain
        if (tradeWeights.Count > 0)
        {
            // Our trade weight is set to the highest of the largest value in the chain and our base trade weight
            // This simulates how a market center is going to be shipping goods to the rest of its market
            //GD.Print(tradeWeight + " vs " + baseTradeWeight);
            tradeWeight = (int)Mathf.Max(tradeWeights.Max(), baseTradeWeight);
        }
    }
    public void ZoneTrade()
    {
        if (!isMarketCenter || objectManager.GetMarket(marketId)?.centerId != id)
        {
            return;
        }

        foreach (var pair in simManager.marketIds)
        {
            Market otherMarket = pair.Value;
            Region marketCenter = objectManager.GetRegion(otherMarket.centerId);
            if (marketCenter == null) continue;

            if (!regionPaths.TryGetValue(marketCenter, out List<Region> path))
            {
                path = GetPath(this, marketCenter, true, 20);

                lock (regionPaths)
                {
                    regionPaths[marketCenter] = path;
                }
                lock (marketCenter.regionPaths)
                {
                    marketCenter.regionPaths[this] = path;
                }
                
            }


            if (path.Count < 20)
            {
                foreach (Region tradeRoute in path)
                {
                    lock (tradeRoute)
                    {
                        tradeRoute.tradeIncome = Mathf.Min(tradeIncome, marketCenter.tradeIncome);
                    }
                    
                }
            }
        }
    }
    public int GetBaseTradeWeight()
    {
        
        //long notMerchants = Pop.FromNativePopulation(workforce - professions[SocialClass.MERCHANT]);
        //long merchants = Pop.FromNativePopulation(professions[SocialClass.MERCHANT]);
        float populationTradeWeight = workforce * 0.001f;
        float zoneSizeTradeWeight = 0;
        
        if (isMarketCenter)
        {
            zoneSizeTradeWeight = objectManager.GetMarket(marketId).GetZoneSize();
        }

        float politySizeTradeWeight = 0f;
        if (owner != null && owner.capital == this)
        {
            politySizeTradeWeight = owner.regions.Count;
        }

        return (int)(((navigability * 5f) + populationTradeWeight + politySizeTradeWeight + zoneSizeTradeWeight) * navigability);
    }    
    public void UpdateWealth()
    {
        wealth = baseWealth + taxIncome + tradeIncome;
    }

    public void DistributeWealth()
    {
        foreach (Pop pop in pops)
        {
            pop.wealth = (tradeIncome + taxIncome) * (pop.population / (float)population);
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
            if (pop.population < 1)
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
        if (arableLand / landCount <= 0.2f)
        {
            migrateable = false;
        }

        if (pop != null)
        {
            return (migrateable && habitable) || isWater;
        }
        return migrateable && habitable;
    }
    public Region PickRandomBorder(bool mustBeLiveable = false)
    {
        int index = rng.Next(0, borderingRegions.Count); 
        Region region = borderingRegions[index];
        if (mustBeLiveable)
        {
            while (!region.habitable)
            {
                index = rng.Next(0, borderingRegions.Count);
                region = borderingRegions[index];
            }
        }
        return region;
    }
    public override string GenerateDescription()
    {
        // Region position
        string desc = $"{name} is a region";

        if (population > 0)
        {
            // Region controller
            desc += " under the control of ";
            // Pretty straightforward
            if (GetController() == null)
            {
                desc += " no established factions.";
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
                desc += $" It is the capital of the {GenerateUrlText(owner, owner.name)}";
            }             
        }

        // Looks Like 
        // Name is a region under the control of blank. It has a population of blank
        desc += (population > 0) ?
        // If the region is populated
        $" It has a population of {population:#,###0}."
        // Otherwise
        : " It uninhabited.";
        // The rest is irrelevant if the region is unpopulated
        if (population == 0)
        {
            return desc;
        }

        return desc;        
    }

    public override string GenerateStatsText()
    {
        string text = $"Name: {name}";
        text += $"\nPopulation: {population:#,###0}\n";
        

        if (population > 0)
        {
            text += $"Cultures Breakdown:\n";

            foreach (var cultureSizePair in cultureIds.OrderByDescending(pair => pair.Value))
            {
                Culture culture = objectManager.GetCulture(cultureSizePair.Key);
                long localPopulation = cultureSizePair.Value;
                
                // Skips if the culture is too small
                if (localPopulation < 1) continue;

                text += GenerateUrlText(culture, culture.name) + ":\n";
                text += $"  Population: {localPopulation:#,###0} ";

                float culturePercentage = localPopulation/(float)population;
                text += $"({culturePercentage:P0})\n";
            }     
            text += $"\nWorkforce: {workforce:#,###0}\n";
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
    public static List<Region> GetPath(Region start, Region target, bool mustBeInhabited = false, int maxDist = -1)
    {
        PriorityQueue<Region, float> frontier = new();
        Dictionary<Region, float> costSoFar = [];
        Dictionary<Region, Region> cameFrom = [];
        List<Region> path = [];

        frontier.Enqueue(start, 0);
        costSoFar[start] = 0;
        cameFrom[start] = null;


        while (frontier.Count > 0)
        {
            Region current = frontier.Dequeue();

            if (current == target || (maxDist > 0 && Heuristic(start, target) > maxDist * maxDist))
            {
                break;
            }

            foreach (Region next in current.borderingRegions)
            {
                if (next == null || !next.habitable || (mustBeInhabited && next.population < 1))
                {
                    continue;
                }

                float newCost = costSoFar[current] + (1.1f - next.navigability);
                if (!costSoFar.TryGetValue(next, out float value) || newCost < value)
                {
                    costSoFar[next] = newCost;
                    frontier.Enqueue(next, newCost + Heuristic(target, next));
                    cameFrom[next] = current;
                }
            }
        }

        if (cameFrom.ContainsKey(target))
        {
            Region nextInPath = target;
            while (nextInPath != null)
            {
                path.Add(nextInPath);
                nextInPath = cameFrom[nextInPath];
            }            
        }


        return path;
    }

    static float Heuristic(Region source, Region target)
    {
        return source.pos.DistanceSquaredTo(target.pos);
    }
}   
