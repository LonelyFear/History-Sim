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
                string leaderText = "Leader: None";
                if (state.leader != null){
                    leaderText = "Leader: " + state.leader.name + "\nLeader Age: " + (int)(state.leader.age/12f);
                }
                Text = "State: " + state.displayName + "\n" + "Population: " + Pop.FromNativePopulation(state.population).ToString("#,###0") + "\n" + "Aristocrats: " + Pop.FromNativePopulation(state.professions[Profession.ARISTOCRAT]).ToString("#,###0") + "\n" + "Manpower: " + Pop.FromNativePopulation(state.manpower).ToString("#,###0") + "\n" + leaderText;
            } else {
                Text = "";
            }            
        }
    } 
}
