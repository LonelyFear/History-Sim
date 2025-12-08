using Godot;
using System;

public partial class FrameBreakdown : Label
{
    SimManager simManager;
	[Export] double updateDelay = 0.1;
	double currentTime = 0;

    public override void _Ready()
	{
		GetNode<SimNodeManager>("/root/Game/Simulation").simStartEvent += OnSimStart;
	}

    public void OnSimStart()
    {
        simManager = GetNode<SimNodeManager>("/root/Game/Simulation").simManager;
	}
    public override void _Process(double delta)
    {
		if (simManager != null)
		{
			currentTime -= delta;
			if (currentTime <= 0)
			{
				currentTime = updateDelay;
				Text = "Frame Breakdown:\n";
				Text += "Total Step Time: " + simManager.totalStepTime.ToString("#,##0.0ms\n");
				Text += "Total Pops Time: " + simManager.stepPerformanceInfo["Pops"].ToString("#,##0.0ms\n");
				foreach (var pair in simManager.popsPerformanceInfo)
                {
                    Text += $"   {pair.Key}: " + pair.Value.ToString("#,##0.0ms\n");
                }
				Text += "Total Regions Time: " + simManager.stepPerformanceInfo["Regions"].ToString("#,##0.0ms\n");
				foreach (var pair in simManager.regionPerformanceInfo)
                {
                    Text += $"   {pair.Key}: " + pair.Value.ToString("#,##0.0ms\n");
                }
				Text += "Total State Time: " + simManager.stepPerformanceInfo["States"].ToString("#,##0.0ms\n");
				Text += "Total Misc Time: " + simManager.stepPerformanceInfo["Misc"].ToString("#,##0.0ms\n");				
			}

        }
		else
		{
			Text = "";
		}
    }
}
