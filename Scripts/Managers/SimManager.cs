using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class SimManager : Node2D
{
    [Export]
    public WorldGeneration world {private set; get;}
    [Export]
    public int tilesPerRegion = 4;
    Sprite2D regionOverlay;
    [Export(PropertyHint.Range, "4,16,4")]
    public TimeManager timeManager { get; set; }

    public Tile[,] tiles;
    public List<Region> regions = new List<Region>();
    public List<Region> habitableRegions = new List<Region>();
    public Vector2I terrainSize;   
    public Vector2I worldSize;
    public MapModes mapMode = MapModes.POLITIY;
    public System.Threading.Mutex m = new System.Threading.Mutex();

    Image regionImage;

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

    public int maxPopsPerRegion = 50;
    public bool mapUpdate = false;
    public long popTaskId = 0;

    int currentBatch = 0;
    int month = 1;

    public long simToPopMult;
    public Task task;
    public Task mapmodeTask;
    Random rng = new Random();
    public Vector2 mousePos;

    // Hovering
    public Vector2I hoveredRegionPos;
    public Region hoveredRegion = null;
    public State hoveredState = null;

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
        regionOverlay = GetNode<Sprite2D>("RegionOverlay");
        world = (WorldGeneration)GetParent().GetNode<Node2D>("World");
        timeManager = GetParent().GetNode<TimeManager>("Time Manager");

        // Connection
        world.Connect("worldgenFinished", new Callable(this, nameof(OnWorldgenFinished)));
        timeManager.Tick += SimTick;
    }

    public override void _Process(double delta)
    {
        mousePos = GetGlobalMousePosition();
        hoveredRegionPos = GlobalToRegionPos(mousePos);
        if (hoveredRegionPos.X >= 0 && hoveredRegionPos.X < worldSize.X && hoveredRegionPos.Y >= 0 && hoveredRegionPos.Y < worldSize.Y){
            hoveredRegion = GetRegion(hoveredRegionPos.X, hoveredRegionPos.Y);
            hoveredState = hoveredRegion.owner;
        } else {
            hoveredRegion = null;
            hoveredState = null;
        }
        CheckMapmodeChange();
    }
    void CheckMapmodeChange(){
        if (mapmodeTask == null || mapmodeTask.IsCompleted){
            if (Input.IsActionJustPressed("MapMode_Polity")){
                mapmodeTask = Task.Run(() => SetMapMode(MapModes.POLITIY));
            }         
            else if (Input.IsActionJustPressed("MapMode_Culture")){
                mapmodeTask = Task.Run(() => SetMapMode(MapModes.CULTURE));
            }      
            else if (Input.IsActionJustPressed("MapMode_Population")){
                mapmodeTask = Task.Run(() => SetMapMode(MapModes.POPULATION));
            }   
        }        
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
        Scale = world.Scale * tilesPerRegion;
        regionImage = Image.CreateEmpty(worldSize.X, worldSize.Y, true, Image.Format.Rgba8);

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
                regionImage.SetPixel(x, y, Color.Color8(0,255,0,1));
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
                // // Adds pops
                // if (newRegion.habitable){
                //     // Add pops here
                //     long startingPopulation = Pop.toNativePopulation(rng.NextInt64(20, 100));
                //     CreatePop((long)(startingPopulation * 0.25f), (long)(startingPopulation * 0.75f), newRegion, new Tech(), CreateCulture(newRegion));
                // }
            }
        }
        BorderingRegions();
        InitPops();
        regionOverlay.Texture = ImageTexture.CreateFromImage(regionImage);
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
            double nodeChance = 0.0025;
            nodeChance *= region.avgFertility;
            if (region.coastal){
                nodeChance *= 10;
            }
            if (rng.NextDouble() <= nodeChance && region.avgFertility > 0.2){
                long startingPopulation = Pop.ToNativePopulation(rng.NextInt64(1000, 2000));
                CreatePop((long)(startingPopulation * 0.25f), (long)(startingPopulation * 0.75f), region, new Tech(), CreateCulture(region));
            }
        }
    }
    public void UpdateRegions(){
        long worldPop = 0;
        Parallel.ForEach(habitableRegions, region =>{
            if (region.pops.Count > 0){
                foreach (Pop pop in region.pops.ToArray()){
                    region.GrowPop(pop);
                    region.MigratePop(pop);
                }

                region.RandomStateFormation();
                if (region.owner != null){
                    region.StateBordering();
                    if (region.frontier){
                        region.NeutralConquest();
                    }
                }
            }          
        });

        Parallel.ForEach(habitableRegions, region =>{
            if (region.pops.Count > 0){
                region.MergePops();
                region.CheckPopulation();     
                foreach (Pop pop in region.pops.ToArray()){
                    if (region.owner != null){
                        region.PopWealth(pop);
                    }
                }           
            }       
            worldPop += region.population;
        });    
        worldPopulation = worldPop;    
    }

    public void UpdateCharacters(){
        Parallel.ForEach(characters.ToArray(), character =>{
            character.age++;
            character.existTime++;
            bool exists = true;

            if (character.existTime > 20*12 && character.role == Character.Role.CIVILIAN){
                m.WaitOne();
                DeleteCharacter(character);
                exists = false;
                m.ReleaseMutex();
                
            }
            if (exists){
                if (character.state.leader == character){
                    character.role = Character.Role.LEADER;
                    Character heir = character.GetHeir();
                    if (heir != null){
                        heir.role = Character.Role.HEIR;
                    }                
                }
        
                if (character.state.leader == character && rng.NextSingle() <= 0.02/12 && character.age > 20 * 12){
                    character.HaveChild();
                }

                if (character.age > (60 * 12) && rng.NextSingle() <= 0.05/12){
                    m.WaitOne();
                    character.Die();
                    m.ReleaseMutex();
                }                
            } 
        });        
    }
    public void UpdateStates(){
        Parallel.ForEach(states, state => {
            //state.CountPopulation();
            state.age++;
            if (state.leader != null && state.leader.family != null){
                state.rulingFamily = state.leader.family;
            } else {
                state.rulingFamily = null;
            }
            state.UpdateCapital();
            state.CountPopulation();
            state.Recruitment();
            
        });
        foreach (State state in states.ToArray()){
            state.RulersCheck();
        }        
    }
    #region SimTick
    public void SimTick(){        
        Parallel.ForEach(states, state =>{
            state.borderingStates = new List<State>();
        });
        UpdateRegions();
        //UpdateCharacters();
        UpdateStates();
        
        Parallel.ForEach(regions, region =>{
            SetRegionColor(region.pos.X, region.pos.Y, GetRegionColor(region));
        });
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

        Pop pop = new Pop();
        pop.batchId = currentBatch;

        pop.ChangePopulation(workforce, dependents);

        pops.Add(pop);
        region.AddPop(pop);       
        culture.AddPop(pop);

        pop.tech = tech;
        pop.profession = profession;
        pop.tech.industryLevel = tech.industryLevel;

        return pop;
    }

    public void DestroyPop(Pop pop){
        if (pop.region != null){
            pop.region.RemovePop(pop);
        }
        pop.culture.ChangePopulation(-pop.population);
        pops.Remove(pop);
    }
    #endregion

    public Region GetRegion(int x, int y){
        int lx = Mathf.PosMod(x, worldSize.X);
        int ly = Mathf.PosMod(y, worldSize.Y);

        int index = (lx * worldSize.Y) + ly;
        return regions[index];
    }
    #region Creation
    public Culture CreateCulture(Region region){
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
    public void CreateNation(Region region){
        if (region.owner == null){
            float r = Mathf.InverseLerp(0.2f, 1f, rng.NextSingle());
            float g = Mathf.InverseLerp(0.2f, 1f, rng.NextSingle());
            float b = Mathf.InverseLerp(0.2f, 1f, rng.NextSingle());    
            State state = new State(){
                name = NameGenerator.GenerateNationName(),
                color = new Color(r, g, b),
                capital = region
            };
            states.Add(state);       
            state.AddRegion(region);
        }
    }
    public Character CreateCharacter(Pop pop, Family family = null, int minAge = 0, int maxAge = 30){
        Character character = new Character(){
            name = NameGenerator.GenerateCharacterName(),
            culture = pop.culture,
            agression = (TraitLevel)rng.Next(-2, 3),
            age = (uint)rng.Next(minAge * 12, (maxAge + 1) * 12),
            simManager = this
        };
        pop.AddCharacter(character);
        if (family != null){
            family.AddCharacter(character);                
        }
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
            character.pop.RemoveCharacter(character);
            character.state.RemoveCharacter(character);
            if (character.family != null){
                character.family.RemoveCharacter(character);                
            }
            if (character.parent != null){
                character.parent.children.Remove(character);
            }

            characters.Remove(character);            
        } catch (Exception e) {
            GD.PushError(e);
        }

    }
    #endregion
    
    #region Map Stuff
    public void SetMapMode(MapModes mode){
        mapMode = mode;
        foreach (Region region in regions){
            SetRegionColor(region.pos.X, region.pos.Y, GetRegionColor(region));
        }
        // Parallel.ForEach(regions, region =>{
        //     SetRegionColor(region.pos.X, region.pos.Y, GetRegionColor(region));
        // });
    }

    public Color GetRegionColor(Region region){
        Color color = new Color(0, 0, 0, 0);
        switch (mapMode){
            case MapModes.POLITIY:  
                if (region.pops.Count > 0){
                    color = new Color(0.2f, 0.2f, 0.2f);
                }
                if (region.owner != null){
                    color = region.owner.color;
                    if (region.border || region.frontier){
                        color = (color * 0.8f) + (new Color(0, 0, 0) * 0.2f);
                    }
                    if (region.owner.capital == region){
                        color = new Color(1,0,0);
                    }
                }
            break;
            case MapModes.POPULATION:
                if (region.habitable && region.pops.Count > 0){
                    color = new Color(0, (float)region.population/Pop.ToNativePopulation(1000 * (int)Mathf.Pow(tilesPerRegion, 2)), 0, 1);
                } else if (region.habitable) {
                    color = new Color(0, 0, 0, 1);
                }
            break;
            case MapModes.CULTURE:
                if (region.habitable && region.pops.Count > 0){
                    color = region.pops[0].culture.color;
                } else if (region.habitable) {
                    color = new Color(0, 0, 0, 1);
                }
            break;
            case MapModes.POPS:
                if (region.habitable && region.pops.Count > 0){
                    color = new Color(0, 0,(float)region.pops.Count/10, 1);
                } else if (region.habitable) {
                    color = new Color(0, 0, 0, 1);
                }
                
            break;
        }
        return color;
    }

    public void SetRegionColor(int x, int y, Color color){
        if (regionImage.GetPixel(x,y) != color){
            regionImage.SetPixel(x, y, color);
            mapUpdate = true;            
        }
    }

    public void UpdateMap(){
        mapUpdate = false;
        regionOverlay.Texture = ImageTexture.CreateFromImage(regionImage);
    }
    #endregion
}

public enum MapModes {
    POLITIY,
    POPULATION,
    CULTURE,
    POPS
}