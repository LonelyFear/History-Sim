using Godot;
using System;

public partial class DebugLabel : Label
{
    [Export] SimManager sim;
    public override void _Process(double delta)
    {
        Position = GetGlobalMousePosition();
        Region region = sim.hoveredRegion;
        if (region != null && region.owner != null){
            State state = region.owner;
            string leaderText = "Leader: None";
            if (state.leader != null){
                leaderText = "Leader: " + state.leader.name + "\nLeader Age: " + (int)(state.leader.age/12f);
            }
            Text = "State: " + state.displayName + "\n" + "Population: " + Pop.FromNativePopulation(state.population).ToString("#,###0") + "\n" + "Manpower: " + Pop.FromNativePopulation(state.manpower).ToString("#,###0") + "\n" + leaderText;
        } else {
            Text = "";
        }
        
    } 
}
