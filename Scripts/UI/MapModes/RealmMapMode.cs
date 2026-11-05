using Godot;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.States.Diplomacy;

public class RealmMapMode : IMapMode
{
    public string Id => "realm";
    public float Opacity => 0.8f;
    public int FocusTabId => 1;

    public bool CanSelectRegion(Region region)
    {
        return region.habitable;
    }
    public Color GetColor(Region region, out (int,int) borderId, bool isCenter = false)
    {
        Color color = IMapMode.BaseColor;
        borderId = (0,0);

        if (region.pops.Count > 0)
        {
            borderId = (1,1);
            color = new Color(0.2f, 0.2f, 0.2f, 1);
        }
        if (region.owner != null)
        {
            // Draws borders between polities
            borderId = ((int)region.claimant.id, (int)region.claimant.GetOverlord().id);
            
            color = region.owner.displayColor;
            if (region.claimant.capital == region && region.claimant.sovereignty == Sovereignty.INDEPENDENT && isCenter)
            {
                color = region.claimant.capitalColor;
            }
        }                 
        if (IMapMode.selectionManager.IsRegionSelected())
        {
            State selectedState = IMapMode.selectionManager.GetSelectedState();
            if (selectedState != null && region.owner != null)
            {
                // Darkens unrelated states
                if (region.owner != null && region.owner.GetOverlord() != selectedState.GetOverlord())
                {
                    color = Utility.MultiColourLerp([color, new Color(0, 0, 0)], 0.7f);
                }                      
            }
            else
            {
                if (region != IMapMode.selectionManager.GetSelectedRegion())
                {
                    color = Utility.MultiColourLerp([color, new Color(0, 0, 0)], IMapMode.UnselectedDarkness);
                }                        
            }
        }
        return color;
    }
    public string GetHoverText(Region region)
    {
        return "";
    }
}