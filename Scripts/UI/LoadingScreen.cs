using Godot;
using System;
using System.Threading.Tasks;

public partial class LoadingScreen : Control
{
    Task task;
    SimNodeManager simNodeManager;
    SimManager sim = new SimManager();
    TimeManager time;
    CanvasLayer ui;
    Label splash;
    Camera2D camera;

    public int seed;
    public int worldMult;
    bool textureGenerated;
    bool firstFrame = false;
    public string savePath = null;
    TerrainMap map;

    public static WorldGenerator generator;
    public override void _Ready()
    {
        generator = new WorldGenerator();
        map = GetNode<TerrainMap>("/root/Game/Terrain Map");
        time = GetNode<TimeManager>("/root/Game/Time Manager");
        simNodeManager = GetNode<SimNodeManager>("/root/Game/Simulation");
        ui = GetNode<CanvasLayer>("/root/Game/UI");
        splash = GetNode<Label>("Splash Text");
        camera = GetNode<Camera2D>("/root/Game/PlayerCamera");
        ui.Visible = false;
        Visible = true;
        GetNode<TextureProgressBar>("ProgressBar").Value = 0;
        AssetManager.LoadMods();
	}
    public override void _Process(double delta)
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
            generator.worldgenFinishedEvent += sim.OnWorldgenFinished;
            sim.worldGenerator = generator;
            time.worldGenerator = generator;
            map.world = generator;            
        }

        if (task == null && generator.WorldExists == false)
        {
            generator.WorldMult = worldMult;
            generator.Seed = seed;
            //sim.tilesPerRegion *= tilesPerRegionFactor;

            task = Task.Run(generator.GenerateWorld);
        }

        float tileCount = generator.WorldSize.X * generator.WorldSize.Y;
        GetNode<TextureProgressBar>("ProgressBar").Value = generator.Stage/5f;
        switch (generator.Stage){
            case 0:
                splash.Text = "Colliding Plates...";
            break;
            case 1:
                splash.Text = "Heating Planet...";
            break;
            case 2:
                splash.Text = "Forming Clouds...";
            break;
            case 3:
                splash.Text = "Seeding Forests...";
            break;
            case 4:
                splash.Text = "Carving Rivers...";
                break;
            default:
                splash.Text = "Finishing Up...";
            break;
        }
        //splash.Text = "Generating World";

        if ((task == null || task.IsCompleted) && generator.WorldExists && !textureGenerated)
        {
            textureGenerated = true;
            splash.Text = "Finishing Up...";
            map.Init();
            map.SetMapImageTexture(generator.GetTerrainImage());
        }
        else if (task == null || task.IsCompleted)
        {
            generator.FinishWorldgen();
            camera.Set("controlEnabled", true);
            ui.Visible = true;
            QueueFree();
        }
    }
}
