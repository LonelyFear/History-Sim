using Godot;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.States.Diplomacy;

public class PopulationMapMode : IMapMode
{
    public string Id => "population";
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

        if (region.habitable && region.pops.Count > 0)
        {
            long minRecordedPopulation = 1000 * (int)Mathf.Pow(RegionGenerator.tilesPerRegion, 2);
            if (IMapMode.simManager.highestPopulation > minRecordedPopulation && region.population > IMapMode.simManager.highestPopulation * 0.7f)
            {
                // Shows top 70% of populations in a different color
                color = Utility.MultiColourLerp([new Color("green"), new Color("white")], region.population / (float)IMapMode.simManager.highestPopulation);
            } else
            {
                color = Utility.MultiColourLerp([new Color("black"), new Color("green")], region.population / Mathf.Max(IMapMode.simManager.highestPopulation * 0.7f, minRecordedPopulation));
            }
        }
        else if (region.habitable)
        {
            color = new Color(0, 0, 0, 1);
        }

        return color;
    }
    public string GetHoverText(Region region)
    {
        return "";
    }
}