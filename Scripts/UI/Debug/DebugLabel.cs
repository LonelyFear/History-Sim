using Godot;
using System;

public partial class DebugLabel : Label
{
    [Export] MapManager map;
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
                AddLine("Ai Weights: ");
                foreach (var pair in state.AIManager.utilityScores)
                {
                    AddLine(pair.Key + ": " + pair.Value);
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
