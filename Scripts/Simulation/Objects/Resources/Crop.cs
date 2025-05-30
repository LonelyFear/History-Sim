using System.Collections.Generic;

public class Crop
{
    public string name {get; set;} = "Crop";
    public string id {get; set;} = "crop";
    public float maxFertility {get; set;} = 1.0f;
    public float minFertility {get; set;} = 0f;
    public Dictionary<BaseResource, float> yields {get; set;}
}