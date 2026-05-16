using Godot;

[GlobalClass]
public partial class Building : SimResource
{
    [Export] public string name = "New Sim Resource";
    [Export] public BuildingType type;

    [ExportCategory("Employment")]
    [Export] public int employees;
    [Export] public SocialClass socialClass;    

    [ExportCategory("Requirements")]
    [ExportGroup("Tech")]
    [Export] public int minSocietyLevel = 0;
    [Export] public int minMilitaryLevel = 0;
    [Export] public int minIndustryLevel = 0;

    [ExportGroup("Resources")]
    [Export] Item[] requiredNaturalResources = [];
    

    [ExportCategory("Output")]
    [Export] int minOutput = 0;
    [Export] int maxOutput = 0;    
    [Export] public BuildingOutput[] outputs = [];
}
    public enum BuildingType
    {
        CIVIL,
        HARVESTING,
        INDUSTRY
    }
