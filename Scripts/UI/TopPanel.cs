using Godot;
using System;

public partial class TopPanel : Panel
{
    [ExportCategory("Top Bar")]
	[Export] Label dateLabel;

    [ExportCategory("References")]
	[Export] SimManagerHolder simHolder;
	SimManager simManager;
    [Export] TimeManager timeManager;
    bool update = false;
    public override void _Ready()
    {
        // Connection
		simHolder.simStartEvent += OnSimStart;
	}
    public void OnSimStart()
    {   
        update = true;
        simManager = simHolder.simManager;
	}
    public override void _Process(double delta)
    {
        if (!update) return;

        dateLabel.Text = $"Year {TimeManager.GetYear(timeManager.ticks)}";
    }
}
