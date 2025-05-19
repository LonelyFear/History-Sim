using Godot;
using System;
using System.ComponentModel;

public partial class ActionPanel : Panel
{
    public Button mainMenuButton;
    bool uiVisible = true;
    public override void _Ready()
    {
        mainMenuButton = GetNode<Button>("HBoxContainer/MainMenuButton");
        mainMenuButton.Pressed += OnMainMenuClick;
    }

    public override void _Process(double delta)
    {
        GetParent<CanvasLayer>().Visible = uiVisible;
        if (Input.IsActionJustPressed("Toggle_UI"))
        {
            uiVisible = !uiVisible;
        }
    }


    public void OnMainMenuClick()
    {
        GetTree().ChangeSceneToPacked(GD.Load<PackedScene>("res://Scenes/main_menu.tscn"));
        GetNode<Game>("/root/Game").QueueFree();
    }

    public void OnUiToggle(bool toggle)
    {
        uiVisible = toggle;
        GetParent<CanvasLayer>().Visible = toggle;
    }
}
