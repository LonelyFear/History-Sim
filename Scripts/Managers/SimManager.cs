using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using FileAccess = Godot.FileAccess;

public partial class SimManager : Node
{
    public Node2D terrainMap;
    [Export(PropertyHint.Range, "4,16,4")]
    public int tilesPerRegion = 4;
    [Export]
    public TileMapLayer reliefs;
    [Export]
    public TimeManager timeManager { get; set; }
    [Export] public bool complexEconomy = false;

    public Tile[,] tiles;
    public List<Region> regions = new List<Region>();
    public List<Region> habitableRegions = new List<Region>();
    public Vector2I terrainSize;
    public static Vector2I worldSize;
    public static System.Threading.Mutex m = new System.Threading.Mutex();
    MapManager mapManager;

    // Population
    public List<Pop> pops = new List<Pop>();
    public long worldPopulation = 0;
    public long worldWorkforce = 0;
    public long worldDependents = 0;
    public long workforceChange = 0;
    public long dependentsChange = 0;
    public uint populatedRegions;
    public float maxAvgWealth;
    public List<Culture> cultures = new List<Culture>();
    public List<State> states = new List<State>();
    public List<Army> armies = new List<Army>();
    public List<Character> characters = new List<Character>();
    public List<Conflict> conflicts = new List<Conflict>();
    public List<War> wars = new List<War>();
    public List<Conflict> resolvedConflicts = new List<Conflict>();
    public List<War> endedWars = new List<War>();

    public int maxPopsPerRegion = 50;
    public long popTaskId = 0;

    int currentBatch = 0;
    Random rng = new Random();

    // Events
    public delegate void SimulationInitializedEventHandler();
    #region Utility
    public override void _Ready()
    {
        terrainMap = GetNode<Node2D>("/root/Game/Terrain Map");
        timeManager = GetParent().GetNode<TimeManager>("Time Manager");
        mapManager = (MapManager)GetParent().GetNode<Node>("Map Manager");

        // Connection
        WorldGenerator.worldgenFinishedEvent += OnWorldgenFinished;
        //Connect("WorldgenFinished", new Callable(this, nameof()));
    }

    public Vector2I GlobalToRegionPos(Vector2 pos)
    {
        return (Vector2I)(pos / (terrainMap.Scale * 16)) / tilesPerRegion;
    }

