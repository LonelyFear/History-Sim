using Godot;
using PixelHistory.Objects.States.Base;
using System;

public partial class DemographicsBox : VBoxContainer
{
	[Export] InfoHolder infoHolder;
	[Export] MapManager mapManager;
	[Export] SelectionManager selectionManager;

	[ExportCategory("Labels")]
	[Export] Label populationLabel;
	[Export] Label foundedLabel;
	[Export] Label ageLabel;
	[Export] Label strengthLabel;
	[Export] Label fertilityLabel;
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
		
		NamedObject obj = infoHolder.selectedObject;
		if (obj is Alliance a && a.type == AllianceType.REALM)
		{
			obj = a.leadState;
		}

		if (obj != null)
		{
			int years = (int)TimeManager.GetYear(obj.GetAge());
			int months = (int)TimeManager.GetMonth(obj.GetAge());
			
			ageLabel.Text = $"Age: {years} Year{(years == 1 ? "" : "s")}, {months} Month{(months == 1 ? "" : "s")}";			
		}

		strengthLabel.Visible = false;
		if (infoHolder.selectedObject is Polity polity)
		{
			strengthLabel.Visible = true;
			strengthLabel.Text = $"Strength: {polity.armyPower}";			
		}

		fertilityLabel.Visible = false;
		if (infoHolder.selectedObject is Region region)
		{
			fertilityLabel.Visible = true;
			fertilityLabel.Text = $"Fertility: {region.fertility:0%}";
			fertilityLabel.Text += $"\nNavigability: {region.navigability:0%}";
		}
		

		techLabel.Visible = false;
	}
}
