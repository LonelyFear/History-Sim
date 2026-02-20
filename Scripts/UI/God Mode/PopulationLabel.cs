using Godot;
using System;

public partial class PopulationLabel : Label
{
    SimManager simManager;

    public override void _Ready()
    {
		GetNode<SimNodeManager>("/root/Game/Simulation").simStartEvent += OnSimStart;
	}

    public void OnSimStart()
    {
        simManager = GetNode<SimNodeManager>("/root/Game/Simulation").simManager;
	}

    public override void _Process(double delta)
    {
        if (simManager != null)
        {
            Text = "World Population: " + (simManager.worldPopulation / Pop.simPopulationMultiplier).ToString("#,##0\n");
            Text += "Total Events: " + simManager.historicalEventIds.Count.ToString("#,##0\n");
            Text += "Total Characters: " + simManager.characterIds.Count.ToString("#,##0\n");
            Text += "Populated Regions: " + simManager.populatedRegions.ToString("#,##0\n");
            Text += "Total Pops: " + simManager.popsIds.Count.ToString("#,##0\n");
            Text += "Wars: " + simManager.warIds.Count.ToString("#,##0");
        }
        else
        {
            Text = "";
        }
    }

}
