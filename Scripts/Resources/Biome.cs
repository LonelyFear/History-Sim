using Godot;

[GlobalClass]
public partial class Biome : SimResource
{
    [Export] public BiomeType type;

    [ExportCategory("Constraints")]
    [ExportGroup("Elevation")]
    [Export(PropertyHint.Range, "-10000, 10000, 1")] public int maxElevation = 10000;
    [Export(PropertyHint.Range, "-10000, 10000, 1")] public int minElevation = -10000;
    [ExportGroup("Moisture")]
    [Export(PropertyHint.Range, "0, 4000, 1")] public int maxMoisture = 4000;
    [Export(PropertyHint.Range, "0, 4000, 1")] public int minMoisture = 0;
    [ExportGroup("Temperature")]
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

