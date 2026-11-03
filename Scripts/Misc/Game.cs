using Godot;
using System;

public partial class Game : Node2D
{
    [Export] public GameUI uiLayer;
    bool uiVisible = true;

    public override void _Process(double delta)
    {
        
        if (Input.IsActionJustPressed("Toggle_UI"))
        {
            uiVisible = !uiVisible;
            uiLayer.show = uiVisible;
        }
        if (Input.IsActionJustPressed("Take_Screenshot"))
        {
            TakeScreenshot();
        }
        
    }
    public void TakeScreenshot()
    {
        string screenshotsFolderPath = "user://screenshots";
        if (DirAccess.Open(screenshotsFolderPath) == null)
        {
            DirAccess.MakeDirAbsolute(screenshotsFolderPath);
        }

        Image screenImage = GetViewport().GetTexture().GetImage();
        string screenshotName = "screenshot" + (DirAccess.Open(screenshotsFolderPath).GetFiles().Length + 1);
        string screenshotPath = screenshotsFolderPath.PathJoin(screenshotName);
        screenImage.SavePng(screenshotPath + ".png");
        GD.Print($"Screenshot {screenshotName} saved at path {screenshotPath}");
    }
}
