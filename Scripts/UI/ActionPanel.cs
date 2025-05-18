using Godot;
using System;

public partial class ActionPanel : Panel
{
    [Export] public Button mainMenuButton;
    public override void _Ready()
    {
        mainMenuButton = GetNode<Button>("HBoxContainer/MainMenuButton");
        mainMenuButton.Pressed += OnMainMenuClick;
    }

    public void OnMainMenuClick()
    {
        GetTree().ChangeSceneToPacked(GD.Load<PackedScene>("res://Scenes/main_menu.tscn"));
        GetNode<Game>("/root/Game").QueueFree();
    }
}
