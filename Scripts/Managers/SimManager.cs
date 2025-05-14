using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

public partial class SimManager : Node
{
    [Export]
    public WorldGeneration world {private set; get;}
    [Export]
    public int tilesPerRegion = 4;
    [Export(PropertyHint.Range, "4,16,4")]
    public TimeManager timeManager { get; set; }

    public Tile[,] tiles;
    public List<Region> regions = new List<Region>();
    public List<Region> habitableRegions = new List<Region>();
    public Vector2I terrainSize;   
    public Vector2I worldSize;
    MapManager mapManager;

    // Population
    public List<Pop> pops = new List<Pop>();
    public long worldPopulation = 0;
    public long worldWorkforce = 0;
    public long worldDependents = 0;
    public long workforceChange = 0;
    public long dependentsChange = 0;
    public List<Culture> cultures = new List<Culture>();
    public List<State> states = new List<State>();
    public List<Character> characters = new List<Character>();
    public List<Conflict> conflicts = new List<Conflict>();
    public List<War> wars = new List<War>(); 
    public List<Conflict> resolvedConflicts = new List<Conflict>();
    public List<War> endedWars = new List<War>(); 

    public int maxPopsPerRegion = 50;
    public long popTaskId = 0;

    int currentBatch = 0;

    public long simToPopMult;
    public Task mapmodeTask;
    Random rng = new Random();

    // Events
    public delegate void SimulationInitializedEventHandler();

    // Saved Stuff
    public Dictionary<string, SimResource> resources = new Dictionary<string, SimResource>();
    public Dictionary<string, BuildingData> buildings = new Dictionary<string, BuildingData>();
    public override void _Ready()
    {
        for (int i = 0; i < 10; i++){
            //GD.Print(NameGenerator.GenerateCharacterName());
            //GD.Print(NameGenerator.GenerateCharacterName(true));
        }
        simToPopMult = Pop.simPopulationMultiplier;
        world = (WorldGeneration)GetParent().GetNode<Node2D>("World");
        timeManager = GetParent().GetNode<TimeManager>("Time Manager");
        mapManager = (MapManager)GetParent().GetNode<Node>("Map Manager");

        // Connection
        world.Connect("worldgenFinished", new Callable(this, nameof(OnWorldgenFinished)));
        timeManager.Tick += SimTick;
    }

    public Vector2I GlobalToRegionPos(Vector2 pos){
        return (Vector2I)(pos / (world.Scale * 16))/tilesPerRegion;
    }

    public Vector2 RegionToGlobalPos(Vector2I regionPos){
        return tilesPerRegion * (regionPos * (world.Scale * 16));
    }

