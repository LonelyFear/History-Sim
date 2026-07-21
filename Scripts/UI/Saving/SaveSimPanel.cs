using Godot;
using System.Collections.Generic;
using System.Text.Json;

public partial class SaveSimPanel : MenuButtonPanelBase
{
	[Export] Button saveButton;
	[Export] Button cancelButton;
	[Export] OptionButton saveOptions;
	[Export] LineEdit saveNameEdit;
	[Export] SimManagerHolder simHolder;
	List<string> saveOverwritePaths;
    SimManager sim;
    public override void _Ready()
    {
		cancelButton.Pressed += () => {
            Visible = false;
            GD.Print("Yay");
        };
		saveButton.Pressed += () => {
            OnSimSave();
            Visible = false;
        };
        VisibilityChanged += OnVisibilityChanged;    
    }

	void OnVisibilityChanged()
	{
        sim = simHolder.simManager;

        saveNameEdit.Text = sim.worldName;

		if (!Visible) return;
        menuButton.ButtonPressed = false;

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
            if (SaveManager.IsSaveValid(savePath))
            {
                saveOptions.AddItem($"Overwrite '{SaveManager.GetSaveData(savePath).saveName}'", i);
                saveOverwritePaths.Add(savePath);
            }
        }		
	}

    public void OnSimSave()
    {
        string saveName = saveNameEdit.Text;
        
        SaveManager.CreateSave(saveName, sim, sim.worldGenerator);
    }
}
