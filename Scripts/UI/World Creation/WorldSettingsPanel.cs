using Godot;
using System;

public partial class WorldSettingsPanel : Panel
{
	[Export] LineEdit seedEdit;
	[Export] OptionButton sizeDropdown;
	[Export] CheckBox heightmapCheckbox;
	[Export] Button backButton;
	[Export] Button generateWorldButton;
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
        if (!int.TryParse(seedEdit.Text, out int seed) || seed == 0)
        {
            seed = rng.Next(-99999999, 99999999);
        }
		GetTree().Root.AddChild(GD.Load<PackedScene>("res://Scenes/game.tscn").Instantiate());
		loadingScreen = GetNode<LoadingScreen>("/root/Game/Loading/Loading Screen");
		

		loadingScreen.generator = new WorldGenerator()
		{
			Seed = seed,
			generateRandomMap = !heightmapCheckbox.ButtonPressed,
			WorldMult = sizeDropdown.GetSelectedId()
		};
		GetParent().QueueFree();
	}
}
