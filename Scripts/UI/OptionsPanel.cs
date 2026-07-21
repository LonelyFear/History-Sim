using Godot;
using System;

public partial class OptionsPanel : MenuButtonPanelBase
{
	//[Export] Button menuButton;
	[Export] Button openSavePanelButton;
	[Export] Panel saveSimPanel;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		openSavePanelButton.Pressed += () => {
			Visible = false;
			saveSimPanel.Visible = true;
		};
	}

	public override void _Process(double delta)
    {
        Visible = menuButton.ButtonPressed && !saveSimPanel.Visible;
    }
}
