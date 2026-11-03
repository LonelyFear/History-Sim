using Godot;
using System;
using System.Collections.Generic;

public partial class DebugPanel : Panel
{
	[Export] TimeManager timeManager;
	[Export] SimManagerHolder simHolder;
	[Export] double updateDelay = 0.1;

    SimManager simManager;
	Button closeButton;
	Label stepTimeLabel;
	Label tpsLabel;

	MenuButton popTimeMenu;
	MenuButton regionTimeMenu;
	MenuButton stateTimeMenu;
	MenuButton miscTimeMenu;
	
	double currentTime = 0;
	public override void _Ready()
	{
		tpsLabel = GetNode<Label>("%TPSLabel");
		closeButton = GetNode<Button>("%CloseButton");
		stepTimeLabel = GetNode<Label>("%StepTimeLabel");
		popTimeMenu = GetNode<MenuButton>("%PopTimeMenu");
		regionTimeMenu = GetNode<MenuButton>("%RegionTimeMenu");
		stateTimeMenu = GetNode<MenuButton>("%StateTimeMenu");
		miscTimeMenu = GetNode<MenuButton>("%MiscTimeMenu");
		
		closeButton.Pressed += () => Visible = false;
	}
    public override void _Process(double delta)
    {
		if (Input.IsActionJustPressed("Toggle_Debug"))
		{
			Visible = !Visible;
		}		
		if (!Visible) return;

		currentTime -= delta;
		if (currentTime <= 0)
		{
			currentTime = updateDelay;

			UpdateTPSLabel(delta);

			simManager = simHolder.simManager;
			if (simManager != null)
			{

					currentTime = updateDelay;
					stepTimeLabel.Text = $"Total Step Time: {simManager.totalStepTime:#,##0.0ms}";

					SetPerformanceMenuText(popTimeMenu, "Pops Time", "Pops", simManager.popsPerformanceInfo);

					SetPerformanceMenuText(regionTimeMenu, "Regions Time", "Regions", simManager.regionPerformanceInfo);

					SetPerformanceMenuText(stateTimeMenu, "States Time", "States", simManager.statePerformanceInfo);

					SetPerformanceMenuText(miscTimeMenu, "Misc Time", "Misc", simManager.miscPerformanceInfo);
			}
		}
    }
	void SetPerformanceMenuText(MenuButton menu, string menuTitle, string menuKey, Dictionary<string, double> performanceInfo)
	{
		menu.Text = $"{menuTitle} ({simManager.stepPerformanceInfo[menuKey]:#,##0.0ms})";
		foreach (var pair in performanceInfo)
		{
			int index = Array.IndexOf([..performanceInfo.Keys], pair.Key);
			string newText = $"{pair.Key}: " + pair.Value.ToString("#,##0.0ms\n");
			if (menu.GetPopup().ItemCount > performanceInfo.Count)
			{
				menu.GetPopup().SetItemText(index, newText);
			} else
			{
				menu.GetPopup().AddItem(newText);
			}
		}		
	}
	void UpdateTPSLabel(double delta)
	{
		tpsLabel.Text = $"TPS: {1f/timeManager.tickDelta:0.#}\n";
		tpsLabel.Text += $"FPS: {1f/delta:0.#}\n";
	}
}
