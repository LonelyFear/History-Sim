using Godot;
using System;

public partial class TopPanel : Panel
{
    [ExportCategory("References")]
	[Export] SimManagerHolder simHolder;
	SimManager simManager;
    [Export] TimeManager timeManager;
    bool update = false;

    Label yearLabel;
    OptionButton timeDropdown;
    public override void _Ready()
    {
		yearLabel = GetNode<Label>("%YearLabel");
		timeDropdown = GetNode<OptionButton>("%TimeDropdown");
        timeDropdown.ItemSelected += index =>

        {
            timeManager.SetGameSpeed((TimeManager.GameSpeed)index);
        };

        // Connection
		simHolder.simStartEvent += OnSimStart;
	}
    public void OnSimStart()
    {   
        update = true;
        simManager = simHolder.simManager;
	}
    public override void _Process(double delta)
    {
        if (!update) return;

        yearLabel.Text = $"Year {TimeManager.GetYear(timeManager.ticks)}";
    }
}
