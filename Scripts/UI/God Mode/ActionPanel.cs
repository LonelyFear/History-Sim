using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;

public partial class ActionPanel : Panel
{
    [Export] SimManagerHolder simHolder;
    [Export] public Button menuButton;
    [Export] public Button mainMenuButton;
    [Export] public Button saveMenuButton;
    [Export] public Button saveButton;
    [Export] public Button saveCancelButton;
    [Export] public OptionButton overwriteButton;
    [Export] public Panel menuPanel;
    [Export] public Panel saveNamePanel;
    [Export] public LineEdit saveNameEdit;
    [Export] public Button encyclopediaButton;
    [Export] EncyclopediaManager encyclopediaManager;

    List<string> saveOverwritePaths;
    bool uiVisible = true;
    public override void _Ready()
    {
        menuButton.Pressed += OnMenuClick;
        mainMenuButton.Pressed += OnMainMenu;
        encyclopediaButton.Pressed += OnEncyclopediaClick;
    }

    public override void _Process(double delta)
    {
        
        if (Input.IsActionJustPressed("Toggle_UI"))
        {
            uiVisible = !uiVisible;
            GetParent<GameUI>().show = uiVisible;
        }
        saveNameEdit.Visible = overwriteButton.Selected == 0;
    }

    public void OnMainMenu()
    {
        GetTree().ChangeSceneToPacked(GD.Load<PackedScene>("res://Scenes/main_menu.tscn"));
        GetNode<Game>("/root/Game").QueueFree();        
    }
    public void OnEncyclopediaClick()
    {
        encyclopediaManager.OpenEncyclopedia();
    }
    public void OnMenuClick()
    {
        menuPanel.Visible = !menuPanel.Visible;
    }

    public void OnUiToggle(bool toggle)
    {
        uiVisible = toggle;
        GetParent<CanvasLayer>().Visible = toggle;
    }
}
