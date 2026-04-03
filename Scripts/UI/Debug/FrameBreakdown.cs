using Godot;
using System;
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

				popTimeMenu.Text = $"Pops Time ({simManager.stepPerformanceInfo["Pops"]:#,##0.0ms})";
				foreach (var pair in simManager.popsPerformanceInfo)
                {
					int index = Array.IndexOf(simManager.popsPerformanceInfo.Keys.ToArray(), pair.Key);
					string newText = $"{pair.Key}: " + pair.Value.ToString("#,##0.0ms\n");
					if (popTimeMenu.GetPopup().ItemCount > simManager.popsPerformanceInfo.Count)
					{
						popTimeMenu.GetPopup().SetItemText(index, newText);
					} else
					{
						popTimeMenu.GetPopup().AddItem(newText);
					}
                }

				regionTimeMenu.Text = $"Regions Time ({simManager.stepPerformanceInfo["Regions"]:#,##0.0ms})";
				foreach (var pair in simManager.regionPerformanceInfo)
                {
					int index = Array.IndexOf(simManager.regionPerformanceInfo.Keys.ToArray(), pair.Key);
					string newText = $"{pair.Key}: " + pair.Value.ToString("#,##0.0ms\n");
					if (regionTimeMenu.GetPopup().ItemCount > simManager.regionPerformanceInfo.Count)
					{
						regionTimeMenu.GetPopup().SetItemText(index, newText);
					} else
					{
						regionTimeMenu.GetPopup().AddItem(newText);
					}
                }	

				stateTimeMenu.Text = $"States Time ({simManager.stepPerformanceInfo["States"]:#,##0.0ms})";
				foreach (var pair in simManager.stepPerformanceInfo)
                {
					int index = Array.IndexOf(simManager.stepPerformanceInfo.Keys.ToArray(), pair.Key);
					string newText = $"{pair.Key}: " + pair.Value.ToString("#,##0.0ms\n");
					if (stateTimeMenu.GetPopup().ItemCount > simManager.stepPerformanceInfo.Count)
					{
						stateTimeMenu.GetPopup().SetItemText(index, newText);
					} else
					{
						stateTimeMenu.GetPopup().AddItem(newText);
					}
                }	

				miscTimeMenu.Text = $"Misc Time ({simManager.stepPerformanceInfo["Misc"]:#,##0.0ms})";
				foreach (var pair in simManager.stepPerformanceInfo)
                {
					int index = Array.IndexOf(simManager.stepPerformanceInfo.Keys.ToArray(), pair.Key);
					string newText = $"{pair.Key}: " + pair.Value.ToString("#,##0.0ms\n");
					if (miscTimeMenu.GetPopup().ItemCount > simManager.stepPerformanceInfo.Count)
					{
						miscTimeMenu.GetPopup().SetItemText(index, newText);
					} else
					{
						miscTimeMenu.GetPopup().AddItem(newText);
					}
                }
			}
		}
    }
}
