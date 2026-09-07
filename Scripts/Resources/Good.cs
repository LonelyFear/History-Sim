using System.Linq;
using Godot;

[GlobalClass]
public partial class Good : SimResource
{
    [Export] public string name = "New Good";
    [Export(PropertyHint.MultilineText)] public string description = "A new good.";
    
    [ExportCategory("Trade")]
    [Export(PropertyHint.Range, "0.0,1000.0")] public float baseValue = 1.0f;
    [Export] public bool tradeable = true;
    [Export] public Defines.GoodsType type = Defines.GoodsType.NONE;
}