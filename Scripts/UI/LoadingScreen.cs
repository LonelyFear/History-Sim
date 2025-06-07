using Godot;
using System;
using System.Threading.Tasks;

public partial class LoadingScreen : Control
{
    Task task;
    WorldGeneration world;
    SimManager sim;
    CanvasLayer ui;
    Label splash;
    Camera2D camera;

    public int seed;
    public int tilesPerRegionFactor;
    public int worldSizeFactor;
    public override void _Ready()
    {
        world = GetNode<WorldGeneration>("/root/Game/WorldGeneration");
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
            world.seed = seed;
            sim.tilesPerRegion *= tilesPerRegionFactor;
            world.Init();
            task = Task.Run(world.GenerateWorld);
        }

        float tileCount = world.worldSize.X * world.worldSize.Y;
        GetNode<TextureProgressBar>("ProgressBar").Value = (world.heightMapProgress/tileCount * 100f) + (world.tempMapProgress/tileCount * 25f) + (world.moistMapProgress/tileCount * 25f) + (world.preparationProgress/tileCount * 50f);
        switch (world.worldGenStage){
            case 1:
                splash.Text = "Colliding Plates...";
            break;
            case 2:
                splash.Text = "Heating Planet...";
            break;
            case 3:
                splash.Text = "Forming Clouds...";
            break;
            case 4:
                splash.Text = "Seeding Forests...";
            break;
            case 5:
                splash.Text = "Carving Rivers...";
                break;
            default:
                splash.Text = "Finishing Up...";
            break;
        }
        //splash.Text = "Generating World";
        
        if (task.IsCompleted && !world.worldCreated && !world.startedColoring){
            splash.Text = "Finishing Up...";
            world.ColorMap();
        } else if (task.IsCompleted){
            camera.Set("controlEnabled", true);
            ui.Visible = true;
            QueueFree();            
        }
    }
}
