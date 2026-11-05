using Godot;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.States.Diplomacy;

public class AllianceMapMode : IMapMode
{
    public string Id => "alliance";
    public float Opacity => 0.8f;
    public int FocusTabId => -1;
    public bool CanSelectRegion(Region region)
    {
        return region.population > 0;
    }
    public Color GetColor(Region region, out (int,int) borderId, bool isCenter = false)
    {
        Color color = IMapMode.BaseColor;

        Alliance alliance = null;
        State overlord; 
        borderId = (0,0);

        if (region.owner != null)
        {
            color = new Color(0.4f, 0.4f, 0.4f, 1);
            overlord = region.owner.GetOverlord();
            borderId = ((int)region.claimant.GetOverlord().id, (int)region.claimant.GetOverlord().id);

            alliance = overlord.GetAllianceOfType(AllianceType.ALLIANCE);
            if (alliance != null)
            {
                borderId = ((int)overlord.id, 0);
                color = alliance.leadState.displayColor;
            }   
                                
            if (overlord.capital == region && isCenter)
            {
                color = overlord.capitalColor;
            }

                                
        }            
        else if (region.habitable)
        {
            color = new Color(0, 0, 0, 1);
        }

        if (IMapMode.selectionManager.IsRegionSelected())
        {
            Alliance selectedAlliance = IMapMode.selectionManager.GetSelectedAlliance(AllianceType.ALLIANCE);
            
            if (region.owner != null)
            {
                // Darkens unrelated alliances
                if ((selectedAlliance == null && IMapMode.selectionManager.GetSelectedPolity() != region.owner.GetPolity()) || alliance != selectedAlliance)
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