using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MessagePack;
using MessagePack.Resolvers;
using FileAccess = Godot.FileAccess;

[MessagePackObject(keyAsPropertyName: true)]
public class SimManager
{
    // Exported 
    [Export] [IgnoreMember] public TileMapLayer reliefs;

    [Export][IgnoreMember] public TimeManager timeManager;
    
    // Not Exported
    [IgnoreMember] public SimNodeManager node;
    [IgnoreMember] public Node2D terrainMap;
    public ObjectManager objectManager = new ObjectManager();
    public uint tick;

    public Tile[,] tiles;
    [IgnoreMember] public List<Region> habitableRegions = new List<Region>();
    [IgnoreMember] public List<Region> paintedRegions = new List<Region>();
    [IgnoreMember] public Vector2I terrainSize;
    [IgnoreMember] public static Vector2I worldSize;
    [IgnoreMember] public WorldGenerator worldGenerator;
    [IgnoreMember] public MapManager mapManager;

    // Population
    
    public long worldPopulation { get; set; } = 0;
    public long highestPopulation { get; set; } = 0;
    public uint populatedRegions;
    public float maxWealth = 0;
    public float maxTradeWeight = 0;

    // Lists
    // Saved Data
    [IgnoreMember] public List<Region> regions { get; set; } = new List<Region>();
    [IgnoreMember] public Dictionary<ulong, Region> regionIds { get; set; } = new Dictionary<ulong, Region>();
    //[IgnoreMember] public List<Pop> pops { get; set; } = new List<Pop>();
    [IgnoreMember] public Dictionary<ulong, Pop> popsIds { get; set; } = new Dictionary<ulong, Pop>();
    [IgnoreMember] public Dictionary<ulong, Culture> cultureIds { get; set; } = new Dictionary<ulong, Culture>();
    //[IgnoreMember] public List<State> states { get; set; } = new List<State>();
    [IgnoreMember] public Dictionary<ulong, State> statesIds { get; set; } = new Dictionary<ulong, State>();
    [IgnoreMember] public List<ulong> deletedStateIds = new List<ulong>();
    [IgnoreMember] public List<TradeZone> tradeZones { get; set; } = new List<TradeZone>();
    [IgnoreMember] public Dictionary<ulong, TradeZone> tradeZonesIds { get; set; } = new Dictionary<ulong, TradeZone>();
    [IgnoreMember] public List<Character> characters { get; set; } = new List<Character>();
    [IgnoreMember] public Dictionary<ulong, Character> charactersIds { get; set; } = new Dictionary<ulong, Character>();
    [IgnoreMember] public Dictionary<ulong, Alliance> allianceIds { get; set; } = new Dictionary<ulong, Alliance>();
    [IgnoreMember] public List<War> wars { get; set; } = new List<War>();
    [IgnoreMember] public Dictionary<ulong, War> warIds { get; set; } = new Dictionary<ulong, War>();
    [IgnoreMember] public Dictionary<ulong, HistoricalEvent> historicalEventIds = new Dictionary<ulong, HistoricalEvent>();
    [IgnoreMember] public Dictionary<ulong, Settlement> settlementIds = new Dictionary<ulong, Settlement>();

    // Misc
    public uint currentBatch = 2;
    public ulong currentId = 0;
    [IgnoreMember] public bool simLoadedFromSave = false;

    [IgnoreMember] public Random rng = new Random();

    // Events
    public delegate void ObjectDeletedEvent(ulong id);
    [IgnoreMember] public ObjectDeletedEvent objectDeleted;
    
    // Debug info
    [IgnoreMember] public double totalStepTime;

    // Constants
    [IgnoreMember] public const int tilesPerRegion = 4; 
    [IgnoreMember] public const int regionGlobalWidth = 16;
    [IgnoreMember] public Dictionary<string, double> stepPerformanceInfo = new(){
        {"Pops", 0},
        {"Regions", 0},
        {"States", 0},
        {"Misc", 0}
    };
        
    [IgnoreMember] public Dictionary<string, double> popsPerformanceInfo = [];
    [IgnoreMember] public Dictionary<string, double> regionPerformanceInfo = [];
    [IgnoreMember] public Vector2 terrainMapScale;
    public Vector2I GlobalToRegionPos(Vector2 pos)
    {
        return (Vector2I)(pos / (terrainMapScale * regionGlobalWidth)) / tilesPerRegion;
    }

