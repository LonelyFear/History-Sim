using Godot.Collections;
using Godot;

[GlobalClass]
public partial class Building : SimResource
{
    [Export] public new string name = "New Building";
    [Export(PropertyHint.MultilineText)] public string description = "A building.";
    [ExportCategory("Requirements")]
    [ExportGroup("Tech")]
    [Export(PropertyHint.Range, "0,20,1")] public int minSocTech = 0;
    [Export(PropertyHint.Range, "0,20,1")] public int minSciTech = 0;    
    [Export(PropertyHint.Range, "0,20,1")] public int minIndTech = 0;
    [Export(PropertyHint.Range, "0,20,1")] public int minMilTech = 0;
    
    [ExportGroup("")]
    [Export] public Dictionary<NaturalResource, float> naturalResources = [];
    [ExportCategory("Tech")]
    [Export] public Defines.BuildingTechScale productionTechScaling;
    [Export] public Curve preIndustrialMultiplierCurve;
    [Export] public bool useIndustrialScaling = false;
    [Export] public Curve industrialMultiplierCurve;

    [ExportCategory("Employment")]
    [Export] public bool employmentRequired = true;
    [Export] public SocialClass workerClass;
    [Export] public int workersPerLevel = 400;
}