using Godot;
using PixelHistory.Objects.States.Base;
using System;

public partial class InfoHolder : VBoxContainer
{
	[Export] SelectionManager selectionManager;
	[Export] Button exitButton;
	[Export] TabBar focusTab;
	[Export] Label nameLabel;
	[Export] Label typeLabel;

	NamedObject selectedObject;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		exitButton.Pressed += () => selectionManager.SelectRegion(null);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Visible = selectionManager.IsRegionSelected();

		if (Visible)
		{
			Region selectedRegion = selectionManager.GetSelectedRegion();
			switch (focusTab.CurrentTab) {
				case 0:
					selectedObject = selectedRegion;
				break;
				case 1:
					State selectedState = selectionManager.GetSelectedState();
					selectedObject = selectedState;
				break;
			}
			
			nameLabel.Text = selectedObject.name;
			typeLabel.Text = selectedObject.GetTypeName();
		}
	}
}
