using Godot;

[GlobalClass]
public partial class PopNeeds : Resource
{
    [Export] public NeedsType type = NeedsType.NEED_FOOD;
    [Export] public float demandPerWorker = 0;
    [Export] public float demandPerDependent = 0;
}
public enum NeedsType
{
    NEED_FOOD,
    NEED_HEATING,
    NEED_CLOTHES,
    NEED_LUXURIES,
    NEED_TOOLS,
    NEED_DRINK,
    NEED_LUXURY_GOODS,
}