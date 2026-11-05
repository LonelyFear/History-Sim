using Godot;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.States.Diplomacy;

public class TradeZoneMapMode : IMapMode
{
    public string Id => "trade_zone";
    public float Opacity => 0.8f;
    public int FocusTabId => -1;
    public bool CanSelectRegion(Region region)
    {
        return region.population > 0;
    }
    public Color GetColor(Region region, out (int,int) borderId, bool isCenter = false)
    {
        Color color = IMapMode.BaseColor;
        borderId = (0,0);
        if (region.habitable)
        {
            color = new Color(0f, 0f, 0f);
            if (region.tradeZone != null)
            {
                borderId = ((int)region.tradeZone.id, (int)region.tradeZone.id);
                color = region.tradeZone.color;
            }
            if (region.isTradeZoneCenter && isCenter) color = new Color("Yellow");
        }   
        return color;
    }
    public string GetHoverText(Region region)
    {
        return "";
    }
}