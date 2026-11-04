using Godot;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.States.Diplomacy;

public class CultureMapMode : IMapMode
{
    public string Id => "culture";
    public float Opacity => 0.8f;
    public int FocusTabId => -1;
    public bool CanSelectRegion(Region region)
    {
        return region.population > 0;
    }
    public Color GetColor(Region region, out ulong borderId, bool isCenter = false)
    {
        Color color = IMapMode.BaseColor;
        borderId = 0;

        if (region.largestCulture != null && region.habitable)
        {
            borderId = region.largestCulture.id;
            color = region.largestCulture.color;
        }
        else if (region.habitable)
        {
            color = new Color(0, 0, 0, 1);
        }

        if (IMapMode.selectionManager.IsRegionSelected())
        {
            Culture culture = IMapMode.selectionManager.GetSelectedCulture();
            if (region.cultures.TryGetValue(culture, out long population) && population > 0 && region.largestCulture != culture && region.habitable)
            {
                color = culture.color;
                color = (color * 0.8f) + (new Color(0, 0, 0) * 0.2f);
            }
            else if (region.largestCulture != culture || !region.habitable)
            {
                color = Utility.MultiColourLerp([color, new Color(0, 0, 0)], IMapMode.UnselectedDarkness);
            }
        }       

        return color;
    }
    public string GetHoverText(Region region)
    {
        return "";
    }
}