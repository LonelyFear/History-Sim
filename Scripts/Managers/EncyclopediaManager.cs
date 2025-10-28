using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class EncyclopediaManager : CanvasLayer
{
	[Export] PackedScene infoTabScene;
	[Export] TimeManager timeManager;
	[Export] TabManager encyclopediaMenu;
	[Export] Control encyclopediaHolder;
	[Export] GameUI gameUi;
	[Export] PlayerCamera playerCamera;
	[Export] Button closeEncyclopediaButton;
	public SimManager simManager;
	Dictionary<ulong, InfoTab> infoTabs = new Dictionary<ulong, InfoTab>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GetNode<SimNodeManager>("/root/Game/Simulation").simStartEvent += OnSimStart;
		encyclopediaMenu.TabClosePressed += CloseTab;
		closeEncyclopediaButton.Pressed += CloseEncyclopedia;
    }
	public void OnSimStart()
	{
		simManager = GetNode<SimNodeManager>("/root/Game/Simulation").simManager;
		simManager.objectDeleted += OnObjectDeleted;
	}
	public void OpenEncyclopedia()
	{
		encyclopediaHolder.Visible = true;
		playerCamera.controlEnabled = false;
		gameUi.forceHide = true;
		timeManager.forcePause = true;
	}
	public void CloseEncyclopedia()
    {
		encyclopediaHolder.Visible = false;
		playerCamera.controlEnabled = true;
		gameUi.forceHide = false;
		timeManager.forcePause = false;
    }
	public void OpenTab(ObjectType objectType, ulong id)
	{
		int indexOffset = 2;
		// If we already have a tab open for this object switch to it
		if (infoTabs.ContainsKey(id))
		{
			encyclopediaMenu.CurrentTab = infoTabs[id].GetIndex() - indexOffset;
			return;
		}
		InfoTab newTab = infoTabScene.Instantiate<InfoTab>();
		NamedObject obj = null;
		switch (objectType)
        {
			case ObjectType.STATE:
				obj = simManager.GetState(id);
				break;
			case ObjectType.REGION:
				obj = simManager.GetRegion(id);
				break;
			case ObjectType.CULTURE:
				obj = simManager.GetCulture(id);
				break;
			case ObjectType.CHARACTER:
				obj = simManager.GetCharacter(id);
				break;
			case ObjectType.WAR:
				obj = simManager.GetWar(id);
				break;
        }
		newTab.loadedObj = obj;
		newTab.objectId = id;
		newTab.objectType = objectType;
		newTab.InitTab();
		encyclopediaMenu.OpenTab(newTab);
		infoTabs.Add(id, newTab);
		encyclopediaMenu.CurrentTab = infoTabs[id].GetIndex() - indexOffset;
	}
	public void CloseTab(long index)
	{
		if (index == 0)
		{
			return;
		}

		ulong id = infoTabs.Keys.ToArray()[index - 1];

		if (!infoTabs.ContainsKey(id))
		{
			return;
		}
		InfoTab infoTab = infoTabs[id];
		encyclopediaMenu.CurrentTab = 0;
		encyclopediaMenu.CloseTab(infoTab);
    }
	public void OnObjectDeleted(ulong id)
	{
		if (infoTabs.ContainsKey(id) == false)
		{
			return;
		}
		InfoTab tab = infoTabs[id];
		tab.QueueFree();
    }
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
    {
        
    }
}
