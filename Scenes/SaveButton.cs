using Godot;
using System;

public partial class SaveButton : Button
{
	public string saveName;
	public string savePath;
	public string displayPath;
	public bool invalid = false;
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
		saveNameLabel.Text = saveName.Capitalize();
		savePathLabel.Text = savePath;
	}

    public override void _Pressed()
    {
		if (saves != null)
		{
			saves.SaveSelected(this);
		}
    }

}
