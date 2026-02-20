using Godot;
using System;
using System.Collections.Generic;
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
        saveMenuButton.Pressed += OpenSaveMenu;
        saveButton.Pressed += OnSimSave;
        saveCancelButton.Pressed += OpenSaveMenu;
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
        SimManager sim = GetNode<SimNodeManager>("/root/Game/Simulation").simManager;
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
    public void OpenSaveMenu()
    {
        saveOverwritePaths = new List<string>();
        saveNamePanel.Visible = !saveNamePanel.Visible;
        
        for (int i = 0; i < overwriteButton.ItemCount; i++)
        {
            overwriteButton.RemoveItem(i);
        }
        if (overwriteButton.ItemCount > 0)
        {
            overwriteButton.RemoveItem(0);
        }

        string[] directories = DirAccess.GetDirectoriesAt("user://saves");
        overwriteButton.AddItem("Create New Save", 0);
        overwriteButton.Select(0);
        saveOverwritePaths.Add("New Save");
        for (int i = 0; i < directories.Length; i++)
        {
            string dirName = directories[i];
            string savePath = "user://saves/" + dirName;
            if (Utility.IsSaveValid(savePath))
            {
                FileAccess saveDataFile = FileAccess.Open(savePath + "/save_data.json", FileAccess.ModeFlags.Read);
                string saveText = saveDataFile.GetAsText(true);
                saveDataFile.Dispose();
                overwriteButton.AddItem($"Overwrite '{JsonSerializer.Deserialize<SaveData>(saveText).saveName}'");
                saveOverwritePaths.Add(savePath);
            }
        }
    }
    public void OnSimSave()
    {

        string saveName = saveNameEdit.Text;
        if (saveName == "")
        {
            saveName = saveOverwritePaths[overwriteButton.Selected];
        }
        int saveNum = DirAccess.GetDirectoriesAt("user://saves").Length + 1;
        string saveFileName = "Save" + saveNum;

        SaveData data = new SaveData()
        {
            saveName = saveName,
            saveID = saveFileName,
            saveVersion = "Alpha 1"
        };

        // Creates a new save folder
        string saveDir = $"user://saves/{saveFileName}";
        if (saveOverwritePaths[overwriteButton.Selected] != "New Save")
        {
            GD.Print(saveOverwritePaths[overwriteButton.Selected]);
            saveDir = saveOverwritePaths[overwriteButton.Selected];            
        }
        else
        {
            DirAccess.MakeDirAbsolute($"user://saves/{saveFileName}");
        }
        

        SimManager sim = GetNode<SimNodeManager>("/root/Game/Simulation").simManager;
        WorldGenerator world = sim.worldGenerator;

        world.SaveTerrainToFile(saveDir);
        FileAccess save = FileAccess.Open($"{saveDir}/save_data.json", FileAccess.ModeFlags.Write);
        GD.Print(JsonSerializer.Serialize(data));
        save.StoreString(JsonSerializer.Serialize(data));
        sim.SaveSimToFile(saveDir);
        save.Dispose();    
    }

    public void OnUiToggle(bool toggle)
    {
        uiVisible = toggle;
        GetParent<CanvasLayer>().Visible = toggle;
    }
}
