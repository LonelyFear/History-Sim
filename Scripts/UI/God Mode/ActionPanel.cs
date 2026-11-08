using Godot;

public partial class ActionPanel : Panel
{
    [Export] SimManagerHolder simHolder;
    [Export] public Button encyclopediaButton;
    [Export] EncyclopediaManager encyclopediaManager;
    [Export] PauseMenus pauseMenus;
    Button menuButton;

    Label worldPopulationLabel;
    Label warCountLabel;
    Label stateCountLabel;
    SimManager simManager;

    public override void _Ready()
    {
        worldPopulationLabel = GetNode<Label>("%WorldPopulationLabel");
        warCountLabel = GetNode<Label>("%WarCountLabel");
        stateCountLabel = GetNode<Label>("%StateCountLabel");
        
        menuButton = GetNode<Button>("%MenuButton");

        encyclopediaButton.Pressed += OnEncyclopediaClick;

        // Connection
		simHolder.simStartEvent += () => {simManager = simHolder.simManager;};
        menuButton.Pressed += TogglePauseMenu;
    }
    public override void _Process(double delta)
    {
        if (simManager == null) return;
        
        worldPopulationLabel.Text = $"World Population: {simManager.worldPopulation:#,##0}";
        warCountLabel.Text = $"Wars: {simManager.warIds.Count:#,##0}";
        stateCountLabel.Text = $"States: {simManager.statesIds.Count:#,##0}";
    }
    public void OnEncyclopediaClick()
    {
        encyclopediaManager.OpenEncyclopedia();
    }
    public void TogglePauseMenu()
    {
        if (pauseMenus.Visible)
        {
            pauseMenus.ClosePauseMenu();
        } else
        {
            pauseMenus.Visible = true;
        }
    }
}
