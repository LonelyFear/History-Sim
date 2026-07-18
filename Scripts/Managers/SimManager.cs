using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MessagePack;
using MessagePack.Resolvers;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.States.Diplomacy;
using PixelHistory.Objects.States.AI;
using FileAccess = Godot.FileAccess;
using PixelHistory.Objects.Wars;

public enum RegionStyle
{
    Square,
    Voronoi
}

[MessagePackObject(keyAsPropertyName: true)]
public class SimManager
{
    // Exported 
    [Export] [IgnoreMember] public TileMapLayer reliefs;

    [Export][IgnoreMember] public TimeManager timeManager;
    
    // Not Exported
    [IgnoreMember] public SimManagerHolder simHolder;
    [IgnoreMember] public Node2D terrainMap;

    // Config
    public string worldName = "";
    public uint tick;
    public RegionStyle regionStyle = RegionStyle.Square;
    public bool useNewEconomy = false;

    // Population
    public long worldPopulation { get; set; } = 0;
    public long highestPopulation { get; set; } = 0;
    public uint populatedRegions;
    public float maxWealth = 0;
    public float maxTradeWeight = 0;
    public Tech highestTech;
    public Tech averageTech; 
       
    // Region Lists
    [IgnoreMember] public List<Region> habitableRegions = [];
    [IgnoreMember] public List<Region> paintedRegions = [];
    public Tile[,] tiles; 

    // Map
    [IgnoreMember] public static Vector2I worldSize;
    [IgnoreMember] public WorldGenerator worldGenerator;
    [IgnoreMember] public MapManager mapManager;



    // Lists
    // Saved Data
       
    [IgnoreMember] public Dictionary<ulong, Region> regionIds { get; set; } = [];
    [IgnoreMember] public Dictionary<ulong, Pop> popsIds { get; set; } = [];
    [IgnoreMember] public Dictionary<ulong, Culture> cultureIds { get; set; } = [];
    [IgnoreMember] public Dictionary<ulong, State> statesIds { get; set; } = [];
    [IgnoreMember] public List<ulong> deletedStateIds = [];
    [IgnoreMember] public Dictionary<ulong, TradeZone> tradeZoneIds { get; set; } = [];
    [IgnoreMember] public ConcurrentDictionary<ulong, Character> characterIds { get; set; } = [];
    [IgnoreMember] public Dictionary<ulong, Alliance> allianceIds { get; set; } = [];
    [IgnoreMember] public ConcurrentDictionary<ulong, War> warIds { get; set; } = [];
    [IgnoreMember] public Dictionary<ulong, Ocean> oceanIds { get; set; } = [];
    [IgnoreMember] public Dictionary<ulong, DiplomaticRelations> relationIds { get; set; } = [];

    [IgnoreMember] public ConcurrentDictionary<ulong, HistoricalEvent> historicalEventIds = [];

    // Misc
    public uint currentBatch = 2;
    public ulong currentId = 20;
    [IgnoreMember] public bool simLoadedFromSave = false;

    [IgnoreMember] public Random rng = new Random();

    // Events
    public delegate void ObjectDeletedEvent(ulong id);
    [IgnoreMember] public ObjectDeletedEvent objectDeleted;
    
    // Debug info
    [IgnoreMember] public double totalStepTime;

    // Constants
    [IgnoreMember] public const int regionGlobalWidth = 16;

    // Performance
    [IgnoreMember] public Dictionary<string, double> stepPerformanceInfo = new(){
        {"Pops", 0},
        {"Regions", 0},
        {"States", 0},
        {"Misc", 0}
    };
        
    [IgnoreMember] public Dictionary<string, double> popsPerformanceInfo = [];
    [IgnoreMember] public Dictionary<string, double> regionPerformanceInfo = [];
    [IgnoreMember] public Dictionary<string, double> statePerformanceInfo = [];
    [IgnoreMember] public Dictionary<string, double> miscPerformanceInfo = [];
    [IgnoreMember] public Vector2 terrainMapScale;

    public Vector2I GlobalToTilePos(Vector2 pos)
    {
        return (Vector2I)(pos / (terrainMap.Scale * regionGlobalWidth));
    }

    public Vector2 TileToGlobalPos(Vector2 regionPos)
    {
        return regionPos * (terrainMap.Scale * regionGlobalWidth);
    }
    public void SaveSimToFile(string path)
    {
        regionIds.Values.ToList().ForEach(r => r.PrepareForSave());
        statesIds.Values.ToList().ForEach(r => r.PrepareForSave());
        cultureIds.Values.ToList().ForEach(r => r.PrepareForSave());
        allianceIds.Values.ToList().ForEach(r => r.PrepareForSave());

        tick = timeManager.ticks;

        var resolver = CompositeResolver.Create(
            [new Vector2IFormatter(), new ColorFormatter()],
            [StandardResolverAllowPrivate.Instance]
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

        FileAccess alliancesSave = FileAccess.Open($"{path}/alliances.pxsave", FileAccess.ModeFlags.Write);
        alliancesSave.StoreBuffer(MessagePackSerializer.Serialize(allianceIds, options));

        FileAccess cultureSave = FileAccess.Open($"{path}/cultures.pxsave", FileAccess.ModeFlags.Write);
        cultureSave.StoreBuffer(MessagePackSerializer.Serialize(cultureIds, options));

        FileAccess tradeSave = FileAccess.Open($"{path}/tradeZones.pxsave", FileAccess.ModeFlags.Write);
        tradeSave.StoreBuffer(MessagePackSerializer.Serialize(tradeZoneIds, options));

        FileAccess charactersSave = FileAccess.Open($"{path}/characters.pxsave", FileAccess.ModeFlags.Write);
        charactersSave.StoreBuffer(MessagePackSerializer.Serialize(characterIds, options));

        FileAccess warsSave = FileAccess.Open($"{path}/wars.pxsave", FileAccess.ModeFlags.Write);
        warsSave.StoreBuffer(MessagePackSerializer.Serialize(warIds, options));

        FileAccess oceansSave = FileAccess.Open($"{path}/oceans.pxsave", FileAccess.ModeFlags.Write);
        oceansSave.StoreBuffer(MessagePackSerializer.Serialize(oceanIds, options));

        FileAccess eventsSave = FileAccess.Open($"{path}/events.pxsave", FileAccess.ModeFlags.Write);
        eventsSave.StoreBuffer(MessagePackSerializer.Serialize(historicalEventIds, options));

        FileAccess diplomacySave = FileAccess.Open($"{path}/diplomacy.pxsave", FileAccess.ModeFlags.Write);
        eventsSave.StoreBuffer(MessagePackSerializer.Serialize(relationIds, options));
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
            [StandardResolverAllowPrivate.Instance]
        );
        var options = MessagePackSerializerOptions.Standard.WithResolver(resolver).WithCompression(MessagePackCompression.Lz4BlockArray);
        //FileAccess save = FileAccess.Open($"{path}/sim_data.pxsave", FileAccess.ModeFlags.Read);
        SimManager sim = MessagePackSerializer.Deserialize<SimManager>(FileAccess.GetFileAsBytes($"{path}/sim_data.pxsave"), options);

        // Loads Sim Stuffs
        sim.regionIds = MessagePackSerializer.Deserialize<Dictionary<ulong, Region>>(FileAccess.GetFileAsBytes($"{path}/regions.pxsave"), options);
        sim.popsIds = MessagePackSerializer.Deserialize<Dictionary<ulong, Pop>>(FileAccess.GetFileAsBytes($"{path}/pops.pxsave"), options);
        sim.statesIds = MessagePackSerializer.Deserialize<Dictionary<ulong, State>>(FileAccess.GetFileAsBytes($"{path}/states.pxsave"), options);
        sim.allianceIds = MessagePackSerializer.Deserialize<Dictionary<ulong, Alliance>>(FileAccess.GetFileAsBytes($"{path}/alliances.pxsave"), options);
        sim.cultureIds = MessagePackSerializer.Deserialize<Dictionary<ulong, Culture>>(FileAccess.GetFileAsBytes($"{path}/cultures.pxsave"), options);
        sim.tradeZoneIds = MessagePackSerializer.Deserialize<Dictionary<ulong, TradeZone>>(FileAccess.GetFileAsBytes($"{path}/tradeZones.pxsave"), options);
        sim.characterIds = MessagePackSerializer.Deserialize<ConcurrentDictionary<ulong, Character>>(FileAccess.GetFileAsBytes($"{path}/characters.pxsave"), options);
        sim.warIds = MessagePackSerializer.Deserialize<ConcurrentDictionary<ulong, War>>(FileAccess.GetFileAsBytes($"{path}/wars.pxsave"), options);
        sim.oceanIds = MessagePackSerializer.Deserialize<Dictionary<ulong, Ocean>>(FileAccess.GetFileAsBytes($"{path}/oceans.pxsave"), options);
        sim.relationIds = MessagePackSerializer.Deserialize<Dictionary<ulong, DiplomaticRelations>>(FileAccess.GetFileAsBytes($"{path}/diplomacy.pxsave"), options);
        sim.historicalEventIds = MessagePackSerializer.Deserialize<ConcurrentDictionary<ulong, HistoricalEvent>>(FileAccess.GetFileAsBytes($"{path}/events.pxsave"), options);
        sim.simLoadedFromSave = true;
        return sim;
    }

    public void RebuildAfterSave()
    {
        AssignSimManager();
        timeManager.ticks = tick;
        
        regionIds.Values.ToList().ForEach(r =>
        {
            r.LoadFromSave();
            r.LoadStats();
        });
        BorderingRegions();

        statesIds.Values.ToList().ForEach(r => r.LoadFromSave());
        cultureIds.Values.ToList().ForEach(r => r.LoadFromSave());
        allianceIds.Values.ToList().ForEach(r => r.LoadFromSave());
    }
   public void InitTerrainTiles()
    {
        tiles = new Tile[worldSize.X, worldSize.Y];

        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                Tile newTile = new(worldGenerator.cells[x,y]);
                tiles[x, y] = newTile;
                newTile.pos = new Vector2I(x,y);
                
                // checks for coasts
                for (int dx = -1; dx < 2; dx++)
                {
                    for (int dy = -1; dy < 2; dy++)
                    {
                        if ((dx != 0 && dy != 0) || (dx == 0 && dy == 0) || newTile.coastal || newTile.IsWater())
                        {
                            continue;
                        }
                        //int nx = Mathf.PosMod(x + dx, worldSize.X);
                        //int ny = Mathf.PosMod(y + dy, worldSize.Y);
                        if (newTile.GetBiome().type == Biome.BiomeType.WATER)
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
    public void BorderingRegions()
    {
        foreach (var pair in regionIds)
        {
            Region region = pair.Value;
            region.GetBorderingRegions();

            foreach (Region r in region.borderingRegions)
            {
                if (r.habitable || region.habitable && !paintedRegions.Contains(region))
                {
                    paintedRegions.Add(region);
                }                
            }         
        }
    }
    void AssignSimManager()
    {
       ObjectManager.simManager = this;
       ObjectManager.timeManager = timeManager;
       ObjectManager.selectionManager = simHolder.selectionManager;

        terrainMapScale = terrainMap.Scale;

        HistoricalEvent.timeManager = timeManager;

       ObjectManager.simManager = this;
       ObjectManager.timeManager = timeManager;

        StateAIManager.simManager = this;
        
        StateAIManager.simManager = this;

        NamedObject.simManager = this;
        
        PopObject.timeManager = timeManager;

        Character.sim = this;

        BaseEncyclopediaTab.simManager = this;
    }

    public void OnWorldgenFinished()
    {
        AssignSimManager();
        worldSize = worldGenerator.WorldSize;

        if (simLoadedFromSave)
        {
            RebuildAfterSave();
            BorderingRegions();
            timeManager.ForceGameSpeed(TimeManager.GameSpeed.PAUSED);
        }
        else
        {
            InitTerrainTiles();

            RegionGenerator regionGen = new(this);
            regionGen.GenerateRegions();

            OceanGenerator oceanGenerator = new(this);
            oceanGenerator.GenerateOceans();
        }
        simHolder.InvokeEvent();

        StartSimulation();
    }

    void StartSimulation()
    {
        foreach (Region region in habitableRegions)
        {
            double nodeChance = 0.004;
            //GD.Print(region.Migrateable());
            if (rng.NextDouble() <= nodeChance && region.Migrateable())
            {
                long startingPopulation = rng.Next(600, 1200);
                
                Culture culture = ObjectManager.CreateCulture();
                ObjectManager.CreatePop((int)(startingPopulation * 0.25f), (int)(startingPopulation * 0.75f), region, new Tech(), culture, "farmer");
            }
        }
    }
    // Updating pops
    public void UpdatePops()
    {
        Dictionary<string, double> countedPerformanceInfo = new();
        Stopwatch totalTime = Stopwatch.StartNew();

        countedPerformanceInfo["Destroy Time"] = 0; 
        //countedPerformanceInfo["Economy Time"] = 0; 
        //countedPerformanceInfo["Growth Time"] = 0; 
        //countedPerformanceInfo["Migration Time"] = 0; 
        countedPerformanceInfo["Parallel Time"] = 0;

        Stopwatch stopwatch = Stopwatch.StartNew();
        Tech techAvg = new();
        foreach (var pair in popsIds.ToArray())
        {
            Pop pop = pair.Value;
            // Deletions
            if (pop.region == null || pop.population <= 1)
            {
                ObjectManager.DestroyPop(pop);
            }
        }
        countedPerformanceInfo["Destroy Time"] += stopwatch.Elapsed.TotalMilliseconds;
        
        stopwatch.Restart();

        var partitioner = Partitioner.Create(popsIds.ToArray());
        Parallel.ForEach(partitioner, (popsPair) =>
        {
            Pop pop = popsPair.Value;
            bool isInBatch = pop.batchId == timeManager.GetMonth(timeManager.ticks);
            pop.politicalPower = pop.CalculatePoliticalPower(); 
              
            if (isInBatch)
            {
                pop.GrowPop();
                pop.TechnologyUpdate();   
            } 
            if (isInBatch)
            {
                pop.Migrate();
            }   
            if (isInBatch) pop.GetDemands();
                      
            lock (this)
            {
                if (pop.tech.GetAdvancement() > highestTech.GetAdvancement())
                {
                    highestTech = pop.tech;
                }                
            }    
            lock (this)
            {
                techAvg = techAvg.AddTech(pop.tech); 
            }
        });

        averageTech.fIndustryLevel = techAvg.industryLevel / (float)popsIds.Count;
        averageTech.fMilitaryLevel = techAvg.militaryLevel / (float)popsIds.Count;
        averageTech.fScienceLevel = techAvg.scienceLevel / (float)popsIds.Count;
        averageTech.fSocietyLevel = techAvg.societyLevel / (float)popsIds.Count;

        countedPerformanceInfo["Parallel Time"] += stopwatch.Elapsed.TotalMilliseconds;
        popsPerformanceInfo = countedPerformanceInfo;
    }
    public void UpdateRegions()
    {
        Dictionary<string, double> countedPerformanceInfo = new();

        float newMaxWealth = 0;
        int newMaxTradeWeight = 0;

        countedPerformanceInfo["Parallel Time"] = 0;
        countedPerformanceInfo["Pop Merging Time"] = 0;
        countedPerformanceInfo["Trade Route Time"] = 0;
        countedPerformanceInfo["Economy Time"] = 0;
        countedPerformanceInfo["State Formation Time"] = 0;
        countedPerformanceInfo["Conquest Time"] = 0;
        countedPerformanceInfo["Border Time"] = 0;
        countedPerformanceInfo["Trade Weight Time"] = 0;
        uint countedPoppedRegions = 0;

        long worldPop = 0;
        //int regionBatches = 8;
        ulong rStartTime = Time.GetTicksMsec();

        try
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            foreach (Region region in habitableRegions)
            {
                region.UpdateWealth();
            }
            
            var partitioner = Partitioner.Create(habitableRegions);
            Parallel.ForEach(partitioner, (region) =>
            {
                region.conquered = false;
                if (region.pops.Count > 0)
                {
                    region.MergePops();
                    region.DistributeWealth();

                    if (region.owner != null)
                    {
                        region.StateBordering();
                    }
                }

                region.linkUpdateCountdown--;
                region.GetTradeWeight();

                // Tax Income
                region.GetTaxIncome();
                // Base Trade Income
                if (region.tradeLink == null) region.GetTradeIncome();

                if (useNewEconomy)
                {
                    region.UpdatePrimaryIndustries();
                    region.CalcProduction();
                    region.CalcDemand();
                    region.CalcSupply();
                    
                    region.economy.CalculatePrices();                    
                }

            });
            countedPerformanceInfo["Parallel Time"] = stopwatch.Elapsed.TotalMilliseconds;
            stopwatch.Restart(); 

            Parallel.ForEach(partitioner, (region) =>
            {
                region.GetRouteIncome();
            }); 
            countedPerformanceInfo["Trade Route Time"] += stopwatch.Elapsed.TotalMilliseconds;
            stopwatch.Restart();        

            foreach (Region region in habitableRegions)
            {
                stopwatch.Restart();
                if (region.pops.Count <= 0)
                {
                    region.linkUpdateCountdown = 0;
                    continue;
                }
                
                // Economy
                if (region.tradeLink == null) region.ZoneTrade();
            
                region.CalcBaseWealth();
                
                countedPerformanceInfo["Trade Route Time"] += stopwatch.Elapsed.TotalMilliseconds;
                stopwatch.Restart();

                if (region.CanUpdateTrade())
                {
                    region.LinkTrade();
                    region.linkUpdateCountdown = 12;
                }
                
                countedPerformanceInfo["Economy Time"] += stopwatch.Elapsed.TotalMilliseconds;
                stopwatch.Restart();

                // States
                region.RandomStateFormation();
                countedPerformanceInfo["State Formation Time"] += stopwatch.Elapsed.TotalMilliseconds;
                stopwatch.Restart();

                if (region.owner != null && !region.conquered)
                {
                    Region borderToActOn = region.PickRandomBorder();
                    if (region.frontier)
                    {
                        region.NeutralConquest(borderToActOn);
                    }
                    region.MilitaryConquest(borderToActOn);   
                }  
                countedPerformanceInfo["Conquest Time"] += stopwatch.Elapsed.TotalMilliseconds;  
                // Increments
                stopwatch.Restart();
                lock (this)
                {
                    countedPoppedRegions += 1;
                    worldPop += region.population;
                    highestPopulation = (long)Mathf.Max(highestPopulation, region.population);
                    newMaxWealth = Mathf.Max(newMaxWealth, region.wealth);
                    newMaxTradeWeight = Mathf.Max(region.tradeWeight, newMaxTradeWeight);                    
                }
                countedPerformanceInfo["Trade Weight Time"] += stopwatch.Elapsed.TotalMilliseconds;
            }
            
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }

        maxWealth = newMaxWealth;
        maxTradeWeight = newMaxTradeWeight;

        populatedRegions = countedPoppedRegions;
        worldPopulation = worldPop;

        regionPerformanceInfo = countedPerformanceInfo;
    }
    public void UpdateTradeZones()
    {
        try {
            if (useNewEconomy)
            {
                foreach (TradeZone tradeZone in tradeZoneIds.Values)
                {
                    tradeZone?.AggregateEconomies();
                }                 
            }
        } catch (Exception e)
        {
            GD.PushError(e);
        }
    }
    public void UpdateStates()
    {
        Dictionary<string, double> countedPerformanceInfo = new();
        countedPerformanceInfo["Delete Time"] = 0;
        countedPerformanceInfo["Parallel Time"] = 0;
        countedPerformanceInfo["Ruling Pop Time"] = 0;
        countedPerformanceInfo["Succession Time"] = 0;  
        countedPerformanceInfo["Join Wars Time"] = 0; 
        countedPerformanceInfo["Diplomacy Time"] = 0; 
        countedPerformanceInfo["AI Time"] = 0;
        countedPerformanceInfo["Stats Time"] = 0;

        Stopwatch stopwatch = Stopwatch.StartNew();
        foreach (var pair in statesIds.ToArray())
        {
            
            State state = pair.Value;
            if (state.vassals.Count > 0)
            {
                //GD.Print(state.regions.Sum(r => r.pops.Count));
            }
            if (state.rulingPop == null) state.FindNewRulingPop();
            if (state.claims.Count < 1 || state.StateCollapse() || state.rulingPop == null || state.capital == null || state.capital.claimant != state)
            {
                try
                {
                    ObjectManager.DeleteState(state);
                } catch (Exception e)
                {
                    GD.PushError(e);                 
                }
                continue;
            }  
        }        
        countedPerformanceInfo["Delete Time"] += stopwatch.Elapsed.TotalMilliseconds;
        stopwatch.Restart();         

        foreach (var pair in statesIds)
        {
            State state = pair.Value; 
            state.tech = state.rulingPop.tech;
            state.maxSize = 6 + state.rulingPop.tech.societyLevel;
            state.culture = state.rulingPop.culture;

            countedPerformanceInfo["Ruling Pop Time"] += stopwatch.Elapsed.TotalMilliseconds;
            stopwatch.Restart(); 

            state.Capitualate();
            state.UpdateStability();

            if (state.leader == null)
            {
                state.SuccessionUpdate();
            }
            countedPerformanceInfo["Succession Time"] += stopwatch.Elapsed.TotalMilliseconds;
            stopwatch.Restart();  

            if (state.sovereignty != Sovereignty.INDEPENDENT)
            {
                state.timeAsVassal += TimeManager.ticksPerMonth;
            }

            state.JoinObligateWars();

            countedPerformanceInfo["Join Wars Time"] += stopwatch.Elapsed.TotalMilliseconds;
            stopwatch.Restart();                                   
        }
        stopwatch.Restart(); 
        var partitioner = Partitioner.Create(statesIds.Values); 
        Parallel.ForEach(partitioner, (state) =>
        {
            state.UpdateRelations();     
        });
        UpdateDiplomacy();
        countedPerformanceInfo["Diplomacy Time"] += stopwatch.Elapsed.TotalMilliseconds;
        // Updates
        //  State Ai
        foreach (var pair in statesIds)
        {
            State state = pair.Value;
            state.AIManager.Tick();
        }
        countedPerformanceInfo["AI Time"] = stopwatch.Elapsed.TotalMilliseconds;
        stopwatch.Restart();
        // Counts State Stats
        Parallel.ForEach(partitioner, (state) =>
        {
            state.CountPopulation();
            state.UpdateDisplayColor();
            state.UpdateCapital();
            NameGenerator.UpdateStateName(state);
        });

        countedPerformanceInfo["Stats Time"] += stopwatch.Elapsed.TotalMilliseconds;
        stopwatch.Restart();

        statePerformanceInfo = countedPerformanceInfo;
    }
    public void UpdateCultures()
    {
        foreach (var pair in cultureIds)
        {
            Culture culture = pair.Value;

            if (culture.dead)
            {
                ObjectManager.DeleteCulture(culture);
                continue;
            }

            if (culture.pops.Count < 1)
            {
                culture.Die();
                continue;
            }
        }       
    }
    public void UpdateDiplomacy()
    {
        Partitioner<DiplomaticRelations> partitioner = Partitioner.Create(relationIds.Values);
        Parallel.ForEach(partitioner, relation =>
        {
            if (relation.truce > 0) relation.truce--;
            State state = rng.NextSingle() < 0.5f ? relation.initiator : relation.recipient;
            if (state.AIManager.CanTick())
            {
                state.AIManager.UpdateRelations(relation);
            }
        });
    }
    public void UpdateAlliances()
    {
        foreach (var pair in allianceIds)
        {
            Alliance alliance = pair.Value;
            if (alliance.leadState == null) alliance.Die();
            if (alliance.dead) ObjectManager.DeleteAlliance(alliance);
        }
        Partitioner<Alliance> partitioner = Partitioner.Create(allianceIds.Values);
        Parallel.ForEach(partitioner, alliance =>
        {
            alliance.tech = alliance.averageTech;
            alliance.CountPopulation();  
        });            
    }
    public void UpdateCharacters()
    {
        try
        {
            foreach (var pair in characterIds)
            {
                Character character = pair.Value;
                // Dead character stuff
                if (character.dead)
                {
                    // Deletes characters after 200 years if they are dead
                    if (timeManager.GetYear(character.GetAge()) > 200)
                    {
                        ObjectManager.DeleteCharacter(character);
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
        Dictionary<string, double> countedPerformanceInfo = new();
        try
        {
            Stopwatch processStopwatch = Stopwatch.StartNew();
            UpdatePops();
            countedPerformanceInfo["Pops"] = processStopwatch.Elapsed.TotalMilliseconds;
            processStopwatch.Restart();

            UpdateRegions();
            countedPerformanceInfo["Regions"] = processStopwatch.Elapsed.TotalMilliseconds;
            processStopwatch.Restart();

            UpdateTradeZones();
            countedPerformanceInfo["Trade Zones"] = processStopwatch.Elapsed.TotalMilliseconds;
            processStopwatch.Restart();

            UpdateStates();
            countedPerformanceInfo["States"] = processStopwatch.Elapsed.TotalMilliseconds;
            processStopwatch.Restart();

            UpdateCharacters();
            UpdateCultures();
            UpdateAlliances();
            countedPerformanceInfo["Misc"] = processStopwatch.Elapsed.TotalMilliseconds;
            processStopwatch.Restart();
          
        } catch  (Exception e)
        {
            GD.PushError(e);
        }
        stepPerformanceInfo = countedPerformanceInfo;
        totalStepTime = stepStopwatch.Elapsed.TotalMilliseconds;
    }
    public void SimYear()
    {

    }
    
}