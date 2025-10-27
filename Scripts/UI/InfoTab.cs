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
	public string objectMetadata;
	public ulong objectId;
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

}
