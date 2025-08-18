using System;
using System.Linq;
using System.Collections.Generic;
using Godot;

public class Region : PopObject
{
    public Tile[,] tiles;
    public Biome[,] biomes;
    public bool habitable;
    public bool coastal;
    private int tradeWeight = 0;
    private int baseTradeWeight = 0;
    public bool hasTradeWeight;
    public bool hasBaseTradeWeight;
    public float lastWealth = 0;
    public float lastBaseWealth = 0;
    public float control = 1f;
    public float baseWealth;
    public float wealth;
    public int linkUpdateCountdown = 4;
    
    public bool habitableAdjacent;

    // trade
    public TradeZone tradeZone;
    public bool isCoT = false;    
    public float tradeIncome = 0;
    public float taxIncome = 0;
    public int zoneSize = 1;
    public Region tradeLink = null;
    public List<Region> connectedTiles = new List<Region>();

    public Vector2I pos;
    public float navigability;
    public float avgTemperature;
    public float avgRainfall;
    public float avgElevation;
    public int landCount;
    public int freeLand = 16;
    public State occupier = null;
    public State owner = null;
    public List<Army> armies;

    // Demographics
    public long maxFarmers = 0;
    public long maxSoldiers = 0;
    public Economy economy = new Economy();
    public Region[] borderingRegions = new Region[4];
    public Region[] habitableBorderingRegions = new Region[4];
    public Dictionary<Region, List<Region>> regionPaths = new Dictionary<Region, List<Region>>();
    public int stability = 100;
    public int unrest = 0;

    public bool border;
    public bool frontier;
    public bool needsJobs { private set; get; }
    public bool needsWorkers { private set; get; }
    public List<Crop> plantableCrops = new List<Crop>();
    public float arableLand;

    public static int populationPerLand = 500;
    public static int farmersPerLand = 115;