    void LoadBuildings(){
        string buildingPath = "Data/Buildings";

        if (Directory.Exists(buildingPath)){
            foreach (string subPath in Directory.GetFiles(buildingPath)){

                StreamReader reader = new StreamReader(subPath.Replace("\\", "/"));
                string buildingData = reader.ReadToEnd();
                BuildingData building = JsonSerializer.Deserialize<BuildingData>(buildingData);

                buildings.Add(building.id, building);
                foreach (string id in building.resourcesProducedIds.Keys){
                    if (GetResource(id) == null){
                        GD.PrintErr("Building couldnt load resource '" + id + "'");
                        return;
                    }
                    building.resourcesProduced.Add(GetResource(id), building.resourcesProducedIds[id]);
                }
            }

        } else {
            GD.PrintErr("Buildings directory not found at path '" + buildingPath + "'"); 
        }
    }
    void LoadResources(){
        string resourcesPath = "Data/Resources/";

        if (Directory.Exists(resourcesPath)){
            foreach (string subPath in Directory.GetFiles(resourcesPath)){
                StreamReader reader = new StreamReader(subPath.Replace("\\", "/"));
                string resourceData = reader.ReadToEnd();
                SimResource resource = JsonSerializer.Deserialize<SimResource>(resourceData);

                resources.Add(resource.id, resource);            
            }

        } else {
            GD.PrintErr("Resources directory not found at path '" + resourcesPath + "'"); 
        }
    }
    public SimResource GetResource(string id){
        if (resources.ContainsKey(id)){
            return resources[id];
        } else {
            GD.PrintErr("Resource not found with ID '" + id + "'");
            return null;
        }
    }
    public BuildingData GetBuilding(string id){
        if (buildings.ContainsKey(id)){
            return buildings[id];
        } else {
            GD.PrintErr("Building not found with ID '" + id + "'");
            return null;
        }
    }
    private void OnWorldgenFinished(){ 
        
        LoadResources();
        // Load Resources Before Buildings
        LoadBuildings();
        terrainSize = world.worldSize;
        worldSize = terrainSize/tilesPerRegion;

        tiles = new Tile[terrainSize.X, terrainSize.Y];
        for (int x = 0; x < terrainSize.X; x++){
            for (int y = 0; y < terrainSize.Y; y++){
                Tile newTile = new Tile();
                tiles[x,y] = newTile;
                newTile.biome = world.biomes[x,y];
            }
        }

        for (int x = 0; x < worldSize.X; x++){
            for (int y = 0; y < worldSize.Y; y++){
                // Creates a region
                Region newRegion = new Region();

                newRegion.tiles = new Tile[tilesPerRegion, tilesPerRegion];
                newRegion.biomes = new Biome[tilesPerRegion, tilesPerRegion];

                newRegion.simManager = this;
                newRegion.pos = new Vector2I(x,y);
                regions.Add(newRegion);
                for (int tx = 0; tx < tilesPerRegion; tx++){
                    for (int ty = 0; ty < tilesPerRegion; ty++){
                        // Adds subregion to tile
                        Tile tile = tiles[x * tilesPerRegion + tx, y * tilesPerRegion + ty];
                        newRegion.tiles[tx, ty] = tile;
                        // Adds biomes to tile
                        newRegion.biomes[tx, ty] = tile.biome;
                    }
                }
                // Calc average fertility
                newRegion.CalcAvgFertility();
                // Checks habitability
                newRegion.CheckHabitability();
                if (newRegion.habitable){
                    habitableRegions.Add(newRegion);
                }
                // Calc max populaiton
                newRegion.CalcMaxPopulation();
                // Adds pops
                // if (newRegion.habitable){
                //     // Add pops here
                //     long startingPopulation = Pop.ToNativePopulation(rng.NextInt64(20, 100));
                //     CreatePop((long)(startingPopulation * 0.25f), (long)(startingPopulation * 0.75f), newRegion, new Tech(), CreateCulture(newRegion));
                // }
            }
        }
        BorderingRegions();
        InitPops();
        mapManager.InitMapManager();
    }

    void BorderingRegions(){
        Parallel.ForEach(regions, region =>{     
            for (int dx = -1; dx < 2; dx++){
                for (int dy = -1; dy < 2; dy++){
                    if ((dx != 0  && dy != 0) || (dx == 0 && dy == 0)){
                        continue;
                    }  
                    Region r = GetRegion(region.pos.X + dx, region.pos.Y + dy);       
                    region.borderingRegions.Add(r);                 
                }
            }

        });  
    }
    void InitPops(){    
        foreach (Region region in habitableRegions){
            double nodeChance = 0.01/world.worldSizeMult;
            nodeChance *= region.avgFertility;

            if (rng.NextDouble() <= nodeChance && region.avgFertility > 0.0){
                long startingPopulation = Pop.ToNativePopulation(rng.NextInt64(1000, 2000));
                Culture culture = CreateCulture();
                foreach (Region testRegion in habitableRegions){
                    if (testRegion.pops.Count > 0 && testRegion.pos.DistanceTo(region.pos) <= 7 * world.worldSizeMult){
                        culture = testRegion.pops[0].culture;
                    }
                }
                CreatePop((long)(startingPopulation * 0.25f), (long)(startingPopulation * 0.75f), region, new Tech(), culture);
            }
        }
    }
    public void UpdateRegions(){
        long worldPop = 0;
        int populatedRegions = 0;
        foreach (Region region in habitableRegions){
            if (region.pops.Count > 0){
                populatedRegions++;
                foreach (Pop pop in region.pops.ToArray()){
                    if (pop.population <= Pop.ToNativePopulation(1 + pop.characters.Count)){
                        DestroyPop(pop);
                    } else {
                        region.GrowPop(pop);
                        if (pop.batchId == timeManager.month){
                            region.MigratePop(pop);
                        }
                    }
                    
                }
            }        
        }

        foreach (Region region in habitableRegions){
            if (region.pops.Count > 0){
                region.MergePops();
                region.CheckPopulation();  
                
                foreach (Pop pop in region.pops){
                    if (region.owner != null){
                        region.PopWealth(pop);
                    }
                } 

                region.RandomStateFormation();
                if (region.owner != null){
                    region.StateBordering();
                    if (region.frontier && region.owner.rulingPop != null){
                        region.NeutralConquest();
                    }
                } 

          
            }       
            worldPop += region.population;            
        }

        worldPopulation = worldPop;    
    }

    public void UpdateCharacters(){
        foreach (Character character in characters.ToArray()){
            character.age++;
            character.existTime++;
            character.childCooldown--;

            bool exists = true;

            if (exists){
                if (character.childCooldown <= 0){
                    character.childCooldown = 0;
                }
                if (character.age < Character.maturityAge){
                    character.ChildUpdate();            
                }

                if (character.CanHaveChild() && rng.NextDouble() <= 1d - Mathf.Pow(1d - 0.05, 1d/12d)){
                    character.HaveChild();
                    character.childCooldown = 12;
                }
                // Gets Death Chance Per Month
                double realDeathChance = 1d - Mathf.Pow(1d - character.GetDeathChance(), 1d/12d);
                //GD.Print(realDeathChance);
                if (rng.NextDouble() <= realDeathChance){
                    character.Die();
                }                
            } 
        }        
    }
    public void UpdateStates(){
        foreach (State state in states.ToArray()){

            if (state.regions.Count < 1){
                DeleteState(state);
                continue;
            }

            state.borderingStates = new List<State>();
            state.age++;
            state.UpdateCapital();
            state.CountStatePopulation();
            state.Recruitment();    
            if (state.rulingPop != null){
                state.RulersCheck();
            } else {
                // State Collapse or Smth
                if (rng.NextSingle() < 0.2f){
                    Region r = state.regions[rng.Next(0, state.regions.Count)];
                    state.RemoveRegion(r);                    
                }
            }   
        }     
    }
    #region SimTick
    public void SimTick(){        
        UpdateRegions();
        UpdateStates();        
        UpdateCharacters();

        mapManager.UpdateRegionColors();
    }
    #endregion

    #region Pops
    void UpdateStats(){
        worldDependents += dependentsChange;
        worldWorkforce += workforceChange;
        dependentsChange = 0;
        workforceChange = 0;
        worldPopulation = worldDependents + worldWorkforce;
    }
    public Pop CreatePop(long workforce, long dependents, Region region, Tech tech, Culture culture, Profession profession = Profession.FARMER){
        currentBatch += 1;
        if (currentBatch > 12){
            currentBatch = 1;
        }

        Pop pop = new Pop(){
            batchId = currentBatch,
            tech = tech,
            profession = profession,
            workforce = workforce,
            dependents = dependents,
            population = workforce + dependents
        };
        //pop.ChangePopulation(workforce, dependents);

        pops.Add(pop);
        region.AddPop(pop, region);       
        culture.AddPop(pop, culture);

        return pop;
    }

    public void DestroyPop(Pop pop){
        if (pop.region.owner != null && pop.region.owner.rulingPop == pop){
            pop.region.owner.rulingPop = null;
        }
        pop.region.RemovePop(pop, pop.region);
        pop.culture.RemovePop(pop, pop.culture);
        pops.Remove(pop);
        foreach (Character character in pop.characters.ToArray()){
            DeleteCharacter(character);
        }
    }
    #endregion

    public Region GetRegion(int x, int y){
        int lx = Mathf.PosMod(x, worldSize.X);
        int ly = Mathf.PosMod(y, worldSize.Y);

        int index = (lx * worldSize.Y) + ly;
        return regions[index];
    }
    #region Creation
    public Culture CreateCulture(){
        float r = rng.NextSingle();
        float g = rng.NextSingle();
        float b = rng.NextSingle();        
        Culture culture = new Culture(){
            name = "Culturism",
            color = new Color(r,g,b)
        };

        cultures.Append(culture);

        return culture;
    }
    public void CreateState(Region region){
        if (region.owner == null){
            float r = Mathf.Lerp(0.2f, 1f, rng.NextSingle());
            float g = Mathf.Lerp(0.2f, 1f, rng.NextSingle());
            float b = Mathf.Lerp(0.2f, 1f, rng.NextSingle());    
            State state = new State(){
                name = NameGenerator.GenerateNationName(),
                color = new Color(r, g, b),
                capital = region,
                simManager = this
            };
            states.Add(state);       
            state.AddRegion(region);
        }
    }

    public void DeleteState(State state){
        states.Remove(state);
        foreach (Region region in state.regions.ToArray()){
            state.RemoveRegion(region);
        }
        foreach (Character character in state.characters.ToArray()){
            DeleteCharacter(character);
        }
    }
    public Character CreateCharacter(Pop pop, int minAge = 0, int maxAge = 30, Character.Gender gender = Character.Gender.MALE){
        string charName = NameGenerator.GenerateCharacterName();
        if (gender == Character.Gender.FEMALE){
            charName = NameGenerator.GenerateCharacterName(true);
        }
        Character character = new Character(){
            name = charName,
            culture = pop.culture,
            agression = rng.Next(0, 101),
            age = (uint)rng.Next(minAge * 12, (maxAge + 1) * 12),
            simManager = this
        };
        pop.AddCharacter(character);
        if (pop == null){
            GD.PushError("No pop to add character to");
        } else if (pop.region == null){
            GD.PushError("No region to add character to");
        } else if (pop.region.owner == null){
            GD.PushError("No state to add character to");
        } else {
            pop.region.owner.AddCharacter(character);            
        }
        characters.Add(character);
        return character;
    }

    public void DeleteCharacter(Character character){
        //GD.Print("Character Deleted");
        try {
            if (character.pop != null)
            character.pop.RemoveCharacter(character);
            if (character.state != null){
                character.state.RemoveCharacter(character);
            }
            if (character.parent != null){
                character.parent.children.Remove(character);
            }

            characters.Remove(character);            
        } catch (Exception e) {
            GD.PushError(e);
        }

    }

    public Conflict StartConflict(State agressor, State defender, List<State> agressorSupporters, List<State> defenderSupporters, Conflict.Type type){
        Conflict conflict = new Conflict(){
            type = type,
            simManager = this
        };
        
        conflict.AddParticipant(agressor, Conflict.Side.AGRESSOR);
        conflict.AddParticipant(defender, Conflict.Side.DEFENDER);

        foreach (State state in agressorSupporters){
            conflict.AddParticipant(state, Conflict.Side.AGRESSOR);
        }
        foreach (State state in defenderSupporters){
            conflict.AddParticipant(state, Conflict.Side.AGRESSOR);
        }

        conflicts.Add(conflict);
        return conflict;
    }

    public void StartWar(Conflict conflict){
        
    }
    #endregion
}