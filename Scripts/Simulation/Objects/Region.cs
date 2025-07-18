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
    public float tradeWeight;
    public float baseWealth;
    public float wealth;

    public Vector2I pos;
    public float navigability;
    public float avgTemperature;
    public float avgRainfall;
    public float avgElevation;
    public int landCount;
    public int freeLand;    
    public State owner = null;
    public List<Army> armies;

    // Demographics
    public long maxPopulation = 0;
    public Economy economy = new Economy();
    public List<Region> borderingRegions = new List<Region>();

    public int currentMonth;
    public bool border;
    public bool frontier;
    public bool needsJobs { private set; get; }
    public bool needsWorkers { private set; get; }
    public List<Crop> plantableCrops = new List<Crop>();
    public float arableLand;

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
                else if (tile.terrainType == TerrainType.WATER)
                {
                    coastal = true;
                }
            }
        }
        if (landCount > 0)
        {
            //GD.Print(arableLand);            
        }
        freeLand = landCount;

        navigability /= landCount;
        avgTemperature /= tiles.Length;
        avgRainfall /= tiles.Length;
        avgElevation /= tiles.Length;
        //economy.ChangeResourceAmount(simManager.GetResource("grain"), 100);

        foreach (Crop crop in AssetManager.crops.Values)
        {
            bool goodTemp = crop.maxTemperature >= avgTemperature && crop.minTemperature <= avgTemperature;
            bool goodRain = crop.maxRainfall >= avgRainfall && crop.minRainfall <= avgRainfall;
            if (goodTemp && goodRain)
            {
                plantableCrops.Add(crop);
            }
        }
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
    public void CalcMaxPopulation()
    {
        for (int x = 0; x < simManager.tilesPerRegion; x++)
        {
            for (int y = 0; y < simManager.tilesPerRegion; y++)
            {
                Biome biome = biomes[x, y];
                Tile tile = tiles[x, y];

                tile.maxPopulation = 0;
                if (tile.IsLand())
                {
                    maxPopulation += (long)(Pop.ToNativePopulation(1000) * tile.navigability);
                }
            }
        }
    }
    #region Nations
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
                if (!owner.borderingStates.Contains(region.owner))
                {
                    owner.borderingStates.Append(region.owner);
                }
            }
        }
    }

    public void NeutralConquest()
    {
        SimManager.m.WaitOne();
        Region region = borderingRegions[rng.Next(0, borderingRegions.Count)];
        SimManager.m.ReleaseMutex();
        if (region != null && region.pops.Count != 0 && region.owner == null && rng.NextSingle() < 0.005f)
        {
            Battle result = Battle.CalcBattle(region, owner, null, owner.GetArmyPower() / 2, (long)(region.workforce * 0.95f));

            SimManager.m.WaitOne();
            if (result.victor == Conflict.Side.AGRESSOR)
            {
                owner.AddRegion(region);
            }

            owner.TakeLosses(result.attackerLosses, owner);
            region.TakeLosses(result.defenderLosses);
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
    }

    public void UpdateWealth()
    {
        float techFactor = 1 + (pops[0].tech.scienceLevel * 0.1f);
        float farmerProduction = ((Pop.FromNativePopulation(professions[Profession.FARMER]) * 0.01f) + (Pop.FromNativePopulation(dependents) * 0.0033f)) * (arableLand / landCount) * techFactor;
        wealth = farmerProduction;
    }

    public void DistributeWealth()
    {
        
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

    public void MovePop(Pop pop, Region destination, long movedWorkforce, long movedDependents, float movedWealth = 0f)
    {
        if (destination == null || destination == this)
        {
            return;
        }
        if (movedWorkforce >= Pop.ToNativePopulation(1) || movedDependents >= Pop.ToNativePopulation(1))
        {
            if (movedWorkforce > pop.workforce)
            {
                movedWorkforce = pop.workforce;
            }
            if (movedDependents > pop.dependents)
            {
                movedDependents = pop.dependents;
            }
            SimManager.m.WaitOne();
            Pop npop = simManager.CreatePop(movedWorkforce, movedDependents, destination, pop.tech, pop.culture, pop.profession, movedWealth);
            npop.canMove = false;
            pop.ChangePopulation(-movedWorkforce, -movedDependents);
            pop.wealth -= movedWealth;
            SimManager.m.ReleaseMutex();
        }
    }

    #endregion
    public static bool GetPathToRegion(Region start, Region goal, out Queue<Region> path)
    {
        path = null;
        bool validPath = false;

        PriorityQueue<Vector2I, float> frontier = new PriorityQueue<Vector2I, float>();
        frontier.Enqueue(start.pos, 0);
        Dictionary<Vector2I, Vector2I> flow = new Dictionary<Vector2I, Vector2I>();
        flow[start.pos] = new Vector2I(0, 0);
        Dictionary<Vector2I, float> flowCost = new Dictionary<Vector2I, float>();
        flowCost[start.pos] = 0;

        uint attempts = 0;

        while (attempts < 10000 && frontier.Count > 0)
        {
            attempts++;
            Vector2I current = frontier.Dequeue();
            if (current == goal.pos)
            {
                validPath = true;
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
                    float newCost = 1;
                    if ((!flowCost.ContainsKey(next) || newCost < flowCost[next]) && simManager.GetRegion(next).habitable)
                    {
                        frontier.Enqueue(next, newCost);
                        flowCost[next] = newCost;
                        flow[next] = current;
                    }
                }
            }
        }
        if (validPath)
        {
            Vector2I pos = goal.pos;
            while (pos != start.pos)
            {
                path.Enqueue(simManager.GetRegion(pos));
                pos = flow[pos];
            }
        }
        return validPath;
    }
    public Region PickRandomBorder()
    {
        if (borderingRegions.Count > 0)
        {
            return borderingRegions[rng.Next(0, borderingRegions.Count)];
        }
        return null;
    }

    public float GetFoodSurplus()
    {
        return ((Pop.FromNativePopulation(workforce) * Pop.workforceNutritionNeed) + (Pop.FromNativePopulation(dependents) * Pop.dependentNutritionNeed))/(float)economy.GetTotalNutrition() - 1f;
    }
}
