using Godot;

[GlobalClass]
public partial class NaturalResource : SimResource
{
    [Export] public new string name = "New Natural Resource";
    [Export(PropertyHint.MultilineText)] public string description = "This is a natural resource";
}