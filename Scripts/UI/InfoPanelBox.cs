using Godot;
using System;
using System.Linq;

[GlobalClass]
public partial class InfoPanelBox : VBoxContainer
{
	[Export] InfoHolder infoHolder;
	[Export] MapManager mapManager;
	[Export] SelectionManager selectionManager;
	[Export] string[] visibleTypes = [];

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Visible = false;
		if (infoHolder.selectedObject != null)
		{
			Visible = visibleTypes.Contains(infoHolder.selectedObject.GetTypeName());
		}
	}
}
