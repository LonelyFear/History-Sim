using Godot;
using System;
using System.Threading.Tasks;

public partial class LoadingScreen : Control
{
    [Export] StreamlineRenderer streamlineRenderer;
    Task task;
    SimNodeManager simNodeManager;
    SimManager sim = new SimManager();
    TimeManager time;
    GameUI ui;
    Label splash;
    PlayerCamera camera;

    public int seed;
    public int worldMult;
    public bool useEarthHeightmap;
    bool textureGenerated;
    bool firstFrame = false;
    public string savePath = null;
    TerrainMap map;
    
    public WorldGenerator generator;
    public override void _Ready()
    {
        map = GetNode<TerrainMap>("/root/Game/Terrain Map");
        time = GetNode<TimeManager>("/root/Game/Time Manager");
        simNodeManager = GetNode<SimNodeManager>("/root/Game/Simulation");
        ui = GetNode<GameUI>("/root/Game/UI");
        splash = GetNode<Label>("Splash Text");
        camera = GetNode<PlayerCamera>("/root/Game/PlayerCamera");
        ui.forceHide = true;
        Visible = true;
        GetNode<TextureProgressBar>("ProgressBar").Value = 0;
        AssetManager.LoadMods();
	}
    public override void _Process(double delta)
    {
        try
        {
            if (!firstFrame)
            {
                if (savePath != null && DirAccess.Open(savePath) != null)
                {
                    WorldGenerator loadedWorld = WorldGenerator.LoadFromSave(savePath);
                    SimManager loadedSim = SimManager.LoadSimFromFile(savePath);

                    if (loadedWorld != null)
                    {
                        generator = loadedWorld;
                        if (loadedSim != null)
                        {
                            sim = loadedSim;
                        }                    
                    }
                }
                firstFrame = true;

                simNodeManager.simManager = sim;
                sim.node = simNodeManager;
                sim.terrainMap = GetNode<Node2D>("/root/Game/Terrain Map");
                sim.timeManager = GetParent().GetNode<TimeManager>("/root/Game/Time Manager");
                // Connection
                //giveGeneratorEvent.Invoke(this, null);

                generator.worldgenFinishedEvent += sim.OnWorldgenFinished;
                sim.worldGenerator = generator;
                map.world = generator;            
            }

            if (task == null && generator.WorldExists == false)
            {
                task = Task.Run(generator.GenerateWorld);
            }

            float tileCount = generator.WorldSize.X * generator.WorldSize.Y;
            GetNode<TextureProgressBar>("ProgressBar").Value = (int)generator.Stage/10f * 100;
            splash.Text = generator.Stage switch
            {
                WorldGenStage.CONTINENTS => "Forming Continents...",
                WorldGenStage.MEASURING => "Measuring Terrain...",
                WorldGenStage.TECTONICS => "Raising Mountains...",
                WorldGenStage.EROSION => "Eroding Continents...",
                WorldGenStage.WIND => "Blowing Winds...",
                WorldGenStage.TEMPERATURE => "Heating Planet...",
                WorldGenStage.SUMMER_RAINFALL => "Forming Clouds...",
                WorldGenStage.WINTER_RAINFALL => "Snowing Snow...",
                WorldGenStage.BIOMES => "Seeding Biomes...",
                WorldGenStage.RIVERS => "Carving Rivers...",
                _ => "Settling World...",
            };
            //splash.Text = "Generating World";


            if ((task == null || task.IsCompleted) && generator.WorldExists && !textureGenerated)
            {
                textureGenerated = true;
                splash.Text = "Finishing Up...";
                map.Init();
                try
                {
                    map.SetMapImageTexture(generator.GetTerrainImage(TerrainMapMode.KOPPEN));
                    streamlineRenderer.world = generator;
                    streamlineRenderer.QueueRedraw();
                } catch (Exception e)
                {
                    GD.PushError(e);
                }
                
            }
            else if (task == null || task.IsCompleted)
            {
                generator.FinishWorldgen();
                camera.controlEnabled = true;
                ui.forceHide = false;
                QueueFree();
                //GD.Print("I shouldnt show up");
            }            
        } catch (Exception e)
        {
            GD.PushError(e);
        }
    }
}
