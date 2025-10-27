using Godot;
using System;
using System.Collections.Generic;

public partial class EncyclopediaManager : CanvasLayer
{
	[Export] PackedScene infoTabScene;
	[Export] TimeManager timeManager;
	[Export] TabContainer encyclopediaMenu;
	public SimManager simManager;
	Dictionary<ulong, InfoTab> infoTabs = new Dictionary<ulong, InfoTab>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
        GetNode<SimNodeManager>("/root/Game/Simulation").simStartEvent += OnSimStart;
    }
	public void OnSimStart()
	{
		simManager = GetNode<SimNodeManager>("/root/Game/Simulation").simManager;
		simManager.objectDeleted += OnObjectDeleted;
	}
	public void OpenTab(ObjectType objectType, ulong id)
	{

	}
	public void CloseTab(ulong id)
    {
        
    }
	public void OnObjectDeleted(ulong id)
    {
		InfoTab tab = infoTabs[id];
		tab.QueueFree();
    }
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
    {
        
    }
}
