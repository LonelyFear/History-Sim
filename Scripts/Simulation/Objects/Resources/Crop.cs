using System.Collections.Generic;

public class Crop
{
    public string name {get; set;} = "Crop";
    public string id {get; set;} = "crop";
    public float maxTemperature {get; set;} = 40;
    public float minTemperature {get; set;} = -10;
    public float maxRainfall { get; set; } = 4000;
    public float minRainfall { get; set; } = 0;
    public Dictionary<BaseResource, float> yields { get; set; }
}