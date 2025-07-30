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
    public float lastWealth = 0;
    public float lastBaseWealth = 0;
    public float baseWealth;
    public float wealth;
    public float taxIncome;
    public float tradeIncome;
    public bool habitableAdjacent;
    

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
    public void TryFormState()
    {
        if (professions[Profession.ARISTOCRAT] > 0 && rng.NextSingle() > wealth * 0.001f && owner == null)
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
            owner.SetLeader(simManager.CreateCharacter(owner.rulingPop));
            owner.UpdateDisplayName();
        }
    }
    public void RandomStateFormation()
    {
        if (rng.NextSingle() < 0.001)
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
            owner.SetLeader(simManager.CreateCharacter(owner.rulingPop));
            owner.UpdateDisplayName();            
        }
    }
    public void StateBordering()
    {
        border = false;
        frontier = false;
        List<State> borders = new List<State>();
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
                    borders.Add(region.owner);
 
                }
            }
        }
        SimManager.m.WaitOne();
        owner.borderingStates.AddRange(borders);
        SimManager.m.ReleaseMutex();        
    }
    public bool DrawBorder(Region r)
    {
        if (r == null)
        {
            return false;
        }
        bool hasPops = pops.Count > 0;
        bool targetHasPops = r.pops.Count > 0;
        if (hasPops != targetHasPops || (hasPops && r.owner != owner))
        {
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
        if (region != null && region.pops.Count != 0 && region.owner == null && rng.NextSingle() < 0.01f / (1f + (owner.regions.Count /(float)owner.maxSize)))
        {
            long defendingCivilians = region.workforce - region.professions[Profession.ARISTOCRAT];
            Battle result = Battle.CalcBattle(region, owner, null, owner.GetArmyPower(), 0, 0, (long)(defendingCivilians));

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

    public void CalcBaseWealth()
    {
        lastBaseWealth = baseWealth;
        lastWealth = wealth;
        float techFactor = 1 + (pops[0].tech.scienceLevel * 0.1f);

        long farmers = Pop.FromNativePopulation(professions[Profession.FARMER]);
        long nonFarmers = Pop.FromNativePopulation(workforce - professions[Profession.FARMER]);

        float baseProduction = (farmers * 0.01f) + (nonFarmers * 0.005f) + (Pop.FromNativePopulation(dependents) * 0.002f);
        baseWealth = baseProduction * (arableLand / landCount) * techFactor;
        taxIncome = 0;
        tradeIncome = 0;
    }
    public void CalcTaxes()
    {
        if (owner != null && owner.capital != null && owner.capital != this)
        {
            float totalTaxIncome = baseWealth * owner.taxRate;
            float capitalTaxIncome = totalTaxIncome * 0.1f;
            float distributedTaxIncome = (totalTaxIncome * 0.9f) / (owner.regions.Count - 1);

            SimManager.m.WaitOne();
            owner.capital.taxIncome += capitalTaxIncome;
            SimManager.m.ReleaseMutex();
            
            foreach (Region r in owner.regions)
            {
                if (r != owner.capital)
                {
                    SimManager.m.WaitOne();
                    r.taxIncome += distributedTaxIncome;
                    SimManager.m.ReleaseMutex();
                }
            }
        }
    }
    public void Trade()
    {
        
    }
    public void CalcTradeWeight()
    {
        tradeWeight = 0f;
        long notMerchants = Pop.FromNativePopulation(workforce - professions[Profession.MERCHANT]);
        long merchants = Pop.FromNativePopulation(professions[Profession.MERCHANT]);
        float populationTradeWeight = (notMerchants * 0.005f) + (merchants * 0.01f);
        float politySizeTradeWeight = 0f;
        if (owner != null && owner.capital == this)
        {
            politySizeTradeWeight = owner.regions.Count * 0.5f;
        }
        tradeWeight = ((navigability * 3f) + populationTradeWeight + politySizeTradeWeight) * navigability;
    }    
    public void UpdateWealth()
    {
        wealth = baseWealth + taxIncome + tradeIncome;
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

    public void MovePop(Pop pop, Region destination, long movedWorkforce, long movedDependents)
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
            Pop npop = simManager.CreatePop(movedWorkforce, movedDependents, destination, pop.tech, pop.culture, pop.profession);
            pop.ChangePopulation(-movedWorkforce, -movedDependents);
            SimManager.m.ReleaseMutex();
        }
    }

    #endregion
    public static bool GetPathToRegion(Region start, Region goal, out List<Region> path)
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
                path.Add(simManager.GetRegion(pos));
                pos = flow[pos];
            }
        }
        return validPath;
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
