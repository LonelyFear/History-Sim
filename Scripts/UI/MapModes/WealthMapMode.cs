using Godot;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.States.Diplomacy;

public class WealthMapMode : IMapMode
{
    public string Id => "wealth";
    public float Opacity => 1f;
    public int FocusTabId => -1;
    public bool CanSelectRegion(Region region)
    {
        return false;
    }
    public Color GetColor(Region region, out (int,int) borderId, bool isCenter = false)
    {
        Color color = IMapMode.BaseColor;
        borderId = (0,0);
        if (region.habitable)
        {
            if (region.wealth > IMapMode.simManager.maxWealth * 0.7f)
            {
                // Shows top 70% of wealth in a different color
                color = Utility.MultiColourLerp([new Color("yellow"), new Color("white")], region.wealth / IMapMode.simManager.maxWealth);
            } else
            {
                color = Utility.MultiColourLerp([new Color("black"), new Color("yellow")], region.wealth / (IMapMode.simManager.maxWealth * 0.7f));
            }
        }
        return color;
    }
    public string GetHoverText(Region region)
    {
        return "";
    }
}