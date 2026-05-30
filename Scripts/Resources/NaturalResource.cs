using Godot;

[GlobalClass]
public partial class NaturalResource : SimResource
{
    [Export] public string name = "New Natural Resource";
    [Export(PropertyHint.MultilineText)] public string description = "This is a natural resource";
    [Export] public Color color = new("white");
    
    [ExportCategory("Requirements")]
    [Export] public float minFertility = 0;
}