using Godot;
using System;

public partial class TopPanel : Panel
{
    [ExportCategory("Top Bar")]
	[Export] Label dateLabel;
    [Export] Label nameLabel;
    [Export] Label ageLabel;
    [ExportCategory("Bottom Bar")]
    [Export] Label populationLabel;
    [Export] Label warsLabel;
    [Export] Label statesLabel;
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

        dateLabel.Text = $"Year {timeManager.GetYear()}";
        nameLabel.Text = $"World of {simManager.worldName}";
        ageLabel.Text = $"Age of History";

        populationLabel.Text = $"World Population: {simManager.worldPopulation:#,##0}";
        warsLabel.Text = $"Wars: {simManager.warIds.Count:#,##0}";
        statesLabel.Text = $"States: {simManager.statesIds.Count:#,##0}";
    }
}
