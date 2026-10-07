using Godot;
using PixelHistory.Objects.States.Base;
using System;

[GlobalClass]
public partial class InfoHolder : VBoxContainer
{
	[Export] MapManager mapManager;
	[Export] SelectionManager selectionManager;
	[Export] TabBar focusTabBar;
	[Export] Label nameLabel;
	[Export] Label typeLabel;
	[Export] EncyclopediaManager encyclopedia;
	[Export] Button encyclopediaButton;

	public NamedObject selectedObject;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		focusTabBar.TabChanged += TabSelected;
		mapManager.mapmodeChanged += OnMapModeChanged;
		encyclopediaButton.Pressed += () =>
		{
			encyclopedia.OpenEncyclopedia();
			
			string idToOpen = selectedObject.GetFullId();
			if (selectedObject is Alliance alliance)
			{
				if (alliance.type == AllianceType.REALM)
				{
					idToOpen = alliance.leadState.GetFullId();
				}
			}
			encyclopedia.OpenTab(idToOpen);
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Visible = selectionManager.IsRegionSelected();
		if (Visible)
		{
			focusTabBar.SetTabDisabled(1, selectionManager.GetSelectedState() == null);
			focusTabBar.SetTabDisabled(2, selectionManager.GetSelectedState() == null);
			focusTabBar.SetTabDisabled(3, selectionManager.GetSelectedAlliance(AllianceType.ALLIANCE) == null);
			focusTabBar.SetTabDisabled(4, selectionManager.GetSelectedCulture() == null);
			focusTabBar.SetTabDisabled(5, true);

            selectedObject = focusTabBar.CurrentTab switch
            {
                1 => selectionManager.GetSelectedState(),
                2 => selectionManager.GetSelectedPolity(),
                3 => selectionManager.GetSelectedAlliance(AllianceType.ALLIANCE),
                4 => selectionManager.GetSelectedCulture(),
                5 => selectionManager.GetSelectedTradeZone(),
                _ => selectionManager.GetSelectedRegion(),
            };
			selectedObject ??= selectionManager.GetSelectedRegion();
			
			nameLabel.Text = selectedObject.name;
			if (selectedObject is State s)
			{
				nameLabel.Text = s.baseName;
			}
			if (selectedObject is Alliance a && a.type == AllianceType.REALM)
			{
				nameLabel.Text = a.leadState.baseName;
			}
			typeLabel.Text = selectedObject.GetTypeName();
        }
	}
	public void OnMapModeChanged(MapModes mapMode)
	{
		switch (mapManager.mapMode)
		{
			case MapModes.REALM:
				focusTabBar.CurrentTab = 2;
				break;
			case MapModes.POLITIY:
				focusTabBar.CurrentTab = 1;
				break;
			case MapModes.ALLIANCE:
				focusTabBar.CurrentTab = 3;
				break;
			case MapModes.CULTURE:
				focusTabBar.CurrentTab = 4;
				break;
			case MapModes.TRADE_WEIGHT:
				focusTabBar.CurrentTab = 5;
				break;
		}
	}
	public void TabSelected(long tab)
	{
		Region selectedRegion = selectionManager.GetSelectedRegion();
		switch (tab)
		{
			case 1:
				mapManager.SetMapMode(MapModes.POLITIY);
				break;
			case 2:
				mapManager.SetMapMode(MapModes.REALM);
				break;
			case 3:
				mapManager.SetMapMode(MapModes.ALLIANCE);
				break;
			case 4:
				mapManager.SetMapMode(MapModes.CULTURE);
				break;
			case 5:
				mapManager.SetMapMode(MapModes.TRADE_WEIGHT);
				break;
		}
		selectionManager.SelectRegion(selectedRegion);
	}
}
