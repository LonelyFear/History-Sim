using Godot;
using System;

public partial class AdvancedCheckbox : CheckButton
{
	[Export] CheckBox heightmapCheckbox;
	public override void _Process(double delta)
	{
		//Visible = !heightmapCheckbox.ButtonPressed;
	}
}
