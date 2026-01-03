using MessagePack;

[MessagePackObject(keyAsPropertyName: true)]
public class PlantType
{
    public string id {get; set;}
    public float minColdTemp {get; set;} = float.MinValue;
    public float maxColdTemp {get; set;} = float.MaxValue;
    public int minGDD {get; set;} = int.MinValue;
    public int minGDDz {get; set;} = int.MinValue;
    public float minWarmTemp {get; set;} = float.MinValue;
    public float minA {get; set;} = float.MinValue;
    public float maxA {get; set;} = float.MaxValue;
    public int dominance {get; set;} = int.MaxValue;
}