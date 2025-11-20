using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class EncyclopediaManager : CanvasLayer
{
	[Export] PackedScene infoTabScene;
	[Export] PackedScene indexTabScene;
	[Export] TimeManager timeManager;
	[Export] TabManager encyclopediaMenu;
	[Export] RichTextLabel mainMenuText;
	[Export] Control encyclopediaHolder;
	[Export] GameUI gameUi;
	[Export] PlayerCamera playerCamera;
	[Export] Button closeEncyclopediaButton;
	public SimManager simManager;
	public ObjectManager objectManager;
	Dictionary<ulong, Control> infoTabs = new Dictionary<ulong, Control>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		IndexTab.encyclopediaManager = this;
		InfoTab.manager = this;
		GetNode<SimNodeManager>("/root/Game/Simulation").simStartEvent += OnSimStart;
		encyclopediaMenu.TabClosePressed += CloseTab;
		closeEncyclopediaButton.Pressed += CloseEncyclopedia;
		mainMenuText.MetaClicked += OnMetaClicked;
    }
	public void OnSimStart()
	{
		simManager = GetNode<SimNodeManager>("/root/Game/Simulation").simManager;
		objectManager = simManager.objectManager;
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
	public void OpenTab(Variant metaData)
	{
		string meta = metaData.ToString();
		string type = meta[..3];
		string stringId = meta[3..];
		if (stringId == "Index")
        {
			// Opens an index
			OpenIndexTab(GetTypeFromString(type));
            return;
        }
		ulong id = ulong.Parse(stringId);
		OpenTab(GetTypeFromString(type), id);
    }
	public void OpenIndexTab(ObjectType objectType)
    {
		ulong id = (ulong)objectType;
        // TODO: Implement
		GD.Print(objectType);
		int indexOffset = 2;
		// If we already have a tab open for this object switch to it
		if (infoTabs.ContainsKey(id))
		{
			encyclopediaMenu.CurrentTab = infoTabs[id].GetIndex() - indexOffset;
			return;
		}
		IndexTab newTab = indexTabScene.Instantiate<IndexTab>();
		newTab.type = objectType;
		encyclopediaMenu.OpenTab(newTab);
		infoTabs.Add(id, newTab);
		encyclopediaMenu.CurrentTab = infoTabs[id].GetIndex() - indexOffset;		
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
				obj = objectManager.GetState(id);
				break;
			case ObjectType.REGION:
				obj = objectManager.GetRegion(id);
				break;
			case ObjectType.CULTURE:
				obj = objectManager.GetCulture(id);
				break;
			case ObjectType.CHARACTER:
				obj = objectManager.GetCharacter(id);
				break;
			case ObjectType.WAR:
				obj = objectManager.GetWar(id);
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
		Control infoTab = infoTabs[id];
		encyclopediaMenu.CurrentTab = 0;
		encyclopediaMenu.CloseTab(infoTab);
		infoTabs.Remove(id);
    }
	public void OnObjectDeleted(ulong id)
	{
		if (infoTabs.ContainsKey(id) == false)
		{
			return;
		}
		Control tab = infoTabs[id];
		encyclopediaMenu.CloseTab(tab);
		infoTabs.Remove(id);
    }
	public ObjectType GetTypeFromString(string s)
    {
        switch (s)
        {
			case "sta":
				return ObjectType.STATE;
			case "reg":
				return ObjectType.REGION;
			case "cul":
				return ObjectType.CULTURE;
			case "cha":
				return ObjectType.CHARACTER;
			case "war":
				return ObjectType.WAR;
			default:
				return ObjectType.UNKNOWN;
        }
    }

    public void OnMetaClicked(Variant meta)
    {
        OpenTab(meta);
    }
}
