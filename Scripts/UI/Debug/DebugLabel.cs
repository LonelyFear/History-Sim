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
                Text = "State: " + state.displayName + "\n" + "Population: " + Pop.FromNativePopulation(state.population).ToString("#,###0") + "\n" + "Aristocrats: " + Pop.FromNativePopulation(state.professions[SocialClass.ARISTOCRAT]).ToString("#,###0") + "\n" + "Manpower: " + Pop.FromNativePopulation(state.manpower).ToString("#,###0") + "\n" + leaderText;
            } else {
                Text = "";
            }            
        }
    } 
}
