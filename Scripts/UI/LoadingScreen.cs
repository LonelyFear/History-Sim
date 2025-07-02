using Godot;
using System;
using System.Threading.Tasks;

public partial class LoadingScreen : Control
{
    Task task;
    SimManager sim;
    CanvasLayer ui;
    Label splash;
    Camera2D camera;

    public int seed;
    public int tilesPerRegionFactor;
    public int worldSizeFactor;
    bool textureGenerated;
    TerrainMap map;
    
    public override void _Ready()
    {
        map = GetNode<TerrainMap>("/root/Game/Terrain Map");
        sim = GetNode<SimManager>("/root/Game/Simulation");
        ui = GetNode<CanvasLayer>("/root/Game/UI");
        splash = GetNode<Label>("Splash Text");
        camera = GetNode<Camera2D>("/root/Game/PlayerCamera");
        ui.Visible = false;
        Visible = true;
        GetNode<TextureProgressBar>("ProgressBar").Value = 0;
    }
    public override void _Process(double delta)
    {
        if (task == null){
            WorldGenerator.Seed = seed;
            sim.tilesPerRegion *= tilesPerRegionFactor;

            task = Task.Run(WorldGenerator.GenerateWorld);
        }

        float tileCount = WorldGenerator.WorldSize.X * WorldGenerator.WorldSize.Y;
        GetNode<TextureProgressBar>("ProgressBar").Value = WorldGenerator.Stage/5f;
        switch (WorldGenerator.Stage){
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

        if (task.IsCompleted && WorldGenerator.WorldExists && !textureGenerated)
        {
            textureGenerated = true;
            splash.Text = "Finishing Up...";
            map.Init();
            map.SetMapImageTexture(WorldGenerator.GetTerrainImage(true));
        }
        else if (task.IsCompleted)
        {
            WorldGenerator.FinishWorldgen();
            camera.Set("controlEnabled", true);
            ui.Visible = true;
            QueueFree();
        }
    }
}
