using Godot;
using System;

public partial class DebugPanel : Panel
{
	[Export] Button closeButton;
	public override void _Ready()
	{
		closeButton.Pressed += () => Visible = false;
	}
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("Toggle_Debug"))
		{
			Visible = !Visible;
		}
	}
}