    public Vector2 RegionToGlobalPos(Vector2I regionPos)
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
    #region Initialization
    private void OnWorldgenFinished(object sender, EventArgs e)
    {
        PopObject.simManager = this;
        Army.simManager = this;
        Character.simManager = this;
        Pop.simManager = this;

        // Load Resources Before Buildings        
        terrainSize = WorldGenerator.WorldSize;
        worldSize = terrainSize / tilesPerRegion;
        #region Tile Initialization
        tiles = new Tile[terrainSize.X, terrainSize.Y];
        for (int x = 0; x < terrainSize.X; x++)
        {
            for (int y = 0; y < terrainSize.Y; y++)
            {
                Tile newTile = new Tile();
                tiles[x, y] = newTile;

                newTile.biome = WorldGenerator.BiomeMap[x, y];
                newTile.temperature = WorldGenerator.GetUnitTemp(WorldGenerator.TempMap[x, y]);
                newTile.moisture = WorldGenerator.GetUnitRainfall(WorldGenerator.RainfallMap[x, y]);
                newTile.elevation = WorldGenerator.GetUnitElevation(WorldGenerator.HeightMap[x, y]);

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
                    if (WorldGenerator.HeightMap[x, y] > WorldGenerator.MountainThreshold)
                    {
                        newTile.navigability *= 0.25f;
                        newTile.arability *= 0.25f;
                        newTile.survivalbility *= 0.8f;
                        newTile.terrainType = TerrainType.MOUNTAINS;
                    }
                    else if (WorldGenerator.HeightMap[x, y] > WorldGenerator.HillThreshold)
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
                        if (WorldGenerator.BiomeMap[nx, ny].type == "water")
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
                // Adds pops
                if (newRegion.habitable)
                {
                    // Add pops here
                    long startingPopulation = Pop.ToNativePopulation(rng.NextInt64(20, 100));
                    //CreatePop((long)(startingPopulation * 0.25f), (long)(startingPopulation * 0.75f), newRegion, new Tech(), CreateCulture());
                }
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
            for (int dx = -1; dx < 2; dx++)
            {
                for (int dy = -1; dy < 2; dy++)
                {
                    if ((dx != 0 && dy != 0) || (dx == 0 && dy == 0))
                    {
                        continue;
                    }
                    Region r = GetRegion(region.pos.X + dx, region.pos.Y + dy);
                    region.borderingRegions.Add(r);
                }
            }

        });
    }
    void InitPops()
    {
        foreach (Region region in habitableRegions)
        {
            double nodeChance = 0.005;

            if (rng.NextDouble() <= nodeChance && region.Migrateable())
            {
                long startingPopulation = Pop.ToNativePopulation(rng.NextInt64(1000, 2000));
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
        int popBatches = Mathf.Clamp(8, 0, pops.Count());
        /*
        Parallel.For(1, popBatches + 1, (batch) =>
        {
            //GD.Print("Pop Batch " + batch + " Running");
            for (int i = pops.Count / popBatches * (batch - 1); i < pops.Count / popBatches * batch - 1; i++)
            {
                Pop pop = pops[i];


            }
        });
        */
        foreach (Pop pop in pops.ToArray())
        {
            ulong startTime = Time.GetTicksMsec();
            if (pop.population <= Pop.ToNativePopulation(1 + pop.characters.Count))
            {
                m.WaitOne();
                DestroyPop(pop);
                m.ReleaseMutex();
            }
            else
            {
                pop.canMove = true;
                pop.income = 0f;
                pop.expenses = 0f;
                pop.EconomyUpdate();
                pop.GrowPop();
                if (pop.batchId == timeManager.GetMonth())
                {
                    pop.ProfessionTransitions();
                    pop.Migrate();
                }
            }            
        }

        // GD.Print("Pops Processing Time: " + (Time.GetTicksMsec() - tickStartTime) + " ms");
        // GD.Print("  Pops Delete Time: " + destroyTime + " ms");
        // GD.Print("  Pops Grow Time: " + growTime + " ms");
        // GD.Print("  Pops Move Time: " + migrateTime + " ms");
    }
    #endregion
    #region Region Update
    public void UpdateRegions()
    {
        uint countedPoppedRegions = 0;
        ulong tickStartTime = Time.GetTicksMsec();
        long worldPop = 0;
        int regionBatches = 8;
        Parallel.For(1, regionBatches + 1, (batch) =>
        {
            for (int i = habitableRegions.Count / regionBatches * (batch - 1); i < habitableRegions.Count / regionBatches * batch - 1; i++)
            {
                Region region = habitableRegions[i];
                // Trade Route Decay

                region.tradeWeight = Mathf.Clamp(region.tradeWeight, 0f, 100f);
                region.tradeWeight -= 1;
                region.economy.RotPerishables();

                if (region.pops.Count > 0)
                {
                    m.WaitOne();
                    countedPoppedRegions += 1;
                    m.ReleaseMutex();
                    
                    region.MergePops();
                    region.CheckPopulation();
                    region.CalcProfessionRequirements();

                    // Economy
                    region.CalcBaseWealth();
                    region.CalcTradeWeight();
                    region.CalcTaxes();
                    // region trade goes here
                    region.UpdateWealth();
                    if (region.owner != null && region.frontier && region.owner.rulingPop != null)
                    {
                        region.NeutralConquest();
                    }
                    region.TryFormState();
                    if (region.owner != null)
                    {
                        region.StateBordering();
                    }

                    m.WaitOne();
                    worldPop += region.population;
                    m.ReleaseMutex();
                }                
            }
        });
        populatedRegions = countedPoppedRegions;
        worldPopulation = worldPop;
        // GD.Print("Region Processing Time: " + (Time.GetTicksMsec() - tickStartTime) + " ms");
        // GD.Print("  Region Merge Time: " + mergeTime + " ms");
        // GD.Print("  Region Census Time: " + checkTime + " ms");
        // GD.Print("  Region Conquest Time: " + conquestTime + " ms");
        // GD.Print("  Region Border Time: " + borderTime + " ms");
    }
    #endregion
    #region Character Update
    public void UpdateCharacters()
    {
        ulong tickStartTime = Time.GetTicksMsec();
        foreach (Character character in characters.ToArray())
        {
            character.age += TimeManager.ticksPerMonth;
            character.existTime += TimeManager.ticksPerMonth;
            character.childCooldown--;

            bool exists = true;

            if (exists)
            {
                if (character.childCooldown <= 0)
                {
                    character.childCooldown = 0;
                }
                if (character.age < Character.maturityAge)
                {
                    character.ChildUpdate();
                }

                if (character.CanHaveChild() && rng.NextDouble() <= 1d - Mathf.Pow(1d - 0.05, 1d / 12d))
                {
                    character.HaveChild();
                    character.childCooldown = 12;
                }
                // Gets Death Chance Per Month
                double realDeathChance = 1d - Mathf.Pow(1d - character.GetDeathChance(), 1d / 12d);
                //GD.Print(realDeathChance);
                if (rng.NextDouble() <= realDeathChance)
                {
                    character.Die();
                }
            }
        }
    }
    #endregion
    #region State Update
    public void UpdateStates()
    {
        foreach (State state in states.ToArray())
        {
            state.age++;
            if (state.regions.Count < 1)
            {
                DeleteState(state);
                continue;
            }

            state.borderingStates = new List<State>();

            state.UpdateCapital();
            state.CountStatePopulation();
            state.Recruitment();
            if (state.rulingPop != null)
            {
                state.RulersCheck();
            }
            else
            {
                // State Collapse or Smth
                if (rng.NextSingle() < 0.2f)
                {
                    Region r = state.regions[rng.Next(0, state.regions.Count)];
                    state.RemoveRegion(r);
                }
            }
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
    #region Army Update
    public void UpdateArmies()
    {
        foreach (Army army in armies.ToArray())
        {
            army.age += TimeManager.daysPerTick;
        }
    }
    #endregion
    #region SimTick
    public void SimTick()
    {
        UpdateArmies();
    }
    public void SimMonth()
    {
        try
        {
            UpdatePops();
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }
        UpdateRegions();
        UpdateStates();
        UpdateCharacters();
        UpdateCultures();
    }
    public void SimYear()
    {

    }
    #endregion

    #region Stat Update
    void UpdateStats()
    {
        worldDependents += dependentsChange;
        worldWorkforce += workforceChange;
        dependentsChange = 0;
        workforceChange = 0;
        worldPopulation = worldDependents + worldWorkforce;
    }
    #endregion
    #region Creation
    #region Pops Creation
    public Pop CreatePop(long workforce, long dependents, Region region, Tech tech, Culture culture, Profession profession = Profession.FARMER)
    {
        currentBatch += 1;
        if (currentBatch > 12)
        {
            currentBatch = 1;
        }

        Pop pop = new Pop()
        {
            batchId = currentBatch,
            tech = tech,
            profession = profession,
            workforce = workforce,
            dependents = dependents,
            population = workforce + dependents,
        };
        //pop.ChangePopulation(workforce, dependents);

        pops.Add(pop);
        culture.AddPop(pop, culture);
        region.AddPop(pop, region);

        return pop;
    }

    public void DestroyPop(Pop pop)
    {
        if (pop.region.owner != null && pop.region.owner.rulingPop == pop)
        {
            pop.region.owner.rulingPop = null;
        }
        pop.ClaimLand(-pop.ownedLand);
        pop.region.RemovePop(pop, pop.region);
        pop.culture.RemovePop(pop, pop.culture);
        pops.Remove(pop);
        foreach (Character character in pop.characters.ToArray())
        {
            DeleteCharacter(character);
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
            foundTick = timeManager.ticks
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
                foundTick = timeManager.ticks
            };
            states.Add(state);
            state.AddRegion(region);
        }
    }

    public void DeleteState(State state)
    {
        states.Remove(state);
        foreach (Region region in state.regions.ToArray())
        {
            state.RemoveRegion(region);
        }
        foreach (Character character in state.characters.ToArray())
        {
            DeleteCharacter(character);
        }
    }
    #endregion
    #region Characters Creation
    public Character CreateCharacter(Pop pop, int minAge = 0, int maxAge = 30, Character.Gender gender = Character.Gender.MALE)
    {
        string charName = NameGenerator.GenerateCharacterName();
        if (gender == Character.Gender.FEMALE)
        {
            charName = NameGenerator.GenerateCharacterName(true);
        }
        Character character = new Character()
        {
            name = charName,
            culture = pop.culture,
            agression = rng.Next(0, 101),
            age = (uint)rng.Next(minAge * 12, (maxAge + 1) * 12),
        };
        pop.AddCharacter(character);
        if (pop == null)
        {
            GD.PushError("No pop to add character to");
        }
        else if (pop.region == null)
        {
            GD.PushError("No region to add character to");
        }
        else if (pop.region.owner == null)
        {
            GD.PushError("No state to add character to");
        }
        else
        {
            pop.region.owner.AddCharacter(character);
        }
        characters.Add(character);
        return character;
    }

    public void DeleteCharacter(Character character)
    {
        //GD.Print("Character Deleted");
        try
        {
            if (character.pop != null)
                character.pop.RemoveCharacter(character);
            if (character.state != null)
            {
                character.state.RemoveCharacter(character);
            }
            if (character.parent != null)
            {
                character.parent.children.Remove(character);
            }

            characters.Remove(character);
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }
    }
    #endregion
    #region Armies Creation
    public Army CreateArmy(Region region, State state, ulong strength)
    {
        Army army = new Army
        {
            headquarters = region,
            strength = strength,
        };
        state.AddArmy(army);
        region.AddArmy(army);
        armies.Add(army);
        return army;
    }

    public void DestroyArmy(Army army)
    {
        armies.Remove(army);
        army.state.RemoveArmy(army);
        army.location.RemoveArmy(army);
    }
    #endregion
    #region Diplomacy Manager
    public Conflict StartConflict(State agressor, State defender, List<State> agressorSupporters, List<State> defenderSupporters, Conflict.Type type)
    {
        Conflict conflict = new Conflict()
        {
            type = type,
            simManager = this
        };

        conflict.AddParticipant(agressor, Conflict.Side.AGRESSOR);
        conflict.AddParticipant(defender, Conflict.Side.DEFENDER);

        foreach (State state in agressorSupporters)
        {
            conflict.AddParticipant(state, Conflict.Side.AGRESSOR);
        }
        foreach (State state in defenderSupporters)
        {
            conflict.AddParticipant(state, Conflict.Side.AGRESSOR);
        }

        conflicts.Add(conflict);
        return conflict;
    }

    public void StartWar(Conflict conflict)
    {

    }
    #endregion
    #endregion
}