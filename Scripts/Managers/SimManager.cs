using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MessagePack;
using MessagePack.Resolvers;
using FileAccess = Godot.FileAccess;

[MessagePackObject(keyAsPropertyName: true)]
public class SimManager
{
    [IgnoreMember] public SimNodeManager node;
    [IgnoreMember] public ulong currentID = 0;
    [IgnoreMember]
    public Node2D terrainMap;
    [IgnoreMember]
    [Export(PropertyHint.Range, "4,16,4")]
    public int tilesPerRegion = 4;
    [Export]
    [IgnoreMember]
    public TileMapLayer reliefs;
    [Export]
    [IgnoreMember]
    public TimeManager timeManager;
    public uint tick;

    public Tile[,] tiles;
    [IgnoreMember] public List<Region> habitableRegions = new List<Region>();
    public List<Region> tradeCenters { get; set; } = new List<Region>();
    [IgnoreMember] public List<Region> paintedRegions = new List<Region>();
    public List<Region> regions { get; set; } = new List<Region>();
    [IgnoreMember] public Dictionary<ulong, Region> regionIds { get; set; } = new Dictionary<ulong, Region>();

    [IgnoreMember]
    public Vector2I terrainSize;
    [IgnoreMember]
    public static Vector2I worldSize;
    [IgnoreMember]
    public static System.Threading.Mutex m = new System.Threading.Mutex();
    [IgnoreMember]
    public WorldGenerator worldGenerator;
    [IgnoreMember]
    public MapManager mapManager;
    [IgnoreMember]
    bool instanceDeleted = false;

    // Population
    
    public long worldPopulation { get; set; } = 0;
    public long highestPopulation { get; set; } = 0;
    public long worldWorkforce { get; set; } = 0;
    public long worldDependents { get; set; } = 0;
    public uint populatedRegions;
    public float maxWealth = 0;
    public float maxTradeWeight = 0;
    public List<Pop> pops { get; set; } = new List<Pop>();
    [IgnoreMember] public Dictionary<ulong, Pop> popsIds { get; set; } = new Dictionary<ulong, Pop>();
    public List<Culture> cultures { get; set; } = new List<Culture>();
    [IgnoreMember] public Dictionary<ulong, Culture> cultureIds { get; set; } = new Dictionary<ulong, Culture>();
    public List<State> states { get; set; } = new List<State>();
    [IgnoreMember] public Dictionary<ulong, State> statesIds { get; set; } = new Dictionary<ulong, State>();
    public List<Army> armies { get; set; } = new List<Army>();
    public List<TradeZone> tradeZones { get; set; } = new List<TradeZone>();
    [IgnoreMember] public Dictionary<ulong, TradeZone> tradeZonesIds { get; set; } = new Dictionary<ulong, TradeZone>();
    public List<Character> characters { get; set; } = new List<Character>();
    [IgnoreMember] public Dictionary<ulong, Character> charactersIds { get; set; } = new Dictionary<ulong, Character>();
    public List<War> wars { get; set; } = new List<War>();
    [IgnoreMember] public Dictionary<ulong, War> warIds { get; set; } = new Dictionary<ulong, War>();
    public List<War> endedWars = new List<War>();
    public uint currentBatch = 2;
    [IgnoreMember] public bool simLoadedFromSave = false;

    [IgnoreMember]
    Random rng = new Random();

