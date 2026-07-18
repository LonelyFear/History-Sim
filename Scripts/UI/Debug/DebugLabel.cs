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
            
            if (region != null && region.owner != null){
                Text = "\n Claimant: " + (region.claimant == null ? "None" : region.claimant.name);
                Text += "\n Owner: " + (region.owner == null ? "None" : region.owner.name);
                /*
                State state = region.owner;
                //string leaderText = "Leader: None";
                Text = "State: " + state.name;
                AddLine("Population: " + state.population.ToString("#,###0"));
                AddLine("Relations: ");
                try
                {
                    foreach (var pair in state.relations.ToArray())
                    {
                        State relationState = pair.Key;
                        //if (relationState?.sovereignty != Sovereignty.INDEPENDENT) continue;
                        DiplomaticRelations relation = pair.Value;
                        AddLine(relationState.name + ": " + Math.Round(relation.opinion * 100));
                    }                    
                } catch (Exception e)
                {
                    GD.PushError(e);
                    AddLine("ERROR!");
                }
                */

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
