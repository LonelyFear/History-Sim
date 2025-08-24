using Godot;
using System;
using System.ComponentModel;

public partial class ActionPanel : Panel
{
    public Button menuButton;
    public Button mainMenuButton;
    public Button saveButton;
    public Panel menuPanel;
    bool uiVisible = true;
    public override void _Ready()
    {
        menuPanel = GetNode<Panel>("/root/Game/UI/Options Panel");
        mainMenuButton = GetNode<Button>("/root/Game/UI/Options Panel/VBoxContainer/MainMenuButton");
        saveButton = GetNode<Button>("/root/Game/UI/Options Panel/VBoxContainer/SaveButton");
        menuButton = GetNode<Button>("HBoxContainer/MenuButton");
        menuButton.Pressed += OnMenuClick;
        mainMenuButton.Pressed += OnMainMenu;
        saveButton.Pressed += OnSimSave;
    }

    public override void _Process(double delta)
    {
        GetParent<CanvasLayer>().Visible = uiVisible;
        if (Input.IsActionJustPressed("Toggle_UI"))
        {
            uiVisible = !uiVisible;
        }
    }

    public void OnMainMenu()
    {
        SimManager.m.Close();
        GetTree().ChangeSceneToPacked(GD.Load<PackedScene>("res://Scenes/main_menu.tscn"));
        GetNode<Game>("/root/Game").QueueFree();        
    }

    public void OnMenuClick()
    {
        menuPanel.Visible = !menuPanel.Visible;
    }
    public void OnSimSave()
    {
        GD.Print("Saving Game");
        SimManager sim = GetNode<SimNodeManager>("/root/Game/Simulation").simManager;
        WorldGenerator world = LoadingScreen.generator;
        world.SaveTerrainToFile("Save1");
        sim.SaveSimToFile("Save1");
        GD.Print("Game saved to Save1");
    }

    public void OnUiToggle(bool toggle)
    {
        uiVisible = toggle;
        GetParent<CanvasLayer>().Visible = toggle;
    }
}
