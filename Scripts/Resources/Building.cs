using Godot;

[GlobalClass]
public partial class Building : SimResource
{
    [Export] public string name = "New Sim Resource";
    [Export] public BuildingType type;

    [ExportCategory("Requirements")]
    
    [ExportGroup("Tech")]
    [Export(PropertyHint.Range, "0,20,1")] public int minSocietyLevel = 0;
    [Export(PropertyHint.Range, "0,20,1")] public int minMilitaryLevel = 0;
    [Export(PropertyHint.Range, "0,20,1")] public int minIndustryLevel = 0;
    [Export] public NaturalResource requiredNaturalResource;

    [ExportCategory("Production")]
    [Export] public bool scalesWithResourceAmount = false;
    [Export] public bool scalesWithWorkforce = false;
}
    public enum BuildingType
    {
        CIVIL,
        PRIMARY_INDUSTRY,
        SECONDARY_INDUSTRY
    }
