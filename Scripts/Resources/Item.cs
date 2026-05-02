using Godot;

[GlobalClass]
public partial class Item : SimResource
{
    [Export] public string name = "Item";
    [Export(PropertyHint.MultilineText)] public string description = "A base item";
    [ExportCategory("Trade")]
    [Export] public float tradeValue;
    [Export(PropertyHint.Range, "0.0,1000.0")] public float basePrice = 1;
    [ExportCategory("Tags")]
    [Export] public string[] tags;
}
