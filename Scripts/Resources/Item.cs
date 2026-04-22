using Godot;

[GlobalClass]
public partial class Item : Resource
{
    [Export] public string name = "Item";
    [Export(PropertyHint.MultilineText)] public string description = "A base item";
    [Export] public float tradeWeight;
    [Export(PropertyHint.Range, "0.0,1000.0")] public float baseCost;
    [Export] public string[] tags;
}
