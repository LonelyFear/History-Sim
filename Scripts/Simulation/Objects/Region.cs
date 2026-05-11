using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using MessagePack;
using System.Text.RegularExpressions;
[MessagePackObject(AllowPrivate = true)]
public partial class Region : PopObject, ISaveable
{
    [Key(17)] public List<Vector2I> tiles { get; set; } = [];
    [IgnoreMember] public bool conquered;
    [Key(18)] public bool habitable { get; set; }
    [Key(19)] public bool coastal { get; set; }
    [Key(20)] public bool isWater { get; set; }
    [Key(21)] public int tradeWeight { get; set; } = 0;
    [Key(22)] public Economy economy = new();
    [Key(23)] public Dictionary<ulong, TradeConnection> tradeConnections = new();
    [IgnoreMember] public List<Building> buildings;
    [IgnoreMember] public List<string> buildingIds;

    [Key(26)] public int linkUpdateCountdown { get; set; } = 12;
    [Key(28)] public Vector2I pos;

    // trade
    [Key(29)] ulong? tradeZoneId { get; set; } = null;
    [Key(30)] public bool isTradeZoneCenter { get; set; } = false;  
    [Key(24)] public float baseWealth { get; set; }
    [Key(25)] public float wealth { get; set; }  
    [Key(31)] public float tradeIncome = 0;
    [Key(32)] public float taxIncome = 0;
    [Key(33)] public int zoneSize = 1;

    [Key(34)] public ulong? tradeLinkId { get; set; }
    [Key(35)] public bool tradedUp { get; set; }


    [IgnoreMember] public float navigability { get; set; }
    [IgnoreMember] public float avgTemperature { get; set; }
    [IgnoreMember] public float[] avgMonthlyTemps { get; set; } = new float[12];
    [IgnoreMember] public float avgRainfall { get; set; }
    [IgnoreMember] public float[] avgMonthlyRainfall { get; set; } = new float[12];
    [IgnoreMember] public float avgElevation { get; set; }
    [IgnoreMember] public Dictionary<string, int> biomes { get; set; }
    [Key(36)] public int landCount { get; set; }
    [Key(37)] public TerrainType terrainType { get; set; }
    
    [Key(38)] public ulong? occupierId { get; set; }
    
    [Key(39)] public ulong? ownerId { get; set; }

    // Demographics
    [IgnoreMember] public List<Region> borderingRegions { get; set; } = [];
    [IgnoreMember] Dictionary<Region, List<Region>> regionPaths = [];

    // References
    [Key(45)] public HashSet<ulong> linkedRegionIds = [];
    [IgnoreMember] public HashSet<Region> linkedRegions = [];
    [IgnoreMember] public List<(Region, Region)> tradeRouteLinks = new List<(Region, Region)>();
    [IgnoreMember] TradeZone _tradeZone;
    [IgnoreMember] public TradeZone tradeZone
    {
        get
        {
            if (_tradeZone == null && tradeZoneId != null) 
                _tradeZone = objectManager.GetTradeZone(tradeZoneId);
            return _tradeZone;
        } 
        set
        {
            tradeZoneId = value?.id;
            _tradeZone = value;
        }         
    }
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

    [Key(40)] public bool border { get; set; }
    [Key(41)] public bool frontier { get; set; }
    [IgnoreMember] public float arableLand { get; set; }

    [Key(42)] public const int populationDensity = 500;
    [Key(43)] public long maxPopulation 
    {
        get
        {
            int additionalPopulation = (int)((tradeIncome + taxIncome) * Mathf.Max(averageTech.societyLevel, 1));
            return (int)((populationDensity + additionalPopulation) * arableLand);
        }
    }
    public override void PrepareForSave()
    {
        base.PrepareForSave();
        linkedRegionIds = [..linkedRegions.Select(r => r.id)];
        buildingIds = [..buildings.Select(b => b.id)];
        //economy.PrepareForSave();
    }
    public override void LoadFromSave()
    {
        base.LoadFromSave();
        linkedRegions = [..linkedRegionIds.Select(r => objectManager.GetRegion(r))];
        buildings = [..buildingIds.Select(AssetManager.GetBuilding)];
        //economy.LoadFromSave();
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
            if (!simManager.regionIds.ContainsKey(border.id)){
                RemoveBorder(border);
            }
        }
    }
    public void GetBorderingRegions()
    {
        borderingRegions = [];
        tradeConnections = [];
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
                    AddBorder(borderRegion);
                }
            }
        }
    }
    public void AddBorder(Region region)
    {
        if (region != null && region != this && !borderingRegions.Contains(region))
        {
            borderingRegions.Add(region);
            tradeConnections[region.id] = new();
        }        
    }
    public void RemoveBorder(Region region)
    {
        if (borderingRegions.Remove(region))
        {
            tradeConnections.Remove(region.id);
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

            if (!biomes.ContainsKey(tile.biomeId))
            {
                biomes.Add(tile.biomeId, 0);
            }
            biomes[tile.biomeId]++;

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

        GetTerrainType(terrainTypes);

        navigability /= Mathf.Max(landCount, 1);
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
    void GetTerrainType(Dictionary<TerrainType, int> terrainTypes)
    {
        TerrainType largestType = TerrainType.LAND;
        int largestSize = -1;
        foreach (var pair in terrainTypes)
        {
            TerrainType type = pair.Key;
            int size = pair.Value;

            if (landCount > 0 && (type == TerrainType.DEEP_WATER || type == TerrainType.SHALLOW_WATER || type == TerrainType.ICE))
            {
                continue;
            }
            if (size > largestSize)
            {
                largestType = type;
            }
        }
        terrainType = largestType;        
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
    public void UpdateOccupation()
    {
        if (owner == null || occupier == null || !GetController(false).diplomacy.IsEnemyWithState(occupier))
        {
            occupier = null;
        }
    }
    public void RandomStateFormation()
    {
        if (rng.NextDouble() < 0.00025f * Mathf.Max(averageTech.societyLevel, 1) * navigability && population > 1000)
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
            NameGenerator.UpdateStateName(owner);           
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
    public State GetController(bool includeOccupier = true)
    {
        State controller = owner;
        
        if (occupier != null && includeOccupier) {
            controller = occupier;
        }

        if (controller != null && controller.sovereignty != Sovereignty.REBELLIOUS)
        {
            return controller.diplomacy.GetOverlord();
        }        
        return controller;
    }   

    public void NeutralConquest()
    {
        Region region = PickRandomBorder();
        bool checks = !region.conquered && occupier == null && region != null && region.pops.Count != 0 && region.owner == null;
        if (!checks || owner.regions.Count >= owner.GetMaxRegionsCount()) return;

        long attackerPower;
        attackerPower = owner.GetArmyPower();

        if (Battle.CalcBattle(region, attackerPower, 2000))
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
        
        State attacker = GetController();
        State enemy = targetRegion.GetController();

        War war = attacker.diplomacy.GetWarWithState(enemy);

        long attackerPower = war.GetSideArmyPower(attacker.diplomacy.wars[war]);
        long defenderPower = (long)(war.GetSideArmyPower(enemy.diplomacy.wars[war]) / 0.1f);

        if (Battle.CalcBattle(targetRegion, attackerPower, defenderPower))
        {
            targetRegion.occupier = GetController();
            targetRegion.conquered = true;
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
        baseWealth = population * 0.005f;
    }
    public void LinkTrade()
    {
        // Default is that we are linked to nobody
        Region selectedLink = null;

        // Default is that we are not a tradeZone center
        bool newTradeZoneCenterStatus = true;

        // Loops over borders
        foreach (Region region in borderingRegions)
        {
            // If we have an equal or lower weight to a region then we cant be tradeZone leader
            if (region.tradeWeight >= tradeWeight)
            {
                newTradeZoneCenterStatus = false;

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

        // Joins tradeZone of region we linked to if it has a tradeZone
        if (selectedLink != null && selectedLink.tradeZone != tradeZone)
        {
            TradeZone tradeZoneJoined = selectedLink.tradeZone;
            if (tradeZoneJoined != null)
            {
                lock (tradeZoneJoined)
                {
                    tradeZoneJoined.AddRegion(this);
                }                 
            }
 
        }

        isTradeZoneCenter = newTradeZoneCenterStatus;

        // If we are a tradeZone center and we dont have a tradeZone or are in someone elses tradeZone
        if (isTradeZoneCenter && (tradeZone == null || tradeZone.centerId != id))
        {
            // Then create a new tradeZone
            tradeZone = objectManager.CreateTradeZone(this);
        }

        // Then, on whatever tradeZone we just created, if we are no longer a tradeZone center
        if (!isTradeZoneCenter && tradeZone != null && tradeZone.centerId == id)
        {
            // Then delete the tradeZone
            objectManager.DeleteTradeZone(tradeZone);
            // And erase all trade routes
            EraseTradeRoutes();
        }
        
        // Finally updates our trade link
        SetTradeLink(selectedLink);
    }
    public void SetTradeLink(Region link)
    {
        tradeLink?.linkedRegions.Remove(this);
        tradeLink = link;
        tradeLink?.linkedRegions.Add(this);
    }
    public float GetTradeIncome()
    {
        float newIncome = baseWealth * 0.1f;
        foreach (Region linkedRegion in linkedRegions)
        {
            newIncome += linkedRegion.GetTradeIncome();
        }       
        tradeIncome = newIncome;
        return tradeIncome;
    }
    
    // Trade weight
    // Trade works almost like gravity, more trade weight means that more tiles link to you
    public void GetTradeWeight()
    {
        int baseTradeWeight = GetBaseTradeWeight();

        // Current depth in tradeZone expansion
        int depth = 0;

        // The maximum depth we will go before stopping, determines the growth range of tradeZones
        // Region in tradeZones will traverse up the link chain, ether reaching the maximum depth or tradeZone center
        // The further the chain goes the less impact the higher trade weights have
        int maxDepth = 7;

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
                // Note that this uses base trade weight so tradeZones dont expand forever
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
            // This simulates how a tradeZone center is going to be shipping goods to the rest of its tradeZone
            //GD.Print(tradeWeight + " vs " + baseTradeWeight);
            tradeWeight = (int)Mathf.Max(tradeWeights.Max(), baseTradeWeight);
        }
    }
    public int GetBaseTradeWeight()
    {
        
        //long notMerchants = Pop.FromNativePopulation(workforce - professions[SocialClass.MERCHANT]);
        //long merchants = Pop.FromNativePopulation(professions[SocialClass.MERCHANT]);
        float populationTradeWeight = population * 0.001f;

        float zoneSizeTradeWeight = 0;
        if (isTradeZoneCenter && tradeZone != null)
        {
            zoneSizeTradeWeight = tradeZone.GetZoneSize();
        }

        float politySizeTradeWeight = 0f;
        if (owner != null && owner.capital == this)
        {
            politySizeTradeWeight = owner.regions.Count;
            if (owner.sovereignty == Sovereignty.INDEPENDENT)
            {
                politySizeTradeWeight = owner.diplomacy.GetPolity().regions.Count;
            }
        }

        return (int)((populationTradeWeight + politySizeTradeWeight + zoneSizeTradeWeight) * navigability);
    } 
    public void ZoneTrade()
    {
        if (!isTradeZoneCenter || tradeZone?.centerId != id)
        {
            return;
        }

        foreach (var pair in simManager.tradeZoneIds)
        {
            TradeZone otherTradeZone = pair.Value;
            Region tradeZoneCenter = objectManager.GetRegion(otherTradeZone?.centerId);
            if (tradeZoneCenter == null) continue;

            if (!regionPaths.TryGetValue(tradeZoneCenter, out List<Region> path))
            {
                path = GetPath(this, tradeZoneCenter, true, 20);

                lock (regionPaths)
                {
                    regionPaths[tradeZoneCenter] = path;
                }
                lock (tradeZoneCenter.regionPaths)
                {
                    tradeZoneCenter.regionPaths[this] = path;
                }

                foreach (Region tradeRoute in path)
                {
                    lock (tradeRoute)
                    {
                        tradeRoute.tradeRouteLinks.Add((this, tradeZoneCenter));
                    }
                }                
            }

        }
    }
    public void EraseTradeRoutes()
    {
        foreach (var pathPair in regionPaths)
        {
            foreach (Region tradeRoute in pathPair.Value)
            {
                lock (tradeRoute)
                {
                    tradeRoute.tradeRouteLinks.Add((this, pathPair.Key));
                }
            }             
        }
    }
    public void GetRouteIncome()
    {
        foreach ((Region, Region) tradingCities in tradeRouteLinks.ToArray())
        {
            if (!tradingCities.Item1.isTradeZoneCenter || !tradingCities.Item2.isTradeZoneCenter)
            {
                tradeRouteLinks.Remove(tradingCities);
                continue;
            }
            tradeIncome = Mathf.Max(tradeIncome, Mathf.Min(tradingCities.Item1.tradeIncome, tradingCities.Item2.tradeIncome)* 0.5f);
        }
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
    public bool CanUpdateTrade()
    {
        return linkUpdateCountdown < 0 || pops.Count < 0 || tradeLink == null;
    }
    // Economy V2
    [IgnoreMember] public bool debugProducer = false;
    public void CalcProduction()
    {
        float fertility = arableLand/landCount;
        float productivity = professions[SocialClass.FARMER] * 3f;
        if (debugProducer)
        {
            productivity *= 1;
        }
        economy.production["grain"] = productivity * fertility;
    }
    public void CalcSupply()
    {
        foreach (var pair in economy.production)
        {
            string itemId = pair.Key;
            float production = pair.Value;

            if (tradeZone == null)
            {
                economy.supply[pair.Key] = production;
                continue;
            }

            float marketAccess = GetMarketAccess();
            float localWeight = 1f - marketAccess;
            float availableMarketSupply = tradeZone.economy.supply[itemId] * Mathf.Min(GetMarketWeight() / tradeZone.totalMarketWeight, 1f);

            //if (isTradeZoneCenter) GD.Print(tradeZone.economy.supply[itemId]);

            economy.supply[pair.Key] = (production * localWeight) + (availableMarketSupply * marketAccess);
        }
    }
    public void CalcDemand()
    {
        economy.demand["grain"] = population;
    }
    public float GetMarketAccess()
    {
        if (isTradeZoneCenter)
        {
            return 1f;
        }
        return 0.75f + Mathf.Lerp(-0.5f, 0f, navigability);
    }
    public float GetMarketWeight()
    {
        // More goods if we have good terrain
        float weight = 1f + Mathf.Lerp(-1f, 0f, navigability);

        // More goods if we are on a trade route
        if (tradeRouteLinks.Count > 0)
        {
            weight *= 2f;
        }

        // If we are market center we have most of the goods in store
        if (isTradeZoneCenter)
        {
            weight = 4f;
        }

        return weight * landCount;
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
        : ". It is uninhabited.";
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

            if (current == target || (maxDist > 0 && Heuristic(start, target) > Mathf.Pow(maxDist * 4, 2)))
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
[MessagePackObject]
public struct TradeConnection
{
    public TradeConnection() {}
    [Key(0)] public float capacity = 10000;
    [Key(1)] public Dictionary<string, float> flow = new();
}
