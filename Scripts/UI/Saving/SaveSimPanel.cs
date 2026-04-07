using Godot;
using System.Collections.Generic;
using System.Text.Json;

public partial class SaveSimPanel : Panel
{
	[Export] Button openSavePanelButton;
	[Export] Button saveButton;
	[Export] Button cancelButton;
	[Export] OptionButton saveOptions;
	[Export] LineEdit saveNameEdit;
	[Export] SimManagerHolder simHolder;
	List<string> saveOverwritePaths;
    public override void _Ready()
    {
		Visible = false;

		openSavePanelButton.Pressed += () => Visible = true;
		cancelButton.Pressed += () => Visible = false;
		saveButton.Pressed += OnSimSave;
        VisibilityChanged += OnVisibilityChanged;
    }
	void OnVisibilityChanged()
	{
		if (!Visible) return;

        saveOverwritePaths = [];
        
        for (int i = 0; i < saveOptions.ItemCount; i++)
        {
            saveOptions.RemoveItem(i);
        }
        if (saveOptions.ItemCount > 0) saveOptions.RemoveItem(0);

        string[] directories = DirAccess.GetDirectoriesAt("user://saves");

        saveOptions.AddItem("Create New Save", 0);
        saveOptions.Select(0);
        saveOverwritePaths.Add("New Save");

        for (int i = 0; i < directories.Length; i++)
        {
            string dirName = directories[i];
            string savePath = "user://saves/" + dirName;
            if (Utility.IsSaveValid(savePath))
            {
                saveOptions.AddItem($"Overwrite '{Utility.GetSaveData(savePath).saveName}'", i);
                saveOverwritePaths.Add(savePath);
            }
        }		
	}

    public void OnSimSave()
    {
        string saveName = saveNameEdit.Text;
		string overwritePath = saveOverwritePaths[saveOptions.Selected];

        if (saveName == "")
        {
            saveName = Utility.GetSaveData(overwritePath).saveName;
        }
        int saveNum = DirAccess.GetDirectoriesAt("user://saves").Length + 1;
        string saveFolderName = "Save" + saveNum;

        SaveData data = new()
        {
            saveName = saveName,
            saveID = saveFolderName,
        };

        // Creates a new save folder
        string saveDir = $"user://saves/{saveFolderName}";

        if (overwritePath == "New Save")
        {
			DirAccess.MakeDirAbsolute($"user://saves/{saveFolderName}");
        }
        else
        {
            GD.Print($"Overwriting {overwritePath}");
            saveDir = overwritePath;             
        }
        
        SimManager sim = simHolder.simManager;
        WorldGenerator world = sim.worldGenerator;

		// Saves save data
        FileAccess saveDataFile = FileAccess.Open(saveDir.PathJoin("save_data.json"), FileAccess.ModeFlags.Write);

        saveDataFile.StoreString(JsonSerializer.Serialize(data));
		saveDataFile.Dispose(); 

		// Saves world and simulation
		world.SaveTerrainToFile(saveDir);
        sim.SaveSimToFile(saveDir);

		Visible = false;   
    }

	
}
