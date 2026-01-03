using Godot;
using System;

public partial class TerrainMap : Node2D
{
    Sprite2D terrainMap;
    public WorldGenerator world;
    [Export] bool switchMap;
    bool isBiomeMap = false;
    public override void _Ready()
    {
        terrainMap = GetNode<Sprite2D>("Terrain Map");
        GetNode<SimNodeManager>("/root/Game/Simulation").simStartEvent += OnSimStart;
    }
    public void OnSimStart()
    {
        world = GetNode<SimNodeManager>("/root/Game/Simulation").simManager.worldGenerator;
	}
    public void Init()
    {
        Scale = new Vector2(1, 1) * 80f / world.WorldSize.X;
    }
    public override void _Process(double delta)
    {
        if (switchMap && world != null && world.WorldExists)
        {
            switchMap = false;
            if (isBiomeMap)
            {
                isBiomeMap = false;
                SetMapImageTexture(world.GetTerrainImage(TerrainMapMode.HEIGHTMAP_REALISTIC));
            } else {
                isBiomeMap = true;
                SetMapImageTexture(world.GetTerrainImage(TerrainMapMode.HEIGHTMAP));
            }
        }
    }

    public void SetMapImageTexture(Image newTexture)
    {
        terrainMap.Texture = ImageTexture.CreateFromImage(newTexture);
    }
}
