using Godot;
using System;

[GlobalClass]
public partial class SelectionManager : Node2D
{
	[Export] PlayerCamera playerCamera;
	[Export] SimManagerHolder simHolder;
	[Export] Button deselectButton;
	[Export] MapManager mapManager;
	ObjectManager objectManager;
	SimManager simManager;

	Vector2I worldSize;
	Vector2 mousePos;
	public Vector2I hoveredPos;

	public Region lastHoveredRegion {get; private set;}
	public Region hoveredRegion {get; private set;}
	Region selectedRegion = null;
	public bool selectionEnabled = false;
    public override void _Ready()
    {
		simHolder.simStartEvent += Init;
        deselectButton.Pressed += DeselectRegion;
	}
	public void Init()
	{
		simManager = simHolder.simManager;
		objectManager = simManager.objectManager;
        worldSize = SimManager.worldSize;
        ObjectManager.selectionManager = this;

		selectionEnabled = true;
	}
    public override void _Process(double delta)
    {
		if (simManager == null) return;

		hoveredPos = simManager.GlobalToTilePos(playerCamera.mousePos);

        lastHoveredRegion = hoveredRegion;
        if (hoveredPos.X >= 0 && hoveredPos.X < worldSize.X && hoveredPos.Y >= 0 && hoveredPos.Y < worldSize.Y && selectionEnabled)
        {
            hoveredRegion = objectManager.GetRegion(hoveredPos.X, hoveredPos.Y);
        }
        else
        {
            hoveredRegion = null;
        }
    }
	public void SelectRegion(Region region)
	{
		if (region == null) return;

		if (CanSelectRegion(region))
		{
			selectedRegion = region;
		} else
		{
			DeselectRegion();
		}
	}
	public bool CanSelectRegion(Region region)
	{
        return mapManager.mapMode switch
        {
			MapModes.REALM => region.habitable,
			MapModes.POLITIY => region.habitable,
			MapModes.ALLIANCE => region.population > 0,
			MapModes.CULTURE => region.population > 0,
            _ => false,
        };
    }
	public void DeselectRegion()
	{
		selectedRegion = null;
	}
    public override void _UnhandledInput(InputEvent evnt)
    {
        if (evnt.IsAction("Select") && hoveredRegion != null)
        {
            SelectRegion(hoveredRegion);
        }
    }

	public bool IsRegionSelected()
	{
		return selectedRegion != null;
	}
	public Region GetSelectedRegion()
	{
		return selectedRegion;
	}
	public Culture GetSelectedCulture()
	{
		if (selectedRegion == null) return null;
		return objectManager.GetCulture(selectedRegion.largestCultureId);
	}
	public State GetSelectedState()
	{
		if (selectedRegion == null) return null;
		return selectedRegion.owner;
	}
	public Polity GetSelectedPolity()
	{
		if (selectedRegion == null || selectedRegion.owner == null) return null;
		return selectedRegion.owner.diplomacy.GetPolity();
	}
	public Alliance GetSelectedAlliance(AllianceType type)
	{
		if (selectedRegion == null || selectedRegion.owner == null) return null;
		return selectedRegion.owner.diplomacy.GetOverlord().diplomacy.GetAllianceOfType(type);
	}
}
