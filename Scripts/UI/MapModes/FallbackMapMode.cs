using Godot;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.States.Diplomacy;

public class FallbackMapMode : IMapMode
{
    public string Id => "fallback";
    public float Opacity => 0;
    public int FocusTabId => -1;
    public bool CanSelectRegion(Region region)
    {
        return region.habitable;
    }
    public Color GetColor(Region region, out ulong borderId, bool isCenter = false)
    {
        borderId = 0;
        return IMapMode.BaseColor;
    }
    public string GetHoverText(Region region)
    {
        return "";
    }
}