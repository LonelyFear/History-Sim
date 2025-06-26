using Godot;
using System;

public partial class TerrainMap : Node2D
{
    Sprite2D terrainMap;
    public override void _Ready()
    {
        terrainMap = GetNode<Sprite2D>("Terrain Map");
    }
    public void Init()
    {
        Scale = new Vector2(1, 1) * 80f / WorldGenerator.WorldSize.X;
    }
    public void SetMapImageTexture(Image newTexture)
    {
        terrainMap.Texture = ImageTexture.CreateFromImage(newTexture);
    }
}
