using Godot;
using Godot.Collections;

[GlobalClass]
public partial class BuildingOutput : Resource
{
    [Export] public Item output;
    [Export] public Dictionary<Item, float> inputs = [];
    [Export] public float amount;
}