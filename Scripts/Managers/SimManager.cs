using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using Godot;
using FileAccess = Godot.FileAccess;

[Serializable]
public partial class SimManager : Node
{
    public Node2D terrainMap;
    [Export(PropertyHint.Range, "4,16,4")]
    public int tilesPerRegion = 4;
    [Export]
    public TileMapLayer reliefs;
    [Export]
    public TimeManager timeManager;

    public Tile[,] tiles;
    public List<Region> regions { get; set; } = new List<Region>();
    public List<Region> habitableRegions = new List<Region>();
    public List<Region> tradeCenters { get; set; } = new List<Region>();
    public List<Region> paintedRegions = new List<Region>();
    public Vector2I terrainSize;
    public static Vector2I worldSize;
    public static System.Threading.Mutex m = new System.Threading.Mutex();
    MapManager mapManager;
    public WorldGenerator worldGenerator = LoadingScreen.generator;

    // Population
    public List<Pop> pops { get; set; } = new List<Pop>();
    public long worldPopulation { get; set; } = 0;
    public long worldWorkforce { get; set; } = 0;
    public long worldDependents { get; set; } = 0;
    public uint populatedRegions;
    public float maxWealth = 0;
    public float maxTradeWeight = 0;
    public List<Culture> cultures { get; set; } = new List<Culture>();
    public List<State> states { get; set; } = new List<State>();
    public List<Army> armies { get; set; } = new List<Army>();
    public List<Character> characters { get; set; } = new List<Character>();
    public List<War> wars { get; set; } = new List<War>();
    public List<War> endedWars = new List<War>();

    public int maxPopsPerRegion = 50;
    public long popTaskId = 0;
    public uint currentBatch = 2;

    Random rng = new Random();

    // Events
    public delegate void SimulationInitializedEventHandler();
    #region Utility
    public override void _Ready()
    {
        terrainMap = GetNode<Node2D>("/root/Game/Terrain Map");
        timeManager = GetParent().GetNode<TimeManager>("/root/Game/Time Manager");
        mapManager = (MapManager)GetParent().GetNode<Node>("Map Manager");

        // Connection
        WorldGenerator.worldgenFinishedEvent += OnWorldgenFinished;
        //Connect("WorldgenFinished", new Callable(this, nameof()));
    }

    public Vector2I GlobalToRegionPos(Vector2 pos)
    {
        return (Vector2I)(pos / (terrainMap.Scale * 16)) / tilesPerRegion;
    }

