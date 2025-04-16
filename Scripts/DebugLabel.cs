using Godot;
using System;

public partial class DebugLabel : Label
{
    [Export] SimManager sim;
    public override void _Process(double delta)
    {
        Position = GetGlobalMousePosition();
        Region region = sim.hoveredRegion;
        if (region != null && region.owner != null){
            State state = region.owner;
            Text = "State: " + state.name + "\n" + "Population: " + Pop.FromNativePopulation(state.population).ToString("#,###0") + "\n" + "Manpower: " + Pop.FromNativePopulation(state.manpower).ToString("#,###0");
        } else {
            Text = "";
        }
        
    } 
}