    // Events
    //public delegate void SimulationInitializedEventHandler();
    public delegate void ObjectDeletedEvent(ulong id);
    public ObjectDeletedEvent objectDeleted;
    // Debug info
    public ulong totalStepTime;
    public ulong totalPopsTime;
    public ulong totalStateTime;
    public ulong totalRegionTime;
    public ulong totalCharacterTime;
    public ulong totalMiscTime;
    #region Utility

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
    public void SaveSimToFile(string path)
    {
        regions.ForEach(r => r.PrepareForSave());
        //pops.ForEach(r => r.PrepareForSave());
        //wars.ForEach(r => r.PrepareForSave());
        //states.ForEach(r => r.PrepareForSave());
        tradeZones.ForEach(r => r.PrepareForSave());
        //cultures.ForEach(r => r.PreparePopObjectForSave());
        tick = timeManager.ticks;

        var resolver = CompositeResolver.Create(
            [new Vector2IFormatter(), new ColorFormatter(), new NodePathFormatter(), new GDStringNameFormatter()],
            [StandardResolver.Instance]
        );

        var options = MessagePackSerializerOptions.Standard.WithResolver(resolver).WithCompression(MessagePackCompression.Lz4BlockArray);

        FileAccess save = FileAccess.Open($"{path}/sim_data.pxsave", FileAccess.ModeFlags.Write);
        save.StoreBuffer(MessagePackSerializer.Serialize(this, options));
    }
    public static SimManager LoadSimFromFile(string path)
    {
        if (DirAccess.Open(path) == null && DirAccess.Open(path).FileExists(path + "/terrain_data.pxsave") && DirAccess.Open(path).FileExists(path + "/sim_data.pxsave"))
        {
            GD.PushError($"Save at path {path} not found");
            return null;
        }
        var resolver = CompositeResolver.Create(
            [new Vector2IFormatter(), new ColorFormatter()],
            [StandardResolver.Instance]
        );
        var options = MessagePackSerializerOptions.Standard.WithResolver(resolver).WithCompression(MessagePackCompression.Lz4BlockArray);
        //FileAccess save = FileAccess.Open($"{path}/sim_data.pxsave", FileAccess.ModeFlags.Read);
        SimManager sim = MessagePackSerializer.Deserialize<SimManager>(FileAccess.GetFileAsBytes($"{path}/sim_data.pxsave"), options);
        sim.simLoadedFromSave = true;
        return sim;
    }
    #endregion
    #region Initialization
    public void RebuildAfterSave()
    {
        timeManager.ticks = tick;
        foreach (Region region in regions)
        {
            regionIds.Add(region.id, region);
        }
        foreach (Pop pop in pops)
        {
            popsIds.Add(pop.id, pop);
        }
        foreach (State state in states)
        {
            statesIds.Add(state.id, state);
        }
        foreach (Culture culture in cultures)
        {
            cultureIds.Add(culture.id, culture);
        }
        foreach (War war in wars)
        {
            warIds.Add(war.id, war);
        }
        foreach (TradeZone tradeZone in tradeZones)
        {
            tradeZonesIds.Add(tradeZone.id, tradeZone);
        }

        foreach (Region region in regions)
        {
            region.LoadFromSave();
            // Calc average fertility
            region.CalcAverages();
            // Checks habitability
            region.CheckHabitability();
            if (region.habitable)
            {
                habitableRegions.Add(region);
            }
            // Calc max populaiton
            region.CalcSocialClassRequirements();
        }
        BorderingRegions();

        pops.ForEach(r => r.LoadFromSave());
        //wars.ForEach(r => r.LoadFromSave());
        states.ForEach(r => r.LoadFromSave());
        //GD.Print(tradeZones.Values);
        tradeZones.ForEach(r => r.LoadFromSave());
        cultures.ForEach(r => r.LoadPopObjectFromSave());   
    }
    public void InitTerrainTiles()
    {
        tiles = new Tile[terrainSize.X, terrainSize.Y];

        for (int x = 0; x < terrainSize.X; x++)
        {
            for (int y = 0; y < terrainSize.Y; y++)
            {
                Tile newTile = new Tile();
                tiles[x, y] = newTile;
                
                //GD.Print(worldGenerator);
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
    }
    public void CreateRegions()
    {
        //GD.Print(regions.Count);
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                // Creates a region
                Region newRegion = new Region()
                {
                    id = getID()
                };

                newRegion.tiles = new Tile[tilesPerRegion, tilesPerRegion];
                newRegion.biomes = new Biome[tilesPerRegion, tilesPerRegion];

                newRegion.pos = new Vector2I(x, y);
                regions.Add(newRegion);
                regionIds.Add(newRegion.id, newRegion);
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
                newRegion.CalcSocialClassRequirements();
            }
        }
    }
    
    public void OnWorldgenFinished()
    {
        TradeZone.simManager = this;
        PopObject.simManager = this;
        PopObject.timeManager = timeManager;
        Army.simManager = this;
        Pop.simManager = this;
        War.simManager = this;
        Character.sim = this;
        terrainSize = worldGenerator.WorldSize;
        worldSize = terrainSize / tilesPerRegion;

        if (simLoadedFromSave)
        {
            RebuildAfterSave();
            BorderingRegions();
        }
        else
        {
            InitTerrainTiles();
            CreateRegions();
            BorderingRegions();
            InitPops();
        }
        node.InvokeEvent();
    }
    #endregion

