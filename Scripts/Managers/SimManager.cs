using System.Threading;
using Godot;
using Godot.Collections;
using Microsoft.VisualBasic;

public partial class SimManager : Node2D
{
    [Export]
    public Node world {private set; get;}
    [Export]
    public int tilesPerRegion {private set; get;} = 4;
    Sprite2D regionOverlay;
    [Export]
    public Node timeManager {private set; get;}

    public Dictionary<Vector2I,Tile> tiles = new Dictionary<Vector2I, Tile>();
    public Array<Region> regions = new Array<Region>();
    public Vector2I terrainSize;   
    public Vector2I worldSize;

    Image regionImage;

    // Population
    public Array<Pop> pops = new Array<Pop>();
    public int worldPopulation = 0;
    public int worldWorkforce = 0;
    public int worldDependents = 0;
    public Array<Culture> cultures = new Array<Culture>();

    public int maxPopsPerRegion {private set; get;} = 15;
    public bool mapUpdate = false;
    public int popTaskId = 0;
    
    public override void _Ready()
    {
        regionOverlay = GetNode<Sprite2D>("RegionOverlay");
        GD.Print(regionOverlay);
        // Connection
        world.Connect("worldgenFinished", new Callable(this, nameof(OnWorldgenFinished)));
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
                        int startingPopulation = Pop.toNativePopulation(20);
                        CreatePop((int)(startingPopulation * 0.25f), (int)(startingPopulation * 0.75f), newRegion, new Tech(), new Culture());
                    }
                }
            }
        }
            
    }

    #region Pops
    public Pop CreatePop(int workforce, int dependents, Region region, Tech tech, Culture culture, Professions profession = Professions.TRIBESPEOPLE){
        Pop pop = new Pop();

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
}
