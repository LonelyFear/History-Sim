using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

[GlobalClass]
public partial class EncyclopediaManager : CanvasLayer
{
	[Export] PackedScene infoTabScene;
	[Export] PackedScene polityTabScene;
	[Export] PackedScene indexTabScene;
	[Export] TimeManager timeManager;
	[Export] TabManager encyclopediaMenu;
	[Export] EncyclopediaMainMenu mainMenu;
	[Export] Control encyclopediaHolder;
	[Export] GameUI gameUi;
	[Export] PlayerCamera playerCamera;
	[Export] Button closeEncyclopediaButton;
	[Export] SimManagerHolder simHolder;
	public SimManager simManager;
	public ObjectManager objectManager;
	Dictionary<ulong, BaseEncyclopediaTab> infoTabs = [];

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        BaseEncyclopediaTab.encyclopediaManager = this;

		simHolder.simStartEvent += OnSimStart;
		encyclopediaMenu.TabClosePressed += CloseTab;
		closeEncyclopediaButton.Pressed += CloseEncyclopedia;
		mainMenu.text.MetaClicked += OnMetaClicked;
    }
	public void OnSimStart()
	{
		simManager = simHolder.simManager;
		objectManager = simManager.objectManager;
		simManager.objectDeleted += OnObjectDeleted;
	}
	public void OpenEncyclopedia()
	{
		encyclopediaHolder.Visible = true;
		playerCamera.controlEnabled = false;
		gameUi.forceHide = true;
		timeManager.forcePause = true;
		foreach (BaseEncyclopediaTab tab in infoTabs.Values)
        {
            tab.InitTab();
        }
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
			OpenIndexTab(NamedObject.GetTypeFromString(type));
            return;
        }
		OpenTab(meta);
    }
	public void OpenIndexTab(ObjectType objectType)
    {
		ulong id = (ulong)objectType;
        // TODO: Implement
		GD.Print(objectType);
		// If we already have a tab open for this object switch to it
		if (infoTabs.ContainsKey(id))
		{
			encyclopediaMenu.CurrentTab = infoTabs[id].GetIndex();
			return;
		}
		IndexTab newTab = indexTabScene.Instantiate<IndexTab>();
		newTab.type = objectType;
		newTab.InitTab();
		encyclopediaMenu.OpenTab(newTab);

		infoTabs.Add(id, newTab);
		encyclopediaMenu.CurrentTab = newTab.GetIndex();		
    }
	public void OpenTab(string fullId)
	{
		ulong id = ulong.Parse(fullId[3..]);

		// If we already have a tab open for this object switch to it
		if (infoTabs.ContainsKey(id))
		{
			encyclopediaMenu.CurrentTab = infoTabs[id].GetIndex();
			return;
		}
		
		BaseEncyclopediaTab newTab;
		NamedObject obj = NamedObject.GetNamedObject(fullId);
		switch (obj)
		{
			case Polity:
				newTab = polityTabScene.Instantiate<PolityTab>();
				((PolityTab)newTab).polity = (Polity)obj;
				break;
			default:
                newTab = infoTabScene.Instantiate<InfoTab>();
                ((InfoTab)newTab).objectType = NamedObject.GetTypeFromString(fullId[..3]);
				((InfoTab)newTab).loadedObj = obj;			
				break;
		}
		newTab.Name = obj.name;
		newTab.InitTab();

		encyclopediaMenu.OpenTab(newTab);
		infoTabs.Add(id, newTab);

		encyclopediaMenu.CurrentTab = infoTabs[id].GetIndex();
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

    public void OnMetaClicked(Variant meta)
    {
        OpenTab(meta);
    }
}
