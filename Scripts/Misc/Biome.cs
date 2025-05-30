using Godot;
using System;
using System.Collections.Generic;

public class Biome
{
    public string id { get; set; }
    public string[] mergedIds { get; set; } 
    public float fertility { get; set; } 
    public string color { get; set; } 
    public int textureX { get; set; } 
    public int textureY { get; set; } 
    public TerrainType terrainType { get; set; }
    //public List<Crop> crops = new List<Crop>();
    public enum TerrainType
    {
        LAND,
        WATER,
        ICE
    } 
}

