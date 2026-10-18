using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class FrameBreakdown : VBoxContainer
{
    SimManager simManager;
	[Export] SimManagerHolder simHolder;
	[Export] Label stepTimeLabel;
	[Export] MenuButton popTimeMenu;
	[Export] MenuButton regionTimeMenu;
	[Export] MenuButton stateTimeMenu;
	[Export] MenuButton miscTimeMenu;
	[Export] double updateDelay = 0.1;
	double currentTime = 0;

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
			currentTime -= delta;
			if (currentTime <= 0)
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
}
