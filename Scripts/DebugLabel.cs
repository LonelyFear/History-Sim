using Godot;
using System;

public partial class DebugLabel : Label
{
    [Export] SimManager sim;
    public override void _Process(double delta)
    {
        Position = GetGlobalMousePosition();
        if (sim.hoveredRegion != null){
            string regionPopulation = "Region Population: " + Pop.fromNativePopulation(sim.hoveredRegion.population);
            string pops = "Region Pops: " + sim.hoveredRegion.pops.Count;
            string owner = "Owner: None";
            if (sim.hoveredState != null){
                owner = "Owner: " + sim.hoveredState.name;
            }
            Text = "Region Data:" + "\n" + regionPopulation + "\n" + pops + "\n" + owner;
        } else {
            Text = "";
        }
        Text = "Mouse Pos: " + sim.mousePos;
        
    } 
}