    public Vector2 RegionToGlobalPos(Vector2 regionPos)
    {
        return tilesPerRegion * (regionPos * (terrainMap.Scale * 16));
    }
    public Region GetRegion(int x, int y)
    {
        int lx = Mathf.PosMod(x, worldSize.X);
        int ly = Mathf.PosMod(y, worldSize.Y);

        int index = (lx * worldSize.Y) + ly;
        return regions[index];
    }
    public Region GetRegion(Vector2I pos)
    {
        int lx = Mathf.PosMod(pos.X, worldSize.X);
        int ly = Mathf.PosMod(pos.Y, worldSize.Y);

        int index = (lx * worldSize.Y) + ly;
        return regions[index];
    }
    #endregion
    #region Saving & Loading
    public void SaveSimToFile(string saveName)
    {
        JsonSerializerOptions options = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        };
        options.Converters.Add(new TwoDimensionalArrayConverter<Tile>());
        options.Converters.Add(new TwoDimensionalArrayConverter<Biome>());
        if (DirAccess.Open("user://saves") == null)
        {
            DirAccess.MakeDirAbsolute("user://saves");
        }
        if (DirAccess.Open($"user://saves/{saveName}") == null)
        {
            DirAccess.MakeDirAbsolute($"user://saves/{saveName}");
        }
        FileAccess save = FileAccess.Open($"user://saves/{saveName}/terrainData.pxsave", Godot.FileAccess.ModeFlags.Write);
        save.StoreLine(JsonSerializer.Serialize(this, options));
    }
    #endregion
    #region Initialization
    private void OnWorldgenFinished(object sender, EventArgs e)
    {
        PopObject.simManager = this;
        PopObject.timeManager = timeManager;
        Army.simManager = this;
        Pop.simManager = this;
        War.simManager = this;

        // Load Resources Before Buildings        
        terrainSize = worldGenerator.WorldSize;
        worldSize = terrainSize / tilesPerRegion;
        #region Tile Initialization
        tiles = new Tile[terrainSize.X, terrainSize.Y];
        for (int x = 0; x < terrainSize.X; x++)
        {
            for (int y = 0; y < terrainSize.Y; y++)
            {
                Tile newTile = new Tile();
                tiles[x, y] = newTile;

                newTile.biome = AssetManager.GetBiome(worldGenerator.BiomeMap[x, y]);
                newTile.temperature = worldGenerator.GetUnitTemp(worldGenerator.TempMap[x, y]);
                newTile.moisture = worldGenerator.GetUnitRainfall(worldGenerator.RainfallMap[x, y]);
                newTile.elevation = worldGenerator.GetUnitElevation(worldGenerator.HeightMap[x, y]);

                newTile.arability = newTile.biome.arability;
                newTile.navigability = newTile.biome.navigability;
                newTile.survivalbility = newTile.biome.survivability;

                switch (newTile.biome.type)
                {
                    case "land":
                        newTile.terrainType = TerrainType.LAND;
                        break;
                    case "water":
                        newTile.terrainType = TerrainType.WATER;
                        break;
                    default:
                        newTile.terrainType = TerrainType.ICE;
                        break;
                }
                if (newTile.terrainType == TerrainType.LAND)
                {
                    if (worldGenerator.HeightMap[x, y] > WorldGenerator.MountainThreshold)
                    {
                        newTile.navigability *= 0.25f;
                        newTile.arability *= 0.25f;
                        newTile.survivalbility *= 0.8f;
                        newTile.terrainType = TerrainType.MOUNTAINS;
                    }
                    else if (worldGenerator.HeightMap[x, y] > WorldGenerator.HillThreshold)
                    {
                        newTile.navigability *= 0.5f;
                        newTile.arability *= 0.5f;
                        newTile.terrainType = TerrainType.HILLS;
                    }
                }
                // checks for coasts
                for (int dx = -1; dx < 2; dx++)
                {
                    for (int dy = -1; dy < 2; dy++)
                    {
                        if ((dx != 0 && dy != 0) || (dx == 0 && dy == 0) || newTile.coastal || newTile.terrainType == TerrainType.WATER)
                        {
                            continue;
                        }
                        int nx = Mathf.PosMod(x + dx, worldSize.X);
                        int ny = Mathf.PosMod(y + dy, worldSize.Y);
                        if (AssetManager.GetBiome(worldGenerator.BiomeMap[nx, ny]).type == "water")
                        {
                            newTile.navigability = Mathf.Clamp(newTile.navigability * 1.5f, 0f, 1f);
                            newTile.arability = Mathf.Clamp(newTile.arability * 1.5f, 0f, 1f);
                            newTile.coastal = true;
                        }
                    }
                }
            }
        }
        #endregion
        #region Region Creation
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                // Creates a region
                Region newRegion = new Region();

                newRegion.tiles = new Tile[tilesPerRegion, tilesPerRegion];
                newRegion.biomes = new Biome[tilesPerRegion, tilesPerRegion];

                newRegion.pos = new Vector2I(x, y);
                regions.Add(newRegion);
                for (int tx = 0; tx < tilesPerRegion; tx++)
                {
                    for (int ty = 0; ty < tilesPerRegion; ty++)
                    {
                        // Adds subregion to tile
                        Tile tile = tiles[x * tilesPerRegion + tx, y * tilesPerRegion + ty];
                        newRegion.tiles[tx, ty] = tile;
                        // Adds biomes to tile
                        newRegion.biomes[tx, ty] = tile.biome;
                    }
                }
                // Calc average fertility
                newRegion.CalcAverages();
                // Checks habitability
                newRegion.CheckHabitability();
                if (newRegion.habitable)
                {
                    habitableRegions.Add(newRegion);
                }
                // Calc max populaiton
                newRegion.CalcProfessionRequirements();
            }
        }
        #endregion
        BorderingRegions();
        InitPops();
        mapManager.InitMapManager();
    }

    void BorderingRegions()
    {
        Parallel.ForEach(regions, region =>
        {
            int habitableBorderCount = 0;
            int i = 0;
            for (int dx = -1; dx < 2; dx++)
            {
                for (int dy = -1; dy < 2; dy++)
                {
                    if ((dx == 0 && dy == 0) || (dx != 0 && dy != 0))
                    {
                        continue;
                    }
                    Region r = GetRegion(region.pos.X + dx, region.pos.Y + dy);
                    region.borderingRegions[i] = r;
                    i++;
                    if (r.habitable)
                    {
                        habitableBorderCount++;
                    }
                    if (r.habitable || region.habitable)
                    {
                        lock (paintedRegions)
                        {
                            paintedRegions.Add(region);
                        }
                    }
                }
            }
            //GD.Print(habitableBorderCount);
            region.habitableBorderingRegions = new Region[habitableBorderCount];
            i = 0;
            for (int dx = -1; dx < 2; dx++)
            {
                for (int dy = -1; dy < 2; dy++)
                {
                    if ((dx == 0 && dy == 0) || (dx != 0 && dy != 0))
                    {
                        continue;
                    }
                    Region r = GetRegion(region.pos.X + dx, region.pos.Y + dy);
                    if (r.habitable)
                    {
                        region.habitableBorderingRegions[i] = r;
                        i++;
                    }

                }
            }
        });
    }
    void InitPops()
    {
        foreach (Region region in habitableRegions)
        {   
            double nodeChance = 0.004;

            if (rng.NextDouble() <= nodeChance && region.Migrateable())
            {
                long startingPopulation = Pop.ToNativePopulation(10000);
                Culture culture = CreateCulture();
                foreach (Region testRegion in habitableRegions)
                {
                    if (testRegion.pops.Count > 0 && testRegion.pos.DistanceTo(region.pos) <= 14)
                    {
                        culture = testRegion.pops[0].culture;
                    }
                }
                CreatePop((long)(startingPopulation * 0.25f), (long)(startingPopulation * 0.75f), region, new Tech(), culture, Profession.FARMER);
            }
        }
    }
    #endregion
    #region Pop Update
    public void UpdatePops()
    {
        /*
        try
        {
            var partitioner = Partitioner.Create(pops.ToArray());
            Parallel.ForEach(partitioner, (pop) =>
            {

            });
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }  
        */      
        foreach (Pop pop in pops.ToArray())
        {
            ulong startTime = Time.GetTicksMsec();
            if (pop.population <= Pop.ToNativePopulation(1))
            {
                //m.WaitOne();
                DestroyPop(pop);
                //m.ReleaseMutex();
            }
            else
            {
                pop.EconomyUpdate();
                pop.GrowPop();
                if (pop.batchId == timeManager.GetMonth(timeManager.ticks))
                {
                    pop.TechnologyUpdate();
                    //pop.ProfessionTransitions();
                    try
                    {
                        pop.Migrate();
                    }
                    catch (Exception e)
                    {
                        GD.PushError(e);
                    }
                    
                }
            }
        }
        /*

        // GD.Print("Pops Processing Time: " + (Time.GetTicksMsec() - tickStartTime) + " ms");
        // GD.Print("  Pops Delete Time: " + destroyTime + " ms");
        // GD.Print("  Pops Grow Time: " + growTime + " ms");
        // GD.Print("  Pops Move Time: " + migrateTime + " ms");
        */
    }
    #endregion
    #region Region Update
    public void UpdateRegions()
    {
        uint countedPoppedRegions = 0;

        long worldPop = 0;
        //int regionBatches = 8;
        ulong rStartTime = Time.GetTicksMsec();
        ulong totalPopTime = 0;
        ulong totalEconomyTime = 0;
        ulong distributionTime = 0;
        ulong totalConquestTime = 0;
        try
        {
            var partitioner = Partitioner.Create(regions);
            ulong startTime = Time.GetTicksMsec();
            Parallel.ForEach(partitioner, (region) =>
            {
                //region.CalcTradeRoutes();
                region.UpdateWealth();
                region.DistributeWealth();
                //region.zoneSize = 1;
                region.connectedTiles = new List<Region>();
                region.hasBaseTradeWeight = false;
                region.hasTradeWeight = false;
                region.tradeIncome = 0f;
                region.taxIncome = 0f;
                region.linkUpdateCountdown--;
                region.zoneSize = region.connectedTiles.Count;
            });

            distributionTime = Time.GetTicksMsec() - startTime; 
            foreach (Region region in habitableRegions)
            {
                if (region.pops.Count > 0)
                {
                    startTime = Time.GetTicksMsec();
                    region.MergePops();
                    //GD.Print("  Pops Time: " + (Time.GetTicksMsec() - startTime).ToString("#,##0 ms"));
                    region.CheckPopulation();
                    //GD.Print("  Population Check Time: " + (Time.GetTicksMsec() - startTime).ToString("#,##0 ms"));
                    region.CalcProfessionRequirements();
                    totalPopTime += Time.GetTicksMsec() - startTime;
                    startTime = Time.GetTicksMsec();
                    // Economy
                    region.CalcBaseWealth();
                    if (region.linkUpdateCountdown < 1 || region.tradeLink == null)
                    {
                        region.linkUpdateCountdown = 12;
                        region.LinkTrade();
                    }



                    //region.CalcTaxes();
                    totalEconomyTime += Time.GetTicksMsec() - startTime;
                    //GD.Print("  Wealth Time: " + (Time.GetTicksMsec() - startTime).ToString("#,##0 ms"));
                    region.RandomStateFormation();
                    region.UpdateOccupation();
                    startTime = Time.GetTicksMsec();
                    if (region.owner != null)
                    {
                        
                        region.StateBordering();
                        if (region.frontier && region.owner.rulingPop != null && region.occupier == null)
                        {
                            region.NeutralConquest();
                        }
                        region.MilitaryConquest();
                    }
                    totalConquestTime += Time.GetTicksMsec() - startTime;
                    startTime = Time.GetTicksMsec();
                    //GD.Print("  Conquest Time: " + (Time.GetTicksMsec() - startTime).ToString("#,##0 ms"));
                    countedPoppedRegions += 1;
                    worldPop += region.population;
                }
                else
                {
                    region.linkUpdateCountdown = 0;
                }
            }
            tradeCenters = new List<Region>();
            foreach (Region region in habitableRegions)
            {
                if (region.owner != null)
                {
                    if (region.occupier != null && !region.owner.enemies.Contains(region.occupier))
                    {
                        region.occupier = null;
                    }
                }
                else
                {
                    region.occupier = null;
                }
                if (region.wealth > maxWealth)
                {
                    maxWealth = region.wealth;
                }
                if (region.GetTradeWeight() > maxTradeWeight)
                {
                    maxTradeWeight = region.GetTradeWeight();
                }
            }
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }
        //GD.Print("Regions Time: " + (Time.GetTicksMsec() - rStartTime).ToString("#,##0 ms"));
        //GD.Print("  Pop Time: " + totalPopTime.ToString("#,##0 ms"));
        //GD.Print("  Economy Time: " + totalEconomyTime.ToString("#,##0 ms"));
        //GD.Print("  Economy Completion Time: " + distributionTime.ToString("#,##0 ms"));
        //GD.Print("  Conquest Time: " + totalConquestTime.ToString("#,##0 ms"));
        populatedRegions = countedPoppedRegions;
        worldPopulation = worldPop;
    }
    #endregion
    #region State Update
    public void UpdateStates()
    {
        try
        {
            foreach (State state in states)
            {
                if (state.rulingPop != null)
                {
                    state.tech = state.rulingPop.tech;
                }
                state.GetRealmBorders();
            }
            foreach (State state in states.ToArray())
            {
                if (state.rulingPop != null)
                {
                    state.maxSize = 6 + state.rulingPop.tech.societyLevel;
                }

                state.age += TimeManager.ticksPerMonth;
                state.UpdateStability();
                if (state.sovereignty != Sovereignty.INDEPENDENT)
                {
                    state.timeAsVassal += TimeManager.ticksPerMonth;
                    state.UpdateLoyalty();
                }
                if (state.regions.Count < 1)
                {
                    DeleteState(state);
                    continue;
                }

                state.UpdateCapital();

                state.Capitualate();
                state.RelationsUpdate();
                state.UpdateDiplomacy();
                state.EndWars();
                state.StartWars();
                state.UpdateEnemies();

                if (state.rulingPop == null)
                {
                    // State Collapse or Smth
                    if (rng.NextSingle() < 0.5f)
                    {
                        Region r = state.regions[rng.Next(0, state.regions.Count)];
                        state.RemoveRegion(r);
                    }
                }
            }
            var partitioner = Partitioner.Create(states);
            Parallel.ForEach(partitioner, (state) =>
            {
                state.CountStatePopulation();                
                state.Recruitment();
                state.UpdateDisplayColor();
                state.UpdateDisplayName();          
            });
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }

    }
    #endregion
    #region Culture Update
    public void UpdateCultures()
    {
        foreach (Culture culture in cultures.ToArray())
        {
            culture.age += TimeManager.ticksPerMonth;
        }
    }
    #endregion
    #region War Update
    public void UpdateWars()
    {
        foreach (War war in wars)
        {
            war.age += TimeManager.daysPerTick;
        }
    }
    #endregion
    #region SimTick
    public void SimTick()
    {
        UpdateWars();
    }
    public void SimMonth()
    {
        ulong startTime = Time.GetTicksMsec();
        try
        {
            UpdatePops();
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }
        //GD.Print("Pops Time: " + (Time.GetTicksMsec() - startTime).ToString("#,##0 ms"));
        startTime = Time.GetTicksMsec();
        UpdateRegions();
        startTime = Time.GetTicksMsec();
        UpdateStates();
        //GD.Print("States Time: " + (Time.GetTicksMsec() - startTime).ToString("#,##0 ms"));
        UpdateCultures();
        UpdateWars();
    }
    public void SimYear()
    {

    }
    #endregion

    #region Creation
    #region Pops Creation
    public Pop CreatePop(long workforce, long dependents, Region region, Tech tech, Culture culture, Profession profession = Profession.FARMER)
    {
        currentBatch++;
        if (currentBatch > 12)
        {
            currentBatch = 2;
        }
        Pop pop = new Pop()
        {
            batchId = currentBatch,
            tech = tech.Clone(),
            profession = profession,
            workforce = workforce,
            dependents = dependents,
            population = workforce + dependents,
        };
        //pop.ChangePopulation(workforce, dependents);
        lock (pops)
        {
            pops.Add(pop);
        }
        lock (culture)
        {
            culture.AddPop(pop, culture);
        }
        lock (region)
        {
            region.AddPop(pop, region);
        }

        return pop;
    }

    public void DestroyPop(Pop pop)
    {
        
        if (pop.region.owner != null && pop.region.owner.rulingPop == pop)
        {
            lock (pop.region.owner)
            {
                pop.region.owner.rulingPop = null;
            }
        }
        pop.ClaimLand(-pop.ownedLand);
        lock (pop.region)
        {
            pop.region.RemovePop(pop, pop.region);
        }
        lock (pop.culture)
        {
            pop.culture.RemovePop(pop, pop.culture);
        }
        lock (pops)
        {
            pops.Remove(pop);
        }
    }
    #endregion
    #region Cultures Creation
    public Culture CreateCulture()
    {
        float r = rng.NextSingle();
        float g = rng.NextSingle();
        float b = rng.NextSingle();
        Culture culture = new Culture()
        {
            name = "Culturism",
            color = new Color(r, g, b),
            tickFounded = timeManager.ticks
        };

        cultures.Append(culture);

        return culture;
    }
    #endregion
    #region States Creation
    public void CreateState(Region region)
    {
        if (region.owner == null)
        {
            float r = Mathf.Lerp(0.2f, 1f, rng.NextSingle());
            float g = Mathf.Lerp(0.2f, 1f, rng.NextSingle());
            float b = Mathf.Lerp(0.2f, 1f, rng.NextSingle());
            State state = new State()
            {
                name = NameGenerator.GenerateNationName(),
                color = new Color(r, g, b),
                capital = region,
                tickFounded = timeManager.ticks
            };
            state.AddRegion(region);
            states.Add(state);
        }
    }

    public void DeleteState(State state)
    {
        states.Remove(state);
        foreach (Region region in state.regions.ToArray())
        {
            state.RemoveRegion(region);
        }
    }
    #endregion
    #region Characters Creation
    #endregion
    #region Diplomacy Creation
    #endregion
    #endregion
}