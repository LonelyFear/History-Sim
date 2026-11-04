using Godot;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.States.Diplomacy;

public class TechMapMode : IMapMode
{
    public string Id => "tech";
    public float Opacity => 1f;
    public int FocusTabId => -1;
    public bool CanSelectRegion(Region region)
    {
        return false;
    }
    public Color GetColor(Region region, out ulong borderId, bool isCenter = false)
    {
        Color color = IMapMode.BaseColor;
        borderId = 0;
        if (region.population > 0)
        {
            color = new Color(region.tech.militaryLevel / 20f, region.tech.industryLevel / 20f, region.tech.societyLevel / 20f);
        }
        return color;
    }
    public string GetHoverText(Region region)
    {
        return "";
    }
}