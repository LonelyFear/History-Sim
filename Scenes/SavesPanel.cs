using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class SavesPanel : Panel
{
	VBoxContainer saveButtonContainer;
	[Export] PackedScene saveButtonScene;
	public string selectedSave = null;
	Dictionary<string, SaveButton> saves = new Dictionary<string, SaveButton>();
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		saveButtonContainer = GetNode<VBoxContainer>("SaveButtonContainer");
		GetNode<Button>("Buttons/Back").Pressed += OnBackClicked;
		GetNode<Button>("Buttons/Load").Pressed += OnLoadSaveClicked;
		GetNode<Button>("Buttons/Delete").Pressed += OnDeleteSaveClicked;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		List<string> savePaths = DirAccess.Open("user://saves").GetDirectories().Select(dir => "user://saves/" + dir).ToList();
		foreach (string path in saves.Keys.ToArray())
		{
			if (!savePaths.Contains(path))
			{
				SaveButton button = saves[path];
				saves.Remove(path);
				button.QueueFree();
			}

		}		
		foreach (string saveName in DirAccess.Open("user://saves").GetDirectories())
		{
			string savePath = "user://saves/" + saveName;
			if (!saves.ContainsKey(savePath) && DirAccess.Open(savePath).FileExists(savePath + "/terrain_data.pxsave") && DirAccess.Open(savePath).FileExists(savePath + "/sim_data.pxsave"))
			{
				SaveButton save = saveButtonScene.Instantiate<SaveButton>();
				save.saves = this;
				save.saveName = saveName;
				save.displayPath = OS.GetUserDataDir() + "/saves/" + saveName;
				save.savePath = savePath;

				saveButtonContainer.AddChild(save);
				saves.Add(savePath, save);
			}
			else if (!saves.ContainsKey(savePath))
			{
				SaveButton save = saveButtonScene.Instantiate<SaveButton>();
				save.saveName = "Invalid Save";
				save.displayPath = OS.GetUserDataDir() + "/saves/" + saveName;
				save.saves = this;
				save.savePath = savePath;
				save.invalid = true;

				save.GetNode<Label>("SaveName").Modulate = new Color(1, 0, 0);
				save.GetNode<Label>("SaveStatus").Modulate = new Color(1, 0, 0);

				saveButtonContainer.AddChild(save);
				saves.Add(savePath, save);
			}
		}
	}

	public void SaveSelected(SaveButton save)
	{
		selectedSave = save.savePath;
	}

	public void OnDeleteSaveClicked()
	{
		if (selectedSave != null)
		{
			OS.MoveToTrash(saves[selectedSave].displayPath);
			selectedSave = null;
		}
	}

	public void OnLoadSaveClicked()
	{
		if (selectedSave != null && !saves[selectedSave].invalid)
		{
			Node2D game = GD.Load<PackedScene>("res://Scenes/game.tscn").Instantiate<Node2D>();
			LoadingScreen loadingScreen = game.GetNode<LoadingScreen>("Loading/Loading Screen");
			loadingScreen.seed = 0;
			//loadingScreen.saveGamePath = selectedSave;
			GetTree().Root.AddChild(game);
			GetParent().QueueFree();
		}
	}

	public void OnBackClicked()
	{
		GetTree().ChangeSceneToPacked(GD.Load<PackedScene>("res://Scenes/main_menu.tscn"));
	}
}
