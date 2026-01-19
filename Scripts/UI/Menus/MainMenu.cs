using Godot;
using System;

public partial class MainMenu : Control
{
	public override void _Ready()
	{
		// Makes a new save folder if there isnt any
        if (DirAccess.Open("user://saves") == null)
		{
			DirAccess.MakeDirAbsolute("user://saves");
		}
	}
}
