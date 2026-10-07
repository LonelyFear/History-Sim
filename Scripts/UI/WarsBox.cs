using Godot;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.Wars;
using System;

public partial class WarsBox : InfoPanelBox
{
    [Export] RichTextLabel warsLabel;

	public override void _Process(double delta)
	{
		Visible = infoHolder.selectedObject is Polity;
        if (!Visible) return;

        warsLabel.Text = "";
        State state;
        if (infoHolder.selectedObject is Alliance alliance)
        {
            state = alliance.leadState;
        } else
        {
            state = infoHolder.selectedObject as State;
        }

        if (state?.enemies.Count > 0)
        {
            
            foreach (var pair in state.wars)
            {
                War war = pair.Key;
                //warsLabel.Text += $"{NamedObject.GenerateUrlText(war, war.name)}: {(pair.Value == War.WarSide.AGRESSOR ? "Attacker" : "Defender")}\n";
            }
            
            foreach (State enemy in state.enemies)
            {
                warsLabel.Text += $"War with {enemy.baseName}\n";
            }
            
        } 
        else
        {
            warsLabel.Text = "At Peace";
        }        
    }
}
