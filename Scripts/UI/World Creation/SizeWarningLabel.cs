using Godot;
using System;

public partial class SizeWarningLabel : Label
{
	[Export] OptionButton worldSizeDropdown;
    public override void _Process(double delta)
    {
		Hide();
        if (worldSizeDropdown.GetSelectedId() > 2)
		{
			Show();
		}
    }
}
