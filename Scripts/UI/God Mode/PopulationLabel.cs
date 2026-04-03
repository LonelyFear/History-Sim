using Godot;
using System;

public partial class PopulationLabel : Label
{
    SimManager simManager;
    [Export] SimManagerHolder simHolder;
    public override void _Ready()
    {
		simHolder.simStartEvent += OnSimStart;
	}

    public void OnSimStart()
    {
        simManager = simHolder.simManager;
	}

    public override void _Process(double delta)
    {
        if (simManager != null)
        {
            Text = "World Population: " + simManager.worldPopulation.ToString("#,##0\n");
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
