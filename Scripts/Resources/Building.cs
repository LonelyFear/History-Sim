using Godot;

[GlobalClass]
public partial class Building : Resource
{
    [Export] public string name = "Building";
    [Export] public int employees;
    [Export] public SocialClass profession;
    [Export] public BuildingOutput[] outputs;
}