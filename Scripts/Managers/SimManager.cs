using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class SimManager : Node2D
{
    [Export]
    public Node world {private set; get;}
    [Export]
    public int tilesPerRegion {private set; get;} = 4;
    Sprite2D regionOverlay;
    [Export]
    public TimeManager timeManager {private set; get;}

    public Dictionary<Vector2I,Tile> tiles = new Dictionary<Vector2I, Tile>();
    public Array<Region> regions = new Array<Region>();
    public Vector2I terrainSize;   
    public Vector2I worldSize;

    Image regionImage;

    // Population
    public Array<Pop> pops = new Array<Pop>();
    public long worldPopulation = 0;
    public long worldWorkforce = 0;
    public long worldDependents = 0;
    public Array<Culture> cultures = new Array<Culture>();

    public int maxPopsPerRegion = 60;
    public bool mapUpdate = false;
    public long popTaskId = 0;

    int currentBatch = 0;
    int month = 1;

    public Task task;
    public override void _Ready()
    {
        regionOverlay = GetNode<Sprite2D>("RegionOverlay");
        world = GetParent().GetNode<Node2D>("World");
        timeManager = GetParent().GetNode<TimeManager>("Time Manager");

        // Connection
        world.Connect("worldgenFinished", new Callable(this, nameof(OnWorldgenFinished)));
        timeManager.Tick += OnTick;
    }
    
    private void OnWorldgenFinished(){
        terrainSize = (Vector2I)world.Get("worldSize");
        worldSize = terrainSize/tilesPerRegion;
        Scale = (Vector2I)world.Get("Scale") * tilesPerRegion;
        regionImage = Image.CreateEmpty(worldSize.X, worldSize.Y, true, Image.Format.Rgba8);

        for (int x = 0; x < terrainSize.X; x++){
            for (int y = 0; y < terrainSize.Y; y++){
                Tile newTile = new Tile();
                tiles[new Vector2I(x,y)] = newTile;
                newTile.biome = (Dictionary)((Dictionary)world.Get("tileBiomes"))[new Vector2I(x,y)];
            }
        }

        for (int x = 0; x < worldSize.X; x++){
            for (int y = 0; y < worldSize.Y; y++){
                regionImage.SetPixel(x, y, Color.Color8(0,0,0,0));
                // Creates a region
                Region newRegion = new Region();
                newRegion.simManager = this;
                newRegion.pos = new Vector2I(x,y);
                regions.Add(newRegion);
                for (int tx = 0; tx < tilesPerRegion; tx++){
                    for (int ty = 0; ty < tilesPerRegion; ty++){
                        // Adds subregion to tile
                        Tile tile = tiles[new Vector2I(x * tilesPerRegion + tx, y * tilesPerRegion + ty)];
                        newRegion.tiles.Add(new Vector2I(tx, ty), tile);
                        // Adds biomes to tile
                        newRegion.biomes.Add(new Vector2I(tx, ty), tile.biome);
                        if ((int)tile.biome["terrainType"] == 0){
                            newRegion.claimable = true;
                        }

                    }
                }
                // Calc average fertility
                newRegion.CalcAvgFertility();
                // Calc max populaiton
                newRegion.CalcMaxPopulation();
                // Adds pops
                if (newRegion.claimable){
                    // Add pops here
                    for (int i = 0; i < maxPopsPerRegion; i++){
                        long startingPopulation = Pop.toNativePopulation(2);
                        CreatePop((long)(startingPopulation * 0.25f), (long)(startingPopulation * 0.75f), newRegion, new Tech(), CreateCulture(newRegion));
                    }
                }
            }
        }
            
    }

    public void OnTick(){
        month = (int)timeManager.Get("month");
        task = Parallel.ForEachAsync(regions, (region, ct) =>
        {
            region.currentMonth = month;
            region.growPops();
            return new ValueTask();
        });
    }

    #region Pops
    public Pop CreatePop(long workforce, long dependents, Region region, Tech tech, Culture culture, Professions profession = Professions.TRIBESPEOPLE){
        currentBatch += 1;
        if (currentBatch > 12){
            currentBatch = 1;
        }

        Pop pop = new Pop();
        pop.batchId = currentBatch;

        region.addPop(pop);
        pops.Add(pop);
        culture.AddPop(pop);

        pop.tech = new Tech();
        pop.tech.militaryLevel = tech.militaryLevel;
        pop.tech.societyLevel = tech.societyLevel;
        pop.tech.industryLevel = tech.industryLevel;

        pop.changeWorkforce(workforce);
        pop.changeDependents(dependents);

        return pop;
    }
    #endregion

    public Culture CreateCulture(Region region){
        Culture culture = new Culture();
        culture.name = "Culturism";
        float r = Mathf.InverseLerp(0, worldSize.X, region.pos.X);
        float g = Mathf.InverseLerp(0, worldSize.Y, region.pos.Y);
        float b = Mathf.InverseLerp(0, worldSize.Y, region.pos.Y - worldSize.Y);
        culture.color = new Color(r,g,b);
        cultures.Append(culture);

        return culture;
    }
}
