using Godot;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.States.Diplomacy;
using System;

public partial class HoverText : RichTextLabel
{
	[Export] SelectionManager selectionManager;
	[Export] MapManager mapManager;
	public override void _Process(double delta)
	{
		Region hoveredRegion = selectionManager.hoveredRegion;
		if (hoveredRegion != null)
		{
			int xPos = hoveredRegion.gridPos.X;
			int yPos = hoveredRegion.gridPos.Y;		
			Text = $"({xPos},{yPos}) {hoveredRegion.name}";

			
			switch (mapManager.mapMode)
			{
				case MapModes.POLITIY:
					Polity state = hoveredRegion.owner;
					if (state != null) Text += $", {state.name}";
					break;
				case MapModes.REALM:
					Polity polity = hoveredRegion.owner?.GetPolity();
					if (polity != null) Text += $", {polity.name}";
					break;
				case MapModes.CULTURE:
					Culture culture = ObjectManager.GetCulture(hoveredRegion.largestCultureId);
					if (culture != null) Text += $", {culture.name}";
					break;
			}
		}
	}
}
