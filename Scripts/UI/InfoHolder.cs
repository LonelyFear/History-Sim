using Godot;
using PixelHistory.Objects.States.Base;
using System;

[GlobalClass]
public partial class InfoHolder : VBoxContainer
{
	[Export] MapManager mapManager;
	[Export] SelectionManager selectionManager;
	[Export] public EncyclopediaManager encyclopedia;

	TabBar focusTabBar;
	Label nameLabel;
	Label typeLabel;
	Button encyclopediaButton;
	Button closeInfoButton;

	public NamedObject selectedObject;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		focusTabBar = GetNode<TabBar>("TabBar");
		nameLabel = GetNode<Label>("%Name");
		typeLabel = GetNode<Label>("%Type");
		closeInfoButton = GetNode<Button>("%CloseInfoButton");
		encyclopediaButton = GetNode<Button>("%EncyclopediaButton");

		focusTabBar.TabChanged += TabSelected;
		mapManager.mapmodeChanged += OnMapModeChanged;

		encyclopediaButton.Pressed += () =>
		{
			encyclopedia.OpenEncyclopedia();
			
			string idToOpen = selectedObject.GetFullId();
			encyclopedia.OpenTab(idToOpen);
		};

		foreach (Node child in FindChildren("*"))
		{
			if (child is InfoPanelBox infoPanel)
			{
				GD.Print("Hi");
				infoPanel.infoHolder = this;
				infoPanel.mapManager = mapManager;
				infoPanel.selectionManager = selectionManager;
			}
		}
		closeInfoButton.Pressed += () => selectionManager.SelectRegion(null);
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
	public void OnMapModeChanged(IMapMode newMapMode)
	{
        focusTabBar.CurrentTab = mapManager.mapMode.Id switch
        {
            "polity" => 1,
            "realm" => 2,
            "alliance" => 3,
            "culture" => 4,
            "trade_zone" => 5,
            _ => 0,
        };
    }
	public void TabSelected(long tab)
	{
		Region selectedRegion = selectionManager.GetSelectedRegion();
		switch (tab)
		{
			case 1:
				mapManager.SetMapMode("polity");
				break;
			case 2:
				mapManager.SetMapMode("realm");
				break;
			case 3:
				mapManager.SetMapMode("alliance");
				break;
			case 4:
				mapManager.SetMapMode("culture");
				break;
			case 5:
				mapManager.SetMapMode("trade_zone");
				break;
		}
		selectionManager.SelectRegion(selectedRegion);
	}
}
