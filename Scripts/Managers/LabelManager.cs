using Godot;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;

public partial class LabelManager : Node
{

	[Export] PackedScene labelScene;
	[Export] int minStateSize = 4;
	[Export] float stateSizeScaleMult = 0.1f;
	Dictionary<State, Label> currentLabels = new Dictionary<State, Label>();
	SimManager simManager;

	public override void _Ready()
	{
		simManager = GetNode<SimManager>("/root/Game/Simulation");
	}

	public override void _Process(double delta)
	{

		foreach (State state in simManager.states.ToArray())
		{
			Vector2 averageWorldPos = GetAveragePosition(state);
			if (currentLabels.ContainsKey(state))
			{
				// Has Label
				if (state.regions.Count < minStateSize)
				{
					currentLabels[state].QueueFree();
					currentLabels.Remove(state);
					continue;
				}
			}
			else
			{
				// Doesnt Have Label
				if (state.regions.Count < minStateSize)
				{
					continue;
				}
				// Can have label
				currentLabels[state] = labelScene.Instantiate<Label>();
				AddChild(currentLabels[state]);

			}
			// Label exists and can exist
			currentLabels[state].Text = state.name;

			float scale = state.regions.Count * stateSizeScaleMult;
			float x = averageWorldPos.X;
			float y = averageWorldPos.Y;
			
			currentLabels[state].Position = new Vector2(x, y);
			currentLabels[state].AddThemeFontSizeOverride("name", (int)(scale));
			//currentLabels[state].AddThemeFontSizeOverride("name", (int)(10 * scale));
		}
	}

	public Vector2 GetAveragePosition(State state)
	{
		Vector2 averagePos = new Vector2(0, 0);
		foreach (Region r in state.regions)
		{
			averagePos += simManager.RegionToGlobalPos(new Vector2(r.pos.X, r.pos.Y));
		}
		averagePos /= (float)state.regions.Count;
		return averagePos;
	}

}
