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
        string saveName = GetNode<LineEdit>("/root/Game/UI/SaveNamePanel/VBoxContainer/TextEdit").Text;
        if (saveName == "")
        {
            int saveNum = 1;
            foreach (string dirName in DirAccess.GetDirectoriesAt("user://saves"))
            {
                GD.Print(dirName);
                if (dirName[..^1] == "Save")
                {
                    if (dirName[dirName.Length - 1] > saveNum)
                    {
                        saveNum++;
                    }
                }
            }
            saveName = "Save" + saveNum;
        }
        SaveData data = new SaveData()
        {
            saveName = saveName,
            saveID = saveName,
            saveVersion = "Alpha 1"
        };
        GD.Print("Saving Game");
        SimManager sim = GetNode<SimNodeManager>("/root/Game/Simulation").simManager;
        WorldGenerator world = LoadingScreen.generator;


        
        world.SaveTerrainToFile(saveName);
        FileAccess save = FileAccess.Open($"user://saves/{saveName}/save_data.json", FileAccess.ModeFlags.Write);
        save.StoreString(JsonSerializer.Serialize(data));        
        sim.SaveSimToFile(saveName);
        GD.Print("Game saved to " + saveName);            
    }

    public void OnUiToggle(bool toggle)
    {
        uiVisible = toggle;
        GetParent<CanvasLayer>().Visible = toggle;
    }
}
