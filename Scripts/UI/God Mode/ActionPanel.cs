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
            uiLayer.show = uiVisible;
        }
        //saveSimPanel.Visible = overwriteButton.Selected == 0;

        if (Input.IsActionJustPressed("Take_Screenshot"))
        {
            TakeScreenshot();
        }
        
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
    public void TakeScreenshot()
    {
        string screenshotsFolderPath = "user://screenshots";
        if (DirAccess.Open(screenshotsFolderPath) == null)
        {
            DirAccess.MakeDirAbsolute(screenshotsFolderPath);
        }

        Image screenImage = GetViewport().GetTexture().GetImage();
        string screenshotName = "screenshot" + (DirAccess.Open(screenshotsFolderPath).GetFiles().Length + 1);
        string screenshotPath = screenshotsFolderPath.PathJoin(screenshotName);
        screenImage.SavePng(screenshotPath + ".png");
        GD.Print($"Screenshot {screenshotName} saved at path {screenshotPath}");
    }
}
