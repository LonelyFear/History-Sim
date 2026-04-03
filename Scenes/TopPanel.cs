using Godot;
using System;

public partial class TopPanel : Panel
{
	[Export] Label dateLabel;
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

        if (dateLabel != null)
        {
            dateLabel.Text = $"Year {timeManager.GetYear()}";
        }
    }
}
