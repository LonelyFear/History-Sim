using Godot;

[GlobalClass]
public partial class Biome : Resource
{
    [Export] public string name = "Ice Sheet";
    [Export] public BiomeType type;

    [ExportCategory("Constraints")]
    [Export(PropertyHint.Range, "-10000, 10000, 1")] public int maxElevation = 10000;
    [Export(PropertyHint.Range, "-10000, 10000, 1")] public int minElevation = -10000;
    [Export(PropertyHint.Range, "0, 4000, 1")] public int maxMoisture = 4000;
    [Export(PropertyHint.Range, "0, 4000, 1")] public int minMoisture = 0;
    [Export(PropertyHint.Range, "-40, 40, 0.5")] public float maxTemperature = 40;
    [Export(PropertyHint.Range, "-40, 40, 0.5")]  public float minTemperature = -40;

    [ExportCategory("Stats")]
    [Export(PropertyHint.Range, "0, 1, 0.01")] public float navigability = 0.0f;
    [Export(PropertyHint.Range, "0, 1, 0.01")] public float arability = 0.0f;
    [Export(PropertyHint.Range, "0, 1, 0.01")] public float survivability = 0.0f;
    [Export] public Color color = new("FFFFFF");

    public enum BiomeType
    {
        ICE,
        WATER,
        LAND
    }
}

