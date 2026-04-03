using Godot;
using System;

public partial class TopPanel : Panel
{
    [ExportCategory("Top Bar")]
	[Export] Label dateLabel;
    [Export] Label ageLabel;
    [ExportCategory("Bottom Bar")]
    [Export] Label populationLabel;
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
        ageLabel.Text = $"{GetAgeName()}";

        populationLabel.Text = $"World Population: {simManager.worldPopulation:#,##0}";
    }
    string GetAgeName()
    {
        string currentAge = "Age of Tribes";

        if (simManager.highestTech.industryLevel > 0)
        {
            currentAge = "Birth of Industry";
        }
        if (simManager.averageTech.fIndustryLevel > 0.2)
        {
            currentAge = "Age of Industry";
        }

        return currentAge;
    }
}