    public void CalcAverages()
    {
        name = "Region";
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
    public void CalcProfessionRequirements()
    {
        maxFarmers = (long)(Pop.ToNativePopulation(farmersPerLand) * arableLand);
        maxSoldiers = 0;
        if (owner != null)
        {
            maxSoldiers = (long)(workforce * owner.mobilizationRate);
        }
    }
    #region Nations
    /// <summary>
    /// Function to be used with professions
    /// Disabled to increase the speed of development
    /// </summary>

    public void UpdateOccupation()
    {
        if (occupier != null && !owner.GetHighestLiege().enemies.Contains(occupier))
        {
            occupier = null;
        }
    }
    public void TryFormState()
    {
        if (professions[Profession.ARISTOCRAT] > 0 && rng.NextSingle() < wealth * 0.001f && owner == null)
        {
            SimManager.m.WaitOne();
            simManager.CreateState(this);
            SimManager.m.ReleaseMutex();

            owner.population = population;
            owner.workforce = workforce;
            Pop rulingPop = null;
            foreach (Pop pop in pops)
            {
                if (pop.profession == Profession.ARISTOCRAT)
                {
                    rulingPop = pop;
                    break;
                }
            }

            owner.rulingPop = rulingPop;
            owner.UpdateDisplayName();
        }
    }
    public void RandomStateFormation()
    {
        if (rng.NextSingle() < 0.0001f * navigability && population > Pop.ToNativePopulation(1000))
        {
            SimManager.m.WaitOne();
            simManager.CreateState(this);
            SimManager.m.ReleaseMutex();

            owner.population = population;
            owner.workforce = workforce;
            Pop rulingPop = null;
            foreach (Pop pop in pops)
            {
                rulingPop = pop;
                break;
            }

            owner.rulingPop = rulingPop;
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
    public void NeutralConquest()
    {
        SimManager.m.WaitOne();
        Region region = borderingRegions[rng.Next(0, borderingRegions.Length)];
        SimManager.m.ReleaseMutex();
        if (region == null )
        {
            return;
        }
        bool checks = GetController() == owner && region != null && region.pops.Count != 0 && region.owner == null;
        //float overSizeExpandChance = owner.GetMaxRegionsCount()/(float)owner.regions.Count * 0.01f;

        if (checks && owner.regions.Count() < owner.GetMaxRegionsCount())
        {
            //long defendingCivilians = region.workforce - region.professions[Profession.ARISTOCRAT];
            Battle result = Battle.CalcBattle(region, owner, null, owner.GetArmyPower(), Pop.ToNativePopulation(250000));

            SimManager.m.WaitOne();
            if (result.attackSuccessful)
            {
                owner.AddRegion(region);
            }

            //owner.TakeLosses(result.attackerLosses, owner);
            //region.TakeLosses(result.defenderLosses, null, true);
            SimManager.m.ReleaseMutex();
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
            return owner.GetHighestLiege();
        }
        return null;
    }
    public void MilitaryConquest()
    {
        SimManager.m.WaitOne();
        Region region = borderingRegions[rng.Next(0, borderingRegions.Length)];
        SimManager.m.ReleaseMutex();

        if (region != null && GetController().enemies.Contains(region.GetController()))
        {
            Battle result = Battle.CalcBattle(region, GetController(), null, GetController().GetArmyPower(), region.owner.GetArmyPower());

            SimManager.m.WaitOne();
            if (result.attackSuccessful)
            {
                region.occupier = GetController();
            }
            if (region.occupier == region.owner)
            {
                region.occupier = null;
            }
            SimManager.m.ReleaseMutex();
        }
    }

    public void AddArmy(Army army)
    {
        if (!armies.Contains(army))
        {
            if (army.location != null)
            {
                army.location.RemoveArmy(army);
            }
            armies.Add(army);
            army.location = this;
        }
    }
    public void RemoveArmy(Army army)
    {
        if (armies.Contains(army))
        {
            army.location = null;
            armies.Remove(army);
        }
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
        if (GetController() != owner)
        {
            control = 0f;
        }
        else
        {
            control = Mathf.Clamp(control + 0.05f, 0f, 1f);
        }
    }

    public void CalcBaseWealth()
    {
        lastBaseWealth = baseWealth;
        lastWealth = wealth;
        float techFactor = 1 + (pops[0].tech.scienceLevel * 0.1f);

        long farmers = Pop.FromNativePopulation(professions[Profession.FARMER]);
        long nonFarmers = Pop.FromNativePopulation(workforce - professions[Profession.FARMER]);

        float baseProduction = (farmers * 0.01f) + (nonFarmers * 0.005f) + (Pop.FromNativePopulation(dependents) * 0.002f);
        baseWealth = baseProduction * (arableLand / landCount) * techFactor;
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
            tradeLink.tradeIncome += (baseWealth * 0.1f) + (tradeIncome * 0.1f);      
    }
    public void CalcTradeRoutes()
    {
        if (!simManager.tradeCenters.Contains(this))
        {
            return;
        }
        foreach (Region tradeCenter in simManager.tradeCenters.ToArray())
        {
            List<Region> path = GetPathToRegion(this, tradeCenter, 8);
            if (path == null)
            {
                continue;
            }
            foreach (Region tradeNode in path)
            {
                if (tradeNode.pops.Count < 1)
                {
                    continue;
                }
                tradeNode.tradeIncome = Mathf.Max(tradeNode.tradeIncome, Mathf.Min(tradeCenter.tradeIncome, tradeIncome) * 200);
            }          
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
        
        //long notMerchants = Pop.FromNativePopulation(workforce - professions[Profession.MERCHANT]);
        //long merchants = Pop.FromNativePopulation(professions[Profession.MERCHANT]);
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
                    float newCost = 1 - simManager.GetRegion(next).navigability;
                    if ((!flowCost.ContainsKey(next) || newCost < flowCost[next]) && simManager.GetRegion(next).habitable)
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
                path.Add(simManager.GetRegion(pos));
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

    public float GetFoodSurplus()
    {
        return ((Pop.FromNativePopulation(workforce) * Pop.workforceNutritionNeed) + (Pop.FromNativePopulation(dependents) * Pop.dependentNutritionNeed))/(float)economy.GetTotalNutrition() - 1f;
    }
}
