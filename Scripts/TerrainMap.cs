using Godot;
using System;

public partial class TerrainMap : Node2D
{
    Sprite2D terrainMap;
    [Export] bool switchMap;
    bool isBiomeMap = false;
    public override void _Ready()
    {
        terrainMap = GetNode<Sprite2D>("Terrain Map");
    }
    public void Init()
    {
        Scale = new Vector2(1, 1) * 80f / WorldGenerator.WorldSize.X;
    }
    public override void _Process(double delta)
    {
        if (switchMap)
        {
            switchMap = false;
            if (isBiomeMap)
            {
                isBiomeMap = false;
                SetMapImageTexture(WorldGenerator.GetTerrainImage(true));
            } else {
                isBiomeMap = true;
                SetMapImageTexture(WorldGenerator.GetTerrainImage(false));
            }
        }
    }

    public void SetMapImageTexture(Image newTexture)
    {
        terrainMap.Texture = ImageTexture.CreateFromImage(newTexture);
    }
}
