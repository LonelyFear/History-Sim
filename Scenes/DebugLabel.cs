using Godot;
using System;

public partial class DebugLabel : Label
{
    [Export] SimManager sim;
    public override void _Process(double delta)
    {
        Position = GetGlobalMousePosition();
        string mouseText = "Mouse Position: " + sim.mousePos;
        string tilePosText = "Region Position: " + sim.hoveredRegionPos;
        string regionPopulation = "";
        string pops = "";
        if (sim.hoveredRegion != null){
            regionPopulation = "Region Population: " + Pop.fromNativePopulation(sim.hoveredRegion.population);
            pops = "Region Pops: " + sim.hoveredRegion.pops.Count;
        }
        Text = mouseText + "\n" + tilePosText + "\n" + "Region Data:" + "\n" + regionPopulation + "\n" + pops;
    } 
}
