using Godot;
using System;

public partial class GameUI : CanvasLayer
{
	public bool forceHide = false;
	public bool show = true;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
    {
        if (forceHide)
        {
			Visible = false;
        } else
        {
			Visible = show;
        }
    }
}
