using Godot;
using System;

public partial class PopulationLabel : Label
{
    [Export]
    SimManager simManager;

    public override void _Ready()
    {
        simManager = GetNode<SimManager>("/root/Game/Simulation");
    }

    public override void _Process(double delta)
    {
        if (simManager != null)
        {
            Text = "World Population: " + (simManager.worldPopulation / Pop.simPopulationMultiplier).ToString("#,##0\n");
            Text += "Total Pops: " + simManager.pops.Count.ToString("#,##0") + "\nTotal Characters: " + simManager.characters.Count.ToString("#,##0\n");
            Text += "Populated Regions: " + simManager.populatedRegions.ToString("#,##0\n");
            Text += "Wars: " + simManager.wars.Count.ToString("#,##0");
        }
        else
        {
            Text = "";
        }
    }

}
