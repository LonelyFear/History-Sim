using Godot;

[GlobalClass]
public partial class ResourceDeposit: SimResource
{
    [Export] public NaturalResource resource;
    [Export] float maxAmount = 0;
    [Export] float minAmount = 0;
}