using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public partial class SavesPanel : Panel
{
	VBoxContainer saveButtonContainer;
	[Export] PackedScene saveButtonScene;
	Button loadButton;
	Button deleteButton;
	public string selectedSave = "";
	Dictionary<string, SaveButton> saves = [];
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		saveButtonContainer = GetNode<VBoxContainer>("ScrollContainer/SaveButtonContainer");
		loadButton = GetNode<Button>("Buttons/Load");
		deleteButton = GetNode<Button>("Buttons/Delete");

		GetNode<Button>("Buttons/Back").Pressed += OnBackClicked;
		loadButton.Pressed += OnLoadSaveClicked;
		deleteButton.Pressed += OnDeleteSaveClicked;

		RefreshSaveButtons();
	}
	public void RefreshSaveButtons()
	{
		DirAccess saveDirectory = DirAccess.Open("user://saves");

		List<string> savePaths = saveDirectory.GetDirectories().Select(dir => "user://saves/" + dir).ToList();
		foreach (string path in saves.Keys.ToArray())
		{
			if (!savePaths.Contains(path))
			{
				SaveButton button = saves[path];
				saves.Remove(path);
				button.QueueFree();
			}
		}	
		
		FileAccess saveDataFile = null;
		foreach (string saveName in saveDirectory.GetDirectories())
		{
			string savePath = "user://saves/" + saveName;
			if (saves.ContainsKey(savePath))
			{
				continue;
			}

			SaveButton save = saveButtonScene.Instantiate<SaveButton>();
			save.savePath = savePath;
			save.systemSavePath = OS.GetUserDataDir() + "/saves/" + saveName;
			save.displayPath = "User/saves/" + saveName;
			save.saves = this;

			saveButtonContainer.AddChild(save);
			saves.Add(savePath, save);

			if (Utility.IsSaveValid(savePath))
			{
				saveDataFile = FileAccess.Open(savePath.PathJoin("save_data.json"), FileAccess.ModeFlags.Read);
				string saveText = saveDataFile.GetAsText(true);
				saveDataFile.Dispose();

				SaveData saveData = JsonSerializer.Deserialize<SaveData>(saveText);
				save.saveData = saveData;

				bool outdated = saveData.saveVersion != SaveData.validSaveVersion;
				save.outdated = outdated;
				save.displayPath = $"{save.displayPath} | Format: {saveData.saveVersion}{(outdated ? " [Outdated]" : "")}";
			}
			else
			{
				save.invalid = true;
			}
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		loadButton.Disabled = selectedSave.Length == 0;
		deleteButton.Disabled = selectedSave.Length == 0;
	}

	public void SaveSelected(SaveButton save)
	{
		// Old
		saves.TryGetValue(selectedSave, out SaveButton oldButton);

		if (oldButton != null)
		{
			DeselectSave(oldButton);
		}
		
		// Reassign
		selectedSave = save.savePath;
		save.selected = true;
	}

	public void OnDeleteSaveClicked()
	{
		if (selectedSave.Length > 0)
		{
			DeleteDirectory(saves[selectedSave].savePath);
			DeselectSave(saves[selectedSave]);
			RefreshSaveButtons();
		}
	}

	void DeleteDirectory(string path)
	{
		if (!DirAccess.DirExistsAbsolute(path)) return;

		DirAccess dir = DirAccess.Open(path);
		dir.ListDirBegin();
		string fileName = dir.GetNext();

		while (fileName != "")
		{
			if (dir.CurrentIsDir())
			{
				DeleteDirectory(path.PathJoin(fileName));
			} else
			{
				dir.Remove(fileName);
			}
			fileName = dir.GetNext();
		}
		DirAccess.RemoveAbsolute(path);
	}

	public void DeselectSave(SaveButton saveButton)
	{
		saveButton.selected = false;
		selectedSave = "";		
	}

	public void OnLoadSaveClicked()
	{
		if (selectedSave.Length > 0  && !saves[selectedSave].invalid)
		{
			Node2D game = GD.Load<PackedScene>("res://Scenes/game.tscn").Instantiate<Node2D>();
			LoadingScreen loadingScreen = game.GetNode<LoadingScreen>("Loading/Loading Screen");
			loadingScreen.savePath = selectedSave;
			GetTree().Root.AddChild(game);
			GetParent().QueueFree();
		}
	}

	public void OnBackClicked()
	{
		GetTree().ChangeSceneToPacked(GD.Load<PackedScene>("res://Scenes/main_menu.tscn"));
	}
}
