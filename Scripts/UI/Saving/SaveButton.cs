using Godot;
using System;

public partial class SaveButton : Button
{
	public string saveName;
	public SaveData saveData;
	public string savePath;
	public string systemSavePath;
	public string displayPath;
	public bool invalid = false;
	public bool selected = false;
	public bool outdated = false;
	public Label saveNameLabel;
	public Label savePathLabel;
	public SavesPanel saves;

	public override void _Ready()
	{
		saveNameLabel = GetNode<Label>("SaveName");
		savePathLabel = GetNode<Label>("SaveStatus");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Color b = new Color(1,1,1);
		saveNameLabel.Text = invalid ? "Invalid Save" : saveData.saveName.Capitalize();
		savePathLabel.Text = displayPath;

		saveNameLabel.Modulate = !invalid ? Color.FromString("white", b) : Color.FromString("red", b);

		if (selected)
		{
			saveNameLabel.Modulate = Color.FromString("yellow", b);
		}
		
		savePathLabel.Modulate = !(invalid || outdated) ? Color.FromString("white", b) : Color.FromString("red", b);
	}

    public override void _Pressed()
    {
		if (saves != null)
		{
			saves.SaveSelected(this);
		}
    }

}
