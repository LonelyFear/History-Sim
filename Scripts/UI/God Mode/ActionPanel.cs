using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    [Export] Panel saveSimPanel;
    [Export] public Button encyclopediaButton;
    [Export] EncyclopediaManager encyclopediaManager;
    [Export] public GameUI uiLayer;

    public override void _Ready()
    {
        menuButton.Pressed += OnMenuClick;
        mainMenuButton.Pressed += OnMainMenu;
        encyclopediaButton.Pressed += OnEncyclopediaClick;
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
        if (saveSimPanel.Visible) return;
        menuPanel.Visible = !menuPanel.Visible;
    }
}