    void BorderingRegions()
    {
        foreach (Region region in regions)
        {
            if (region == null)
            {
                GD.PushError("Something is wrong");
            }
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
                    if (r.habitable || region.habitable && !paintedRegions.Contains(region))
                    {
                        if (region == null)
                        {
                            GD.PushError("Something is wrong");
                        }
                        else
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
        }
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

                CreatePop((long)(startingPopulation * 0.25f), (long)(startingPopulation * 0.75f), region, new Tech(), culture, SocialClass.FARMER);
            }
        }
    }
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
                pop.politicalPower = pop.CalculatePoliticalPower();
                pop.EconomyUpdate();
                pop.GrowPop();
                pop.UpdateHappiness();
                pop.UpdateLoyalty();
                if (pop.batchId == timeManager.GetMonth(timeManager.ticks))
                {
                    pop.TechnologyUpdate();
                    //pop.SocialClassTransitions();
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
                region.hasBaseTradeWeight = false;
                region.hasTradeWeight = false;
                region.tradeIncome = 0f;
                region.taxIncome = 0f;
                region.linkUpdateCountdown--;
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
                    highestPopulation = (long)Mathf.Max(highestPopulation, region.population);
                    region.CalcSocialClassRequirements();
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
                    if (region.occupier != null && !region.owner.enemyIds.Contains(region.occupier.id))
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
        foreach (State state in states.ToArray())
        {
            if (state.regions.Count < 1)
            {
                DeleteState(state);
                continue;
            }
            if (state.StateCollapse())
            {
                continue;
            }
            if (state.rulingPop != null)
            {
                state.tech = state.rulingPop.tech;
            }
            state.GetRealmBorders();
            state.Capitualate();
        }
        foreach (State state in states)
        {
            if (state.rulingPop != null)
            {
                state.maxSize = 6 + state.rulingPop.tech.societyLevel;
            }

            state.age += TimeManager.ticksPerMonth;
            try
            {
                if (state.leaderId == null)
                {
                    state.SuccessionUpdate();
                }
                state.UpdateStability();
                if (state.sovereignty != Sovereignty.INDEPENDENT)
                {
                    state.timeAsVassal += TimeManager.ticksPerMonth;
                    state.UpdateLoyalty();
                }


                state.UpdateCapital();

                
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
            } catch (Exception e)
            {
                GD.PushError(e);
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
            war.age += TimeManager.ticksPerMonth;
        }
    }
    #endregion
    #region Character Update
    public void UpdateCharacters()
    {
        try
        {
            foreach (Character character in characters.ToArray())
            {
                // Dead character stuff
                if (character.dead)
                {
                    DeleteCharacter(character);
                    continue;
                }

                character.age += TimeManager.ticksPerMonth;
                // Character Aliveness

                // Character Aging
                // Calculated yearly, decreases character health as they age
                if (timeManager.GetMonth(character.birthTick) == timeManager.GetMonth())
                {
                    character.CharacterAging();
                }
                // Character Death
                // Calculated monthly, characters have a chance to die every month if their health is below a certain threshold
                float deathChance = Mathf.Lerp(0.01f, 0.25f, (float)character.health / Character.dieHealthThreshold);
                if (character.health <= Character.dieHealthThreshold && rng.NextSingle() < deathChance)
                {
                    character.Die();
                }
            }            
        } catch (Exception e)
        {
            GD.PushError(e);
        }

    }
    #endregion
    #region SimTick
    public void SimMonth()
    {
        ulong stepTime = Time.GetTicksMsec();
        ulong startTime = Time.GetTicksMsec();
        UpdatePops();
        totalPopsTime = Time.GetTicksMsec() - startTime;
        startTime = Time.GetTicksMsec();
        UpdateRegions();
        totalRegionTime = Time.GetTicksMsec() - startTime;
        startTime = Time.GetTicksMsec();
        UpdateStates();
        totalStateTime = Time.GetTicksMsec() - startTime;
        startTime = Time.GetTicksMsec();
        UpdateCharacters();
        UpdateCultures();
        UpdateWars();
        totalMiscTime = Time.GetTicksMsec() - startTime;
        totalStepTime = Time.GetTicksMsec() - stepTime;
    }
    public void SimYear()
    {

    }
    #endregion

    #region Creation
    public Region GetRegion(ulong? id)
    {
        try
        {
            return regionIds[(ulong)id];
        }
        catch
        {
            //GD.PushWarning(e);
            return null;
        }
    }
    public War GetWar(ulong? id)
    {
        try
        {
            return warIds[(ulong)id];
        }
        catch
        {
            //GD.PushWarning(e);
            return null;
        }        
    }
    #region Pops Creation
    public Pop CreatePop(long workforce, long dependents, Region region, Tech tech, Culture culture, SocialClass profession = SocialClass.FARMER)
    {
        currentBatch++;
        if (currentBatch > 12)
        {
            currentBatch = 2;
        }
        Pop pop = new Pop()
        {
            id = getID(),
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
        try
        {
            if (pop.region.owner != null && pop.region.owner.rulingPop == pop)
            {
                lock (pop.region.owner)
                {
                    pop.region.owner.rulingPop = null;
                }
            }
        }
        catch (Exception e)
        {
            GD.Print(e);
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
            id = getID(),
            name = "Culture",
            color = new Color(r, g, b),
            tickFounded = timeManager.ticks
        };

        cultures.Add(culture);
        cultureIds.Add(culture.id, culture);
        return culture;
    }
    public Culture GetCulture(ulong? id)
    {
        try {
            return cultureIds[(ulong)id];
        } catch {
            //GD.PushWarning(e);
            return null;
        }        
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
                id = getID(),
                name = NameGenerator.GenerateNationName(),
                color = new Color(r, g, b),
                capital = region,
                tickFounded = timeManager.ticks
            };
            state.AddRegion(region);
            states.Add(state);
            statesIds.Add(state.id, state);
        }
    }
    public void DeleteState(State state)
    {
        if (mapManager.selectedMetaObj == state)
        {
            mapManager.selectedMetaObj = null;
            mapManager.UpdateRegionColors(regions);
        }

        foreach (War war in state.wars.Keys)
        {
            war.RemoveParticipant(state.id);
        }
        foreach (State vassal in state.vassals)
        {
            state.RemoveVassal(vassal);
        }
        foreach (Region region in state.regions.ToArray())
        {
            state.RemoveRegion(region);
        }
        foreach (ulong characterId in state.characterIds.ToArray())
        {
            charactersIds[characterId].LeaveState();
        }
        objectDeleted.Invoke(state.id);
        states.Remove(state);
        statesIds.Remove(state.id);
    }
    public State GetState(ulong? id)
    {
        try {
            return statesIds[(ulong)id];
        } catch {
            //GD.PushWarning(e);
            return null;
        }
    }
    #endregion
    #region Characters Creation
    public Character GetCharacter(ulong? id)
    {
        if (id == null)
        {
            return null;
        }
        else
        {
            if (charactersIds.ContainsKey((ulong)id))
            {
                return charactersIds[(ulong)id];
            }
            return null;
        }
    }
    public Character GetCharacter(ulong id)
    {
        if (charactersIds.ContainsKey(id))
        {
            return charactersIds[id];
        }
        return null;
    }
    public Character CreateCharacter(string firstName, string lastName, uint age, State state, CharacterRole role)
    {
        Character character = new Character()
        {
            // Gives character id
            id = getID(),

            // Names character
            firstName = firstName,
            lastName = lastName,

            age = age,
            birthTick = timeManager.ticks - age,

            // Randomizes Character Personality
            charisma = rng.Next(0, 101),
            intellect = rng.Next(0, 101),
            greed = rng.Next(0, 101),
            ambition = rng.Next(0, 101),
            empathy = rng.Next(0, 101),
            boldness = rng.Next(0, 101),
            temperment = rng.Next(0, 101),
            sociability = rng.Next(0, 101),
        };
        // Adds character to state and gives it role
        character.JoinState(state.id);
        character.SetRole(role);

        // Documents character
        characters.Add(character);
        charactersIds.Add(character.id, character);
        return character;
    }
    public void DeleteCharacter(Character character)
    {

        character.LeaveState();
        foreach (ulong charId in character.childIds)
        {
            Character child = charactersIds[charId];
            child.parentId = null;
        }
        if (character.parentId != null)
        {
            Character parent = charactersIds[(ulong)character.parentId];
            parent.childIds.Remove(character.id);
        }
        objectDeleted.Invoke(character.id);
        characters.Remove(character);
        charactersIds.Remove(character.id);             
    }
    #endregion
    #region Diplomacy Creation
    #endregion
    #endregion

    public ulong getID()
    {
        currentID++;
        if (currentID == ulong.MaxValue)
        {
            currentID = 1;
        }
        return currentID;
    }
}