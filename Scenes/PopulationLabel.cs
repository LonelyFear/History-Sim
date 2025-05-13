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
        if (simManager != null){
            Text = "World Population: " + (simManager.worldPopulation/simManager.simToPopMult).ToString("#,##0") + "\nTotal Pops: " + simManager.pops.Count.ToString("#,##0") + "\nTotal Characters: " + simManager.characters.Count.ToString("#,##0");
        } else {
            Text = "";
        }
    }

}
