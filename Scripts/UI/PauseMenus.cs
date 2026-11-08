using Godot;
using System;
using System.Linq;

public partial class PauseMenus : Control
{
    TimeManager timeManager;
    PlayerCamera playerCamera;

    VBoxContainer pauseMenuContainer;
    VBoxContainer saveMenuContainer;
    Button mainMenuButton;
    Button saveSimulationButton;

    CheckButton overwriteToggle;
    LineEdit saveNameEdit;
    OptionButton overwriteSaveDropdown;
    Button acceptSaveButton;
    Button cancelSaveButton;

    Button pauseMenuClose;
    Button saveMenuClose;

    public override void _Ready()
    {
        playerCamera = GetTree().Root.GetChild(0).GetNode<PlayerCamera>("PlayerCamera");
        timeManager = GetTree().Root.GetChild(0).GetNode<TimeManager>("TimeManager");

        mainMenuButton = GetNode<Button>("%MainMenuButton");
        saveSimulationButton = GetNode<Button>("%SaveSimulationButton");
        acceptSaveButton = GetNode<Button>("%AcceptSaveButton");
        cancelSaveButton = GetNode<Button>("%CancelSaveButton");
        overwriteToggle = GetNode<CheckButton>("%OverwriteToggle");
        overwriteSaveDropdown = GetNode<OptionButton>("%OverwriteSaveDropdown");
        saveNameEdit = GetNode<LineEdit>("%SaveNameEdit");

        pauseMenuClose = GetNode<Button>("%PauseMenuClose");
        saveMenuClose = GetNode<Button>("%SaveMenuClose");

        pauseMenuContainer = GetNode<VBoxContainer>("%PauseMenuContainer");
        saveMenuContainer = GetNode<VBoxContainer>("%SaveMenuContainer");

        saveSimulationButton.Pressed += () => ToggleSavePanel(true);
        cancelSaveButton.Pressed += () => ToggleSavePanel(false);

        overwriteToggle.Pressed += ToggleSaveOverwriting;
        acceptSaveButton.Pressed += AcceptSavePressed;

        pauseMenuClose.Pressed += ClosePauseMenu;
        saveMenuClose.Pressed += ClosePauseMenu;

        timeManager.forcePause = true;
        playerCamera.controlEnabled = false;
    }
    public void ClosePauseMenu()
    {
        timeManager.forcePause = false;
        playerCamera.controlEnabled = true;
        QueueFree();   
    }
    public override void _Process(double delta)
    {
        if (overwriteToggle.ButtonPressed)
        {
            acceptSaveButton.Disabled = overwriteSaveDropdown.Selected <= 0;
        }
        else
        {
            acceptSaveButton.Disabled = saveNameEdit.Text.Length < SaveManager.MinSaveNameLength;
        }
    }

    void AcceptSavePressed()
    {
        string saveName;
        if (overwriteToggle.ButtonPressed)
        {
            // Overwrite
            saveName = SaveManager.GetSaveDatas(false)[overwriteSaveDropdown.Selected - 1].saveName;
        }
        else
        {
            // New Save
            saveName = saveNameEdit.Text;
        }
        if (saveName.Length > 0) SaveManager.CreateSave(saveName);
        ToggleSavePanel(false);
    }
    void ToggleSaveOverwriting()
    {
        saveNameEdit.Visible = !saveNameEdit.Visible;
        overwriteSaveDropdown.Visible = !overwriteSaveDropdown.Visible;

        if (overwriteSaveDropdown.Visible)
        {
            for (int i = 1; i < overwriteSaveDropdown.ItemCount; i++)
            {
                overwriteSaveDropdown.RemoveItem(i);
            }
            overwriteSaveDropdown.Selected = 0;
            foreach (SaveData saveData in SaveManager.GetSaveDatas(false))
            {
                overwriteSaveDropdown.AddItem(saveData.saveName);
            }
        }
    }
    void ToggleSavePanel(bool open)
    {
        pauseMenuContainer.Visible = !open;
        saveMenuContainer.Visible = open;
    }
}
