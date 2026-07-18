using Godot;
using System;

public partial class WorldSettingsPanel : Panel
{
	[Export] LineEdit nameEdit;
	[Export] LineEdit seedEdit;
	[Export] OptionButton sizeDropdown;
	[Export] CheckBox heightmapCheckbox;
	[Export] CheckBox riverCheckbox;
	[Export] Button backButton;
	[Export] Button generateWorldButton;
	[Export] Slider largeContinents;
	[Export] Slider smallContinents;
	[Export] OptionButton landCoverageDropdown;
	Random rng = new Random();
	LoadingScreen loadingScreen;
	string oldText = "";
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		backButton.Pressed += OnBackPressed;
		seedEdit.TextChanged += OnSeedChanged;
		generateWorldButton.Pressed += OnStartPressed;
	}
	public void OnSeedChanged(string newText)
	{
		if (string.IsNullOrEmpty(newText) || !int.TryParse(newText, out _))
		{
			oldText = newText;
		}
		else
		{
			seedEdit.Text = oldText;
		}
	}
	public void OnBackPressed()
	{
		GetTree().ChangeSceneToPacked(GD.Load<PackedScene>("res://Scenes/main_menu.tscn"));
		GetParent().QueueFree();
	}
	public void OnStartPressed()
	{
		string worldName = "";
		if (nameEdit.Text.Length < 1)
		{
			worldName = NameGenerator.GenerateRandomName(3, 5, false, ["", "a", "ia", "al", "ica", "en", "una", "eth", "ar", "or", "inia"]);
		}
        if (!int.TryParse(seedEdit.Text, out int seed) || seed == 0)
        {
            seed = rng.Next(-99999999, 99999999);
        }
		GetTree().Root.AddChild(GD.Load<PackedScene>("res://Scenes/game.tscn").Instantiate());
		loadingScreen = GetNode<LoadingScreen>("/root/Game/Loading/Loading Screen");
		
		loadingScreen.worldSettings = new WorldSettings()
		{
			worldName = worldName,
			seed = seed,
			largeContinents = (int)largeContinents.Value,
			smallContinents = (int)smallContinents.Value,
			landCoverage = 1f - ((landCoverageDropdown.Selected + 2)/10f),
			useRandomMap = !heightmapCheckbox.ButtonPressed,
			useRivers = riverCheckbox.ButtonPressed,
			multiplier = sizeDropdown.GetSelectedId()			
		};
		GetParent().QueueFree();
	}
}
