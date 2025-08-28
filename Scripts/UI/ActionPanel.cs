using Godot;
using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;

public partial class ActionPanel : Panel
{
    [Export] public Button menuButton;
    [Export] public Button mainMenuButton;
    [Export] public Button saveMenuButton;
    [Export] public Button saveButton;
    [Export] public Button saveCancelButton;
    [Export] public Panel menuPanel;
    [Export] public Panel saveNamePanel;
    bool uiVisible = true;
    public override void _Ready()
    {
        menuButton.Pressed += OnMenuClick;
        mainMenuButton.Pressed += OnMainMenu;
        saveMenuButton.Pressed += OpenSaveMenu;
        saveButton.Pressed += OnSimSave;
        saveCancelButton.Pressed += OpenSaveMenu;
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
        SimManager sim = GetNode<SimNodeManager>("/root/Game/Simulation").simManager;
        LoadingScreen.generator.worldgenFinishedEvent -= sim.OnWorldgenFinished;
        GetTree().ChangeSceneToPacked(GD.Load<PackedScene>("res://Scenes/main_menu.tscn"));
        GetNode<Game>("/root/Game").QueueFree();        
    }

    public void OnMenuClick()
    {
        menuPanel.Visible = !menuPanel.Visible;
    }
    public void OpenSaveMenu()
    {
        saveNamePanel.Visible = !saveNamePanel.Visible;
    }
    public void OnSimSave()
    {
        // Creates a new saves folder if there isnt any
        if (DirAccess.Open("user://saves") == null)
        {
            DirAccess.MakeDirAbsolute("user://saves");
        }

        string saveName = GetNode<LineEdit>("/root/Game/UI/SaveNamePanel/VBoxContainer/TextEdit").Text;
        int saveNum = DirAccess.GetDirectoriesAt("user://saves").Length + 1;
        string saveFileName = "Save" + saveNum;

        SaveData data = new SaveData()
        {
            saveName = saveName,
            saveID = saveFileName,
            saveVersion = "Alpha 1"
        };

        // Creates a new save folder
        DirAccess.MakeDirAbsolute($"user://saves/{saveFileName}");

        SimManager sim = GetNode<SimNodeManager>("/root/Game/Simulation").simManager;
        WorldGenerator world = LoadingScreen.generator;

        world.SaveTerrainToFile(saveFileName);
        FileAccess save = FileAccess.Open($"user://saves/{saveFileName}/save_data.json", FileAccess.ModeFlags.Write);
        GD.Print(JsonSerializer.Serialize(data));
        save.StoreString(JsonSerializer.Serialize(data));        
        sim.SaveSimToFile(saveFileName);        
    }

    public void OnUiToggle(bool toggle)
    {
        uiVisible = toggle;
        GetParent<CanvasLayer>().Visible = toggle;
    }
}
