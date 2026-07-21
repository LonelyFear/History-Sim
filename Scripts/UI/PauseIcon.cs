using Godot;
using System;

public partial class PauseIcon : TextureRect
{
	[Export] TimeManager timeManager;
	public override void _Process(double delta)
	{
		Visible = timeManager.gameSpeed == TimeManager.GameSpeed.PAUSED && !timeManager.forcePause;
	}
}
