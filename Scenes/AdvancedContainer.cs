using Godot;
using System;

public partial class AdvancedContainer : Panel
{
	[Export] CheckButton advancedOptionsToggle;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Visible = advancedOptionsToggle.ButtonPressed && advancedOptionsToggle.Visible;
	}
}
