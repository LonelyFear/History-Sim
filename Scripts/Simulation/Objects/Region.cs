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
    public float tradeDifficulty;
    public float movementDifficulty;

    public Vector2I pos;
    public float avgFertility;
    public int landCount;
    public State owner = null;
    public List<Army> armies;

    // Demographics
    public long maxPopulation = 0;
    public double wealth;

    public Economy economy = new Economy();
    public List<Region> borderingRegions = new List<Region>();

    public int currentMonth;
    public bool border;
    public bool frontier;
    public bool needsJobs { private set; get; }
    public bool needsWorkers { private set; get; }
    public List<Crop> plantableCrops = new List<Crop>();
    public float ariableLand;
    public bool fieldsFull;
    public void CalcAvgFertility()
    {
        name = "Region";
        landCount = 0;
        float f = 0;
        for (int x = 0; x < simManager.tilesPerRegion; x++)
        {
            for (int y = 0; y < simManager.tilesPerRegion; y++)
            {
                Tile tile = tiles[x, y];
                if (tile.IsLand())
                {
                    landCount++;
                    f += tile.fertility;

                    switch (tile.terrainType)
                    {
                        case TerrainType.LAND:
                            ariableLand++;
                            tradeDifficulty += 0.1f;
                            break;
                        case TerrainType.HILLS:
                            ariableLand += 0.5f;
                            tradeDifficulty += 0.5f;
                            break;
                        case TerrainType.MOUNTAINS:
                            ariableLand += 0.1f;
                            tradeDifficulty++;
                            break;
                        default:
                            if (tile.biome.id == "river")
                            {
                                ariableLand++;
                            }
                            break;
                    }
                }
                else if (tile.terrainType == TerrainType.WATER)
                {
                    coastal = true;
                }
            }
        }

        tradeDifficulty /= landCount;     
        //economy.ChangeResourceAmount(simManager.GetResource("grain"), 100);
        avgFertility = f / landCount;

        foreach (Crop crop in AssetManager.crops.Values)
        {
            if (avgFertility <= crop.maxFertility && avgFertility >= crop.minFertility)
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
                    switch (tile.terrainType)
                    {
                        case TerrainType.LAND:
                            maxPopulation += (long)(Pop.ToNativePopulation(1000) * biome.fertility);
                            tile.maxPopulation = (long)(Pop.ToNativePopulation(1000) * biome.fertility);
                            break;
                        case TerrainType.HILLS:
                            maxPopulation += (long)(Pop.ToNativePopulation(800) * biome.fertility);
                            tile.maxPopulation = (long)(Pop.ToNativePopulation(800) * biome.fertility);
                            break;
                        case TerrainType.MOUNTAINS:
                            maxPopulation += (long)(Pop.ToNativePopulation(500) * biome.fertility);
                            tile.maxPopulation = (long)(Pop.ToNativePopulation(500) * biome.fertility);
                            break;
                    }
                }
            }
        }
    }
    #region Nations
    public void RandomStateFormation()
    {
        if (owner == null && population > Pop.ToNativePopulation(1000) && rng.NextDouble() <= 0.0001)
        {
            SimManager.m.WaitOne();
            simManager.CreateState(this);
            SimManager.m.ReleaseMutex();

            owner.population = population;
            owner.workforce = workforce;
            Pop basePop = pops[0];
            Pop rulingPop = basePop.ChangeProfession(Pop.ToNativePopulation(25), Pop.ToNativePopulation(75), Profession.ARISTOCRAT);

            owner.rulingPop = rulingPop;
            owner.SetLeader(simManager.CreateCharacter(owner.rulingPop));
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
                if (!owner.borderingStates.Contains(region.owner))
                {
                    owner.borderingStates.Append(region.owner);
                }
            }
        }
    }

    public void NeutralConquest()
    {
        Region region = borderingRegions[rng.Next(0, borderingRegions.Count)];
        if (region != null && region.pops.Count != 0 && region.owner == null && rng.NextSingle() < 0.005f)
        {
            SimManager.m.WaitOne();
            Battle result = Battle.CalcBattle(region, owner, null, owner.GetArmyPower() / 2, (long)(region.workforce * 0.95f));
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
    #region Checks & Taxes
    public void CheckPopulation()
    {
        CountPopulation();
        if (population < Pop.ToNativePopulation(1) && owner != null)
        {
            owner.RemoveRegion(this);
        }
    }

    public void PopWealth(Pop pop)
    {
        pop.totalWealth = 0;
        switch (pop.profession)
        {
            case Profession.FARMER:
                pop.totalWealth += 1 * Pop.FromNativePopulation(pop.workforce);
                break;
            case Profession.MERCHANT:
                pop.totalWealth += 2 * Pop.FromNativePopulation(pop.workforce); ;
                break;
            case Profession.ARISTOCRAT:
                pop.totalWealth += 3 * Pop.FromNativePopulation(pop.workforce);
                break;
        }
        pop.CalcWealthPerCapita();

        double taxesPerCapita = 0;
        switch (pop.profession)
        {
            case Profession.FARMER:
                taxesPerCapita = pop.wealthPerCapita * 0.2f;
                break;
            case Profession.MERCHANT:
                taxesPerCapita = pop.wealthPerCapita * 0.1f;
                break;
            case Profession.ARISTOCRAT:
                taxesPerCapita = pop.wealthPerCapita * 0.05f;
                break;
        }
        double taxesCollected = taxesPerCapita * pop.population;
        pop.totalWealth -= taxesCollected;
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
                    merger.ChangePopulation(-merger.workforce, -merger.dependents);
                    break;
                }
            }
        }
    }

    #region PopGrowth
    public void GrowPop(Pop pop)
    {
        pop.canMove = true;

        float bRate;
        if (pop.population < Pop.ToNativePopulation(2))
        {
            bRate = 0;
        }
        else
        {
            bRate = pop.GetBirthRate();
        }
        if (population > maxPopulation)
        {
            bRate *= 0.75f;
        }

        float NIR = (bRate - pop.GetDeathRate()) / 12f;

        long change = Mathf.RoundToInt((pop.workforce + pop.dependents) * NIR);
        long dependentChange = Mathf.RoundToInt(change * pop.targetDependencyRatio);
        long workforceChange = change - dependentChange;
        pop.ChangeWorkforce(workforceChange);
        pop.ChangeDependents(dependentChange);
    }
    #endregion

    public bool Migrateable(Pop pop)
    {
        if (habitable)
        {
            if (avgFertility > 0.15f)
            {
                return true;
            }
            else if (pop.tech.scienceLevel * 0.05 > avgFertility)
            {
                return true;
            }
        }
        return false;
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
            npop.canMove = false;
            pop.ChangePopulation(-movedWorkforce, -movedDependents);
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

    #region Farmin'
    public Crop SelectCrop()
    {
        if (plantableCrops.Count > 0)
        {
            return plantableCrops[0];
        }
        else
        {
            return null;
        }
    }
    public void GrowCrops()
    {
        fieldsFull = false;
        Crop crop = SelectCrop();
        if (crop != null)
        {
            float cropsPerAribleLand = 230f;
            float smoothing = 0.05f;

            long totalFarmers = (long)Mathf.Round(Pop.FromNativePopulation(professions[Profession.FARMER]));
            double totalWork = cropsPerAribleLand * ariableLand * (1 - Mathf.Pow(Mathf.E, -smoothing * (totalFarmers / ariableLand)));
            foreach (BaseResource yield in crop.yields.Keys)
            {
                economy.ChangeResourceAmount(yield, crop.yields[yield] * totalWork * 2f);
            }
            fieldsFull = totalWork == cropsPerAribleLand * ariableLand;
        }
    }
    #endregion

    public Region PickRandomBorder()
    {
        if (borderingRegions.Count > 0)
        {
            return borderingRegions[rng.Next(0, borderingRegions.Count)];
        }
        return null;
    }
}
