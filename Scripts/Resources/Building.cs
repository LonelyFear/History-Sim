using Godot;

[GlobalClass]
public partial class Building : SimResource
{
    [Export] public string name = "New Sim Resource";
    [Export(PropertyHint.MultilineText)] public string description = "A building";
    [Export] public BuildingType type;

    [ExportCategory("Requirements")]
    [Export] public NaturalResource requiredNaturalResource;
    [ExportGroup("Tech")]
    [Export(PropertyHint.Range, "0,20,1")] public int minSocietyLevel = 0;
    [Export(PropertyHint.Range, "0,20,1")] public int minMilitaryLevel = 0;
    [Export(PropertyHint.Range, "0,20,1")] public int minIndustryLevel = 0;
    [ExportCategory("Production")]
    //[Export] public Profession[] professions = [];
    [Export] public bool scalesWithResource = false;
    [Export(PropertyHint.Range, "0,2,0.01")] public float populationFactor = 0;
    [Export] public BuildingOutput[] outputs;

    public bool Teched(Tech tech)
    {
        return tech.societyLevel >= minSocietyLevel && tech.militaryLevel >= minMilitaryLevel && tech.industryLevel >= minIndustryLevel;
    }
}
    public enum BuildingType
    {
        CIVIL,
        PRIMARY_INDUSTRY,
        SECONDARY_INDUSTRY
    }
