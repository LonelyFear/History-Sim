using Godot;

[GlobalClass]
public partial class Profession : SimResource
{
    [Export(PropertyHint.MultilineText)] public string description = "The base profession.";
    [ExportCategory("Needs")]
    [Export] public PopNeeds[] needs = [];
}
