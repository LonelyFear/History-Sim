using Godot;
using System;

public class Biome
{
    public string id { get; set; }
    public string[] mergedIds { get; set; } 
    public float fertility { get; set; } 
    public string color { get; set; } 
    public int textureX { get; set; } 
    public int textureY { get; set; } 
    public TerrainType terrainType { get; set; } 
    public enum TerrainType{
        LAND,
        WATER,
        ICE
    } 
}

