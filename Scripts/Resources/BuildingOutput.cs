using Godot;

public partial class BuildingOutput : Resource
{
    [Export] public Item output;
    [Export] public Item[] inputs;
    [Export] public float weight;
}