using Godot;
using System;

public partial class InfoTab : VBoxContainer
{
	[Export] Label objName;
	[Export] Label objType;
	[Export] RichTextLabel objDesc;
	[Export] RichTextLabel objStats;
	[Export] RichTextLabel objHist;
	public string name;
	public string type;
	public string description;
	public string stats;
	public string history;
	public ulong objectId;
	public ObjectType objectType;
	public NamedObject loadedObj;
	public static SimManager sim;
	public override void _Process(double delta)
	{
		objName.Text = name;
		objType.Text = type;
		objDesc.Text = description;
		objStats.Text = stats;
		objHist.Text = history;
	}
	public void InitTab()
	{
		type = objectType.ToString().Capitalize();
		Name = loadedObj.name;
        name = loadedObj.name;
		GetDescription();
		GetStats();
		GetHistory();
	}
	public void GetDescription()
    {
		description = loadedObj.GenerateDescription();
    }
	public void GetStats()
	{
		stats = loadedObj.GenerateStatsText();
	}
	public void GetHistory()
    {
		history = "This object has no history yet";
    }
}
