using Godot;
using System;
using System.Linq;

public partial class DebugLabel : Label
{
    [Export] MapManager map;
    [Export] SimManagerHolder simHolder;
    SimManager simManager;
    ObjectManager objectManager;
    public override void _Ready()
    {
		simHolder.simStartEvent += Init;
	}
    void Init() {
        simManager = simHolder.simManager;
        objectManager = simManager.objectManager;
    }
    public override void _Process(double delta)
    {
        if (map != null)
        {
            Position = GetGlobalMousePosition();
            Region region = map.hoveredRegion;
            if (region != null && region.owner != null){
                State state = region.owner;
                //string leaderText = "Leader: None";
                Text = "State: " + state.name;
                AddLine("Population: " + state.population.ToString("#,###0"));
                AddLine("Relations: ");
                try
                {
                    foreach (var pair in state.diplomacy.relationIds.ToArray())
                    {
                        State relationState = objectManager.GetState(pair.Key);
                        if (relationState?.sovereignty != Sovereignty.INDEPENDENT) continue;
                        Relation relation = pair.Value;
                        AddLine(relationState.name + ": " + Math.Round(relation.opinion * 100));
                    }                    
                } catch (Exception e)
                {
                    GD.PushError(e);
                    AddLine("ERROR!");
                }

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