    public Vector2 RegionToGlobalPos(Vector2 regionPos)
    {
        return tilesPerRegion * (regionPos * (terrainMapScale * regionGlobalWidth));
    }
    public void SaveSimToFile(string path)
    {
        regionIds.Values.ToList().ForEach(r => r.PrepareForSave());
        statesIds.Values.ToList().ForEach(r => r.PrepareForSave());
        cultureIds.Values.ToList().ForEach(r => r.PreparePopObjectForSave());
        tick = timeManager.ticks;

        var resolver = CompositeResolver.Create(
            [new Vector2IFormatter(), new ColorFormatter()],
            [StandardResolver.Instance]
        );

        var options = MessagePackSerializerOptions.Standard.WithResolver(resolver).WithCompression(MessagePackCompression.Lz4BlockArray);
        FileAccess simSave = FileAccess.Open($"{path}/sim_data.pxsave", FileAccess.ModeFlags.Write);
        simSave.StoreBuffer(MessagePackSerializer.Serialize(this, options));
        FileAccess regionsSave = FileAccess.Open($"{path}/regions.pxsave", FileAccess.ModeFlags.Write);
        regionsSave.StoreBuffer(MessagePackSerializer.Serialize(regionIds, options));
        FileAccess popsSave = FileAccess.Open($"{path}/pops.pxsave", FileAccess.ModeFlags.Write);
        popsSave.StoreBuffer(MessagePackSerializer.Serialize(popsIds, options));
        FileAccess statesSave = FileAccess.Open($"{path}/states.pxsave", FileAccess.ModeFlags.Write);
        statesSave.StoreBuffer(MessagePackSerializer.Serialize(statesIds, options));
        FileAccess cultureSave = FileAccess.Open($"{path}/cultures.pxsave", FileAccess.ModeFlags.Write);
        cultureSave.StoreBuffer(MessagePackSerializer.Serialize(cultureIds, options));
        FileAccess tradeSave = FileAccess.Open($"{path}/trade_zones.pxsave", FileAccess.ModeFlags.Write);
        tradeSave.StoreBuffer(MessagePackSerializer.Serialize(tradeZonesIds, options));
        FileAccess charactersSave = FileAccess.Open($"{path}/characters.pxsave", FileAccess.ModeFlags.Write);
        charactersSave.StoreBuffer(MessagePackSerializer.Serialize(charactersIds, options));
        FileAccess warsSave = FileAccess.Open($"{path}/wars.pxsave", FileAccess.ModeFlags.Write);
        warsSave.StoreBuffer(MessagePackSerializer.Serialize(warIds, options));
        FileAccess eventsSave = FileAccess.Open($"{path}/events.pxsave", FileAccess.ModeFlags.Write);
        eventsSave.StoreBuffer(MessagePackSerializer.Serialize(historicalEventIds, options));
        FileAccess settlementsSave = FileAccess.Open($"{path}/settlements.pxsave", FileAccess.ModeFlags.Write);
        settlementsSave.StoreBuffer(MessagePackSerializer.Serialize(settlementIds, options));
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

        // Loads Sim Stuffs
        sim.regionIds = MessagePackSerializer.Deserialize<Dictionary<ulong, Region>>(FileAccess.GetFileAsBytes($"{path}/regions.pxsave"), options);
        sim.popsIds = MessagePackSerializer.Deserialize<Dictionary<ulong, Pop>>(FileAccess.GetFileAsBytes($"{path}/pops.pxsave"), options);
        sim.statesIds = MessagePackSerializer.Deserialize<Dictionary<ulong, State>>(FileAccess.GetFileAsBytes($"{path}/states.pxsave"), options);
        sim.cultureIds = MessagePackSerializer.Deserialize<Dictionary<ulong, Culture>>(FileAccess.GetFileAsBytes($"{path}/cultures.pxsave"), options);
        sim.tradeZonesIds = MessagePackSerializer.Deserialize<Dictionary<ulong, TradeZone>>(FileAccess.GetFileAsBytes($"{path}/trade_zones.pxsave"), options);
        sim.charactersIds = MessagePackSerializer.Deserialize<Dictionary<ulong, Character>>(FileAccess.GetFileAsBytes($"{path}/characters.pxsave"), options);
        sim.warIds = MessagePackSerializer.Deserialize<Dictionary<ulong, War>>(FileAccess.GetFileAsBytes($"{path}/wars.pxsave"), options);
        sim.historicalEventIds = MessagePackSerializer.Deserialize<Dictionary<ulong, HistoricalEvent>>(FileAccess.GetFileAsBytes($"{path}/events.pxsave"), options);
        sim.settlementIds = MessagePackSerializer.Deserialize<Dictionary<ulong, Settlement>>(FileAccess.GetFileAsBytes($"{path}/settlements.pxsave"), options);
        sim.simLoadedFromSave = true;
        return sim;
    }
    public void RebuildAfterSave()
    {
        AssignSimManager();
        timeManager.ticks = tick;
        regions = [.. regionIds.Values];
        wars = [.. warIds.Values];
        tradeZones = [.. tradeZonesIds.Values];
        characters = [.. charactersIds.Values];
        
        foreach (var pair in regionIds)
        {
            Region region = pair.Value;
            region.LoadFromSave();
            // Adds tiles and biomes
            region.tiles = new Tile[tilesPerRegion, tilesPerRegion];
            region.biomes = new Biome[tilesPerRegion, tilesPerRegion];
            for (int tx = 0; tx < tilesPerRegion; tx++)
            {
                for (int ty = 0; ty < tilesPerRegion; ty++)
                {
                    // Adds subregion to tile
                    Tile tile = tiles[region.pos.X * tilesPerRegion + tx, region.pos.Y * tilesPerRegion + ty];
                    region.tiles[tx, ty] = tile;
                    // Adds biomes to tile
                    region.biomes[tx, ty] = tile.biome;
                }
            }
            // Calc average fertility
            region.CalcAverages();
            // Checks habitability
            region.CheckHabitability();
            if (region.habitable)
            {
                habitableRegions.Add(region);
            }
        }
        BorderingRegions();

        //pops.ForEach(r => r.LoadFromSave());
        //wars.ForEach(r => r.LoadFromSave());
        statesIds.Values.ToList().ForEach(r => r.LoadFromSave());
        cultureIds.Values.ToList().ForEach(r => r.LoadPopObjectFromSave());   
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
                        newTile.navigability *= 0.1f;
                        newTile.arability *= 0.25f;
                        newTile.survivalbility *= 0.8f;
                        newTile.terrainType = TerrainType.MOUNTAINS;
                    }
                    else if (worldGenerator.HeightMap[x, y] > WorldGenerator.HillThreshold)
                    {
                        newTile.navigability *= 0.25f;
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
                Region newRegion = objectManager.CreateRegion(x, y);
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
            }
        }
    }
    void AssignSimManager()
    {
        terrainMapScale = terrainMap.Scale;
        HistoricalEvent.timeManager = timeManager;
        StateDiplomacyManager.objectManager = objectManager;
        StateVassalManager.objectManager = objectManager;
        ObjectManager.simManager = this;
        ObjectManager.timeManager = timeManager;
        MapManager.objectManager = objectManager;
        Battle.objectManager = objectManager;

        NamedObject.simManager = this;
        NamedObject.objectManager = objectManager;
        PopObject.timeManager = timeManager;

        TradeZone.simManager = this;
        TradeZone.objectManager = objectManager;

        Pop.objectManager = objectManager;
        Character.sim = this;
        IndexTab.sim = this;
    }
    public void OnWorldgenFinished()
    {
        AssignSimManager();
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
    

    void BorderingRegions()
    {
        foreach (var pair in regionIds)
        {
            Region region = pair.Value;
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
                    Direction direction = Utility.GetDirectionFromVector(new Vector2I(dx, dy));
                    Region r = objectManager.GetRegion(region.pos.X + dx, region.pos.Y + dy);
                    region.borderingRegionIds.Add(direction, r.id);

                    i++;
                    if (r.habitable)
                    {
                        habitableBorderCount++;
                    }
                    if (r.habitable || region.habitable && !paintedRegions.Contains(region))
                    {
                        paintedRegions.Add(region);
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
                Culture culture = objectManager.CreateCulture();

                objectManager.CreatePop((long)(startingPopulation * 0.25f), (long)(startingPopulation * 0.75f), region, new Tech(), culture, SocialClass.FARMER);
            }
        }
    }
    // Updating pops
    public void UpdatePops()
    {
        Stopwatch totalTime = Stopwatch.StartNew();

        popsPerformanceInfo["Destroy Time"] = 0; 
        popsPerformanceInfo["Economy Time"] = 0; 
        popsPerformanceInfo["Growth Time"] = 0; 
        popsPerformanceInfo["Migration Time"] = 0; 
        popsPerformanceInfo["Parallel Time"] = 0;

        Stopwatch stopwatch = Stopwatch.StartNew();
        foreach (var pair in popsIds.ToArray())
        {
            Pop pop = pair.Value;
            // Deletions
            if (pop.region == null || pop.population <= Pop.ToNativePopulation(1))
            {
                objectManager.DestroyPop(pop);
            }
        }
        popsPerformanceInfo["Destroy Time"] += stopwatch.Elapsed.TotalMilliseconds;
        
        stopwatch.Restart();

        var partitioner = Partitioner.Create(popsIds.ToArray());
        Parallel.ForEach(partitioner, (popsPair) =>
        {
            Pop pop = popsPair.Value;
            pop.GrowPop();
            pop.politicalPower = pop.CalculatePoliticalPower();          
            if (pop.batchId == timeManager.GetMonth(timeManager.ticks))
            {
                pop.TechnologyUpdate();
                pop.SocialClassTransitions();
                pop.Migrate();
            }            
        });
        popsPerformanceInfo["Parallel Time"] += stopwatch.Elapsed.TotalMilliseconds;
    }
    public void UpdateRegions()
    {
        regionPerformanceInfo["Parallel Time"] = 0;
        regionPerformanceInfo["Pop Merging Time"] = 0;
        regionPerformanceInfo["Economy Time"] = 0;
        regionPerformanceInfo["State Formation Time"] = 0;
        regionPerformanceInfo["Conquest Time"] = 0;
        regionPerformanceInfo["Border Time"] = 0;
        regionPerformanceInfo["Trade Weight Time"] = 0;
        uint countedPoppedRegions = 0;

        long worldPop = 0;
        //int regionBatches = 8;
        ulong rStartTime = Time.GetTicksMsec();
        try
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var partitioner = Partitioner.Create(habitableRegions);
            Parallel.ForEach(partitioner, (region) =>
            {
                region.UpdateMaxPopulation();
                region.UpdateWealth();
                if (region.pops.Count > 0)
                {
                    region.MergePops();
                    region.DistributeWealth();
                    region.settlement.UpdateSlots();
                    region.settlement.UpdateEmployment();
                }
                region.hasBaseTradeWeight = false;
                region.hasTradeWeight = false;
                region.tradeIncome = 0f;
                region.taxIncome = 0f;
                region.linkUpdateCountdown--;
            });
            regionPerformanceInfo["Parallel Time"] = stopwatch.Elapsed.TotalMilliseconds;
            stopwatch.Restart();

            foreach (Region region in habitableRegions)
            {
                if (region.pops.Count <= 0)
                {
                    region.linkUpdateCountdown = 0;
                    continue;
                }
                
                
                // Economy
                region.CalcBaseWealth();
                if (region.linkUpdateCountdown < 1 || region.tradeLink == null)
                {
                    region.linkUpdateCountdown = 12;
                    region.LinkTrade();
                }
                regionPerformanceInfo["Economy Time"] += stopwatch.Elapsed.TotalMilliseconds;
                stopwatch.Restart();

                // States
                region.RandomStateFormation();
                region.UpdateOccupation();
                regionPerformanceInfo["State Formation Time"] += stopwatch.Elapsed.TotalMilliseconds;
                stopwatch.Restart();

                if (region.owner != null)
                {
                    region.StateBordering();
                    if (region.frontier && region.occupier == null)
                    {
                        region.NeutralConquest();
                    }
                    region.MilitaryConquest();
                    
                }  
                regionPerformanceInfo["Conquest Time"] += stopwatch.Elapsed.TotalMilliseconds;  
                // Increments
                lock (this)
                {
                    countedPoppedRegions += 1;
                    worldPop += region.population;
                }
            }

            stopwatch.Restart();
            foreach (Region region in habitableRegions)
            {
                highestPopulation = (long)Mathf.Max(highestPopulation, region.population);
                if (region.owner != null)
                {
                    if (region.occupier != null && !region.owner.diplomacy.enemyIds.Contains(region.occupier.id))
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
            regionPerformanceInfo["Trade Weight Time"] = stopwatch.Elapsed.TotalMilliseconds;
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }
        populatedRegions = countedPoppedRegions;
        worldPopulation = worldPop;
    }
    public void UpdateStates()
    {
        foreach (var pair in statesIds.ToArray())
        {
            State state = pair.Value;
            if (state.regions.Count < 1 || state.StateCollapse() || state.rulingPop == null)
            {
                objectManager.DeleteState(state);
                continue;
            }
            if (state.rulingPop != null)
            {
                state.tech = state.rulingPop.tech;
            }
            //state.GetRealmBorders();
            state.Capitualate();
        }
        foreach (var pair in statesIds.ToArray())
        {
            State state = pair.Value;
            if (state.rulingPop != null)
            {
                state.maxSize = 6 + state.rulingPop.tech.societyLevel;
            }

            try
            {
                if (state.leaderId == null)
                {
                    state.SuccessionUpdate();
                }
                state.UpdateStability();
                if (state.vassalManager.sovereignty != Sovereignty.INDEPENDENT)
                {
                    if (state.vassalManager.GetLiege() == null)
                    {
                        GD.Print(state.vassalManager.liegeId);
                    }
                    state.timeAsVassal += TimeManager.ticksPerMonth;
                    state.UpdateLoyalty();
                    state.diplomacy.JoinLiegeWars();
                }   

                state.vassalManager.UpdateRealm();
                state.UpdateCapital();
                
                state.diplomacy.UpdateEnemies();
                state.diplomacy.RelationsUpdate();
                state.diplomacy.UpdateDiplomacy();

                state.diplomacy.EndWars();
                state.diplomacy.StartWars();     
            } catch (Exception e)
            {
                GD.PushError(e);
            }
        }
        var partitioner = Partitioner.Create(statesIds.Values);
        Parallel.ForEach(partitioner, (state) =>
        {
            state.CountPopulation();
            state.Recruitment();
            state.UpdateDisplayColor();
            StateNamer.UpdateStateNames(state);
        });
    }
    public void UpdateCultures()
    {
        foreach (var pair in cultureIds)
        {
            Culture culture = pair.Value;

            if (culture.dead)
            {
                objectManager.DeleteCulture(culture);
                continue;
            }

            if (culture.pops.Count < 1)
            {
                culture.Die();
                continue;
            }
        }       
    }
    public void UpdateAlliances()
    {
        foreach (var pair in allianceIds)
        {
            Alliance alliance = pair.Value;

            if (alliance.dead)
            {
                objectManager.DeleteAlliance(alliance);
                continue;
            }

            if (alliance.leadStateId == null || alliance.memberStateIds.Count < 2)
            {
                alliance.Die();
                continue;
            }
        }
    }
    public void UpdateCharacters()
    {
        try
        {
            foreach (Character character in characters.ToArray())
            {
                // Dead character stuff
                if (character.dead)
                {
                    // Deletes characters after 200 years if they are dead
                    if (timeManager.GetYear(character.GetAge()) > 300)
                    {
                        objectManager.DeleteCharacter(character);
                    }
                    
                    continue;
                }
                // Character Aliveness

                // Character Aging
                // Calculated yearly, decreases character health as they age
                if (timeManager.GetMonth(character.tickCreated) == timeManager.GetMonth())
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
    public void SimMonth()
    {
        Stopwatch stepStopwatch = Stopwatch.StartNew();
        try
        {
            Stopwatch processStopwatch = Stopwatch.StartNew();
            UpdatePops();
            stepPerformanceInfo["Pops"] = processStopwatch.Elapsed.TotalMilliseconds;
            processStopwatch.Restart();

            UpdateRegions();
            stepPerformanceInfo["Regions"] = processStopwatch.Elapsed.TotalMilliseconds;
            processStopwatch.Restart();

            UpdateStates();
            stepPerformanceInfo["States"] = processStopwatch.Elapsed.TotalMilliseconds;
            processStopwatch.Restart();

            UpdateCharacters();
            UpdateCultures();
            UpdateAlliances();
            stepPerformanceInfo["Misc"] = processStopwatch.Elapsed.TotalMilliseconds;
            processStopwatch.Restart();
          
        } catch  (Exception e)
        {
            GD.PushError(e);
        }
        totalStepTime = stepStopwatch.Elapsed.TotalMilliseconds;
    }
    public void SimYear()
    {

    }
    
}