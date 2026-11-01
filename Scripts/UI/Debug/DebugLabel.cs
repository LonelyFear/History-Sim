using Godot;
using PixelHistory.Objects.States.Base;
using System;
using System.Linq;

public partial class DebugLabel : Label
{
    [Export] SelectionManager selectionManager;
    [Export] MapManager map;
    [Export] SimManagerHolder simHolder;
    SimManager simManager;
    public override void _Ready()
    {
		simHolder.simStartEvent += Init;
	}
    void Init() {
        simManager = simHolder.simManager;
    }
    public override void _Process(double delta)
    {
        if (map != null)
        {
            Position = GetGlobalMousePosition();
            Region region = selectionManager.hoveredRegion;
            
            /*
            if (region != null && region.owner != null){
                Text = "\n Claimant: " + (region.claimant == null ? "None" : region.claimant.name);
                Text += "\n Owner: " + (region.owner == null ? "None" : region.owner.name);
            } else {
                Text = "";
            }   
            */  
            if (region != null && region.habitable){
                Text = $"\n Supply: {region.economy.supplies[Defines.Goods.FOOD]:#,##0}";
                Text += $"\n Production: {region.economy.production[Defines.Goods.FOOD]:#,##0}";
                Text += $"\n Demand: {region.economy.demands[Defines.Goods.FOOD]:#,##0}";
            } else {
                Text = "";
            }        
        }
    } 

    void AddLine(string str)
    {
        Text += "\n" + str;
    }
}
