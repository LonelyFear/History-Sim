using Godot;
using System;

public partial class DemographicsBox : VBoxContainer
{
	[Export] InfoHolder infoHolder;
	[Export] MapManager mapManager;
	[Export] SelectionManager selectionManager;
	[Export] Label populationLabel;
	[Export] Label foundedLabel;
	[Export] Label ageLabel;
	[Export] Label techLabel;

	public override void _Process(double delta)
	{
		if (infoHolder.selectedObject == null) return;
		populationLabel.Text = "";
		if (infoHolder.selectedObject is PopObject popObject)
		{
			populationLabel.Text = $"Population: {popObject.population:#,##0}";
		}

		foundedLabel.Visible = !(infoHolder.selectedObject is Region);
		ageLabel.Visible = !(infoHolder.selectedObject is Region);

		foundedLabel.Text = $"Founded: Month {TimeManager.GetMonth(infoHolder.selectedObject.tickCreated)} of {TimeManager.GetYear(infoHolder.selectedObject.tickCreated)}";
		
		int years = (int)TimeManager.GetYear(infoHolder.selectedObject.GetAge());
		int months = (int)TimeManager.GetMonth(infoHolder.selectedObject.GetAge());
		ageLabel.Text = $"Age: {years} Year{(years == 1 ? "" : "s")}, {months} Month{(months == 1 ? "" : "s")}";

		techLabel.Visible = false;
	}
}
