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
			if (mapManager.mapMode.GetHoverText(hoveredRegion).Length > 0)
			{
				Text = mapManager.mapMode.GetHoverText(hoveredRegion);
			}
		}
	}
}
