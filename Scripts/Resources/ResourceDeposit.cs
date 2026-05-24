using Godot;

[GlobalClass]
public partial class ResourceDeposit: SimResource
{
    [Export] public NaturalResource resource;
    [Export] public float maxAmount = 0;
    [Export] public float minAmount = 0;
}