using Godot;
using System;
using System.Collections.Generic;

public partial class MapModeSelector : GridContainer
{
	List<MapModeButton> mapModeButtons = [];
	[Export] MapManager mapManager;

	public override void _Ready()
	{
		foreach (Node child in GetChildren())
		{
			if (child is MapModeButton button)
			{
				button.Pressed += OnMapmodePressed;
				mapModeButtons.Add(button);

				if (button.mapMode == mapManager.mapMode)
				{
					button.Disabled = true;
				}
			}
		}
	}

	public void OnMapmodePressed()
	{
		foreach (MapModeButton button in mapModeButtons)
		{
			button.Disabled = false;
			if (button.ButtonPressed)
			{
				button.Disabled = true;
				button.ButtonPressed = false;
				mapManager.SetMapMode(button.mapMode);
			}
		}
	}
}
