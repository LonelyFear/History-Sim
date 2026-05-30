using Godot;

[GlobalClass]
public partial class Profession : SimResource
{
    [Export] public string name = "New Sim Resource";
    [Export(PropertyHint.MultilineText)] public string description = "The base socialClass.";
    [ExportCategory("Statistics")]
    [Export] public float politicalPower;
    [ExportCategory("Needs")]
    [Export] public PopNeeds[] needs = [];
}
