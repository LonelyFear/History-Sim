using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    [IgnoreMember] [Export(PropertyHint.Range, "4,16,4")] public int tilesPerRegion = 4; 
    
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
    [IgnoreMember] public List<Pop> pops { get; set; } = new List<Pop>();
    [IgnoreMember] public Dictionary<ulong, Pop> popsIds { get; set; } = new Dictionary<ulong, Pop>();
    [IgnoreMember] public List<Culture> cultures { get; set; } = new List<Culture>();
    [IgnoreMember] public Dictionary<ulong, Culture> cultureIds { get; set; } = new Dictionary<ulong, Culture>();
    [IgnoreMember] public List<State> states { get; set; } = new List<State>();
    [IgnoreMember] public Dictionary<ulong, State> statesIds { get; set; } = new Dictionary<ulong, State>();
    [IgnoreMember] public List<ulong> deletedStateIds = new List<ulong>();
    [IgnoreMember] public List<TradeZone> tradeZones { get; set; } = new List<TradeZone>();
    [IgnoreMember] public Dictionary<ulong, TradeZone> tradeZonesIds { get; set; } = new Dictionary<ulong, TradeZone>();
    [IgnoreMember] public List<Character> characters { get; set; } = new List<Character>();
    [IgnoreMember] public Dictionary<ulong, Character> charactersIds { get; set; } = new Dictionary<ulong, Character>();
    [IgnoreMember] public List<Alliance> alliances { get; set; } = new List<Alliance>();
    [IgnoreMember] public Dictionary<ulong, Alliance> allianceIds { get; set; } = new Dictionary<ulong, Alliance>();
    [IgnoreMember] public List<War> wars { get; set; } = new List<War>();
    [IgnoreMember] public Dictionary<ulong, War> warIds { get; set; } = new Dictionary<ulong, War>();
    [IgnoreMember] public Dictionary<ulong, BaseEvent> historicalEventIds = new Dictionary<ulong, BaseEvent>();

    // Misc
    public uint currentBatch = 2;
    public ulong currentId = 0;
    [IgnoreMember] public bool simLoadedFromSave = false;

    [IgnoreMember] public Random rng = new Random();

    // Events
    public delegate void ObjectDeletedEvent(ulong id);
    [IgnoreMember] public ObjectDeletedEvent objectDeleted;
    
    // Debug info
    [IgnoreMember] public ulong totalStepTime;
    [IgnoreMember] public ulong totalPopsTime;
    [IgnoreMember] public ulong totalStateTime;
    [IgnoreMember] public ulong totalRegionTime;
    [IgnoreMember] public ulong totalCharacterTime;
    [IgnoreMember] public ulong totalMiscTime;
    #region Utility

    public Vector2I GlobalToRegionPos(Vector2 pos)
    {
        return (Vector2I)(pos / (terrainMap.Scale * 16)) / tilesPerRegion;
    }

    public Vector2 RegionToGlobalPos(Vector2 regionPos)
    {
        return tilesPerRegion * (regionPos * (terrainMap.Scale * 16));
    }
    #endregion
    #region Saving & Loading
    public void SaveSimToFile(string path)
    {
        regions.ForEach(r => r.PrepareForSave());
        pops.ForEach(r => r.PrepareForSave());
        //wars.ForEach(r => r.PrepareForSave());
        states.ForEach(r => r.PrepareForSave());
        tradeZones.ForEach(r => r.PrepareForSave());
        cultures.ForEach(r => r.PreparePopObjectForSave());
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

        sim.simLoadedFromSave = true;
        return sim;
    }
    #endregion
    #region Initialization
    public void RebuildAfterSave()
    {
        AssignSimManager();
        timeManager.ticks = tick;
        regions = [.. regionIds.Values];
        pops = [.. popsIds.Values];
        states = [.. statesIds.Values];
        cultures = [.. cultureIds.Values];
        wars = [.. warIds.Values];
        tradeZones = [.. tradeZonesIds.Values];
        characters = [.. charactersIds.Values];
        
        foreach (Region region in regions)
        {
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
            // Calc max populaiton
            region.CalcSocialClassRequirements();
        }
        BorderingRegions();

        pops.ForEach(r => r.LoadFromSave());
        //wars.ForEach(r => r.LoadFromSave());
        states.ForEach(r => r.LoadFromSave());
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
                // Calc max populaiton
                newRegion.CalcSocialClassRequirements();
            }
        }
    }
    void AssignSimManager()
    {
        DiplomacyManager.objectManager = objectManager;
        ObjectManager.simManager = this;
        ObjectManager.timeManager = timeManager;

        NamedObject.simManager = this;
        NamedObject.objectManager = objectManager;
        PopObject.timeManager = timeManager;

        TradeZone.simManager = this;
        TradeZone.objectManager = objectManager;

        Pop.objectManager = objectManager;
        Character.sim = this;
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
                    Region r = objectManager.GetRegion(region.pos.X + dx, region.pos.Y + dy);
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
                    Region r = objectManager.GetRegion(region.pos.X + dx, region.pos.Y + dy);
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
                Culture culture = objectManager.CreateCulture();

                objectManager.CreatePop((long)(startingPopulation * 0.25f), (long)(startingPopulation * 0.75f), region, new Tech(), culture, SocialClass.FARMER);
            }
        }
    }
    #region Pop Update
    public void UpdatePops()
    {
        foreach (Pop pop in pops.ToArray())
        {
            ulong startTime = Time.GetTicksMsec();
            if (pop.region == null || pop.population <= Pop.ToNativePopulation(1))
            {
                objectManager.DestroyPop(pop);
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
            foreach (Region region in habitableRegions)
            {
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
            if (state.rulingPop == null)
            {
                // State Collapse or Smth
                if (rng.NextSingle() < 0.5f)
                {
                    Region r = state.regions[rng.Next(0, state.regions.Count)];
                    state.RemoveRegion(r);
                }
            }   
            if (state.regions.Count < 1 || state.StateCollapse())
            {
                objectManager.DeleteState(state);
                continue;
            }
            if (state.rulingPop != null)
            {
                state.tech = state.rulingPop.Tech;
            }
            state.GetRealmBorders();
            state.Capitualate();
        }
        foreach (State state in states.ToArray())
        {

            if (state.rulingPop != null)
            {
                state.maxSize = 6 + state.rulingPop.Tech.societyLevel;
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
                    foreach (War war in state.liege.diplomacy.warIds.Keys.Select(id => objectManager.GetWar(id)))
                    {
                        war.AddParticipant(state.id, state.liege.diplomacy.warIds[war.id]);
                    }  
                }


                state.UpdateCapital();

                state.diplomacy.RelationsUpdate();
                state.diplomacy.UpdateDiplomacy();
                state.diplomacy.UpdateEnemies();

                state.diplomacy.EndWars();
                state.diplomacy.StartWars();     
            } catch (Exception e)
            {
                GD.PushError(e);
            }
        }
        var partitioner = Partitioner.Create(states.ToArray());
        Parallel.ForEach(partitioner, (state) =>
        {
            state.CountStatePopulation();
            state.Recruitment();
            state.UpdateDisplayColor();
            StateNamer.UpdateStateNames(state);
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
                character.name = $"{character.firstName} {character.lastName}";
                // Dead character stuff
                if (character.dead)
                {
                    objectManager.DeleteCharacter(character);
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
        try
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
        } catch  (Exception e)
        {
            GD.PushError(e);
        }
    }
    public void SimYear()
    {

    }
    #endregion
}