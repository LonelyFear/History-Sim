using Godot;
using System;

public partial class DebugLabel : Label
{
    [Export] SimManager sim;
    public override void _Process(double delta)
    {
        Position = GetGlobalMousePosition();
        Region region = sim.hoveredRegion;
        if (region != null){
            Text = "Food: " + region.economy.AmountOfType(ResourceType.FOOD);
        } else {
            Text = "";
        }
        
    } 
}
