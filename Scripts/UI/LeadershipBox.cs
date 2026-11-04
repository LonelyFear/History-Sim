using Godot;
using MessagePack;
using PixelHistory.Objects.States.Base;
using System;

public partial class LeadershipBox : InfoPanelBox
{
	EncyclopediaManager encyclopedia;

	[ExportCategory("Labels")]
	Label govtLabel;
	RichTextLabel leaderLabel;
	Label leaderAgeLabel;
	Label stabLabel;
    public override void _Ready()
    {
		govtLabel = GetNode<Label>("GovtLabel");
		leaderLabel = GetNode<RichTextLabel>("LeaderLabel");
		leaderAgeLabel = GetNode<Label>("LeaderAgeLabel");
		stabLabel = GetNode<Label>("StabLabel");		
		
		encyclopedia = ((InfoHolder)Owner).encyclopedia;
        leaderLabel.MetaClicked += encyclopedia.OpenTab;
    }

	public override void _Process(double delta)
	{
        Visible = (infoHolder.selectedObject is State) || (infoHolder.selectedObject is Alliance);	
		State state = infoHolder.selectedObject as State;
		Alliance alliance = infoHolder.selectedObject as Alliance;
		if (state == null) state = alliance?.leadState;

		govtLabel.Visible = false;
		if (state != null)
		{
			leaderLabel.Text = $"Leader: {(state.leader != null ? NamedObject.GenerateUrlText(state.leader, $"{state.leaderTitle} {state.leader.name}") : "None!")}";
			
			leaderAgeLabel.Visible = state.leader != null;
			if (state.leader != null) leaderAgeLabel.Text = $"Age: {TimeManager.GetYear(state.leader.GetAge())} Years Old";
			
			stabLabel.Text = $"Stability: {state.stability:P0}";
		} 
    }
}
