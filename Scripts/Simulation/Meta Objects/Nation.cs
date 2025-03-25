using System;
using Godot;
using Godot.Collections;

public partial class Nation : GodotObject
{
    public string name = "Nation";
    public Color color;
    public GovernmentTypes government = GovernmentTypes.AUTOCRACY;
    public Array<Region> regions = new Array<Region>();
    public Region capital;
    public long population;

    public void AddRegion(Region region){
        if (!regions.Contains(region)){
            if (region.nation != null){
                region.nation.RemoveRegion(region);
            }
            region.nation = this;
            regions.Add(region);
        }
    }

    public void RemoveRegion(Region region){
        if (regions.Contains(region)){
            region.nation = null;
            regions.Remove(region);
        }
    }

    public static Nation CreateNation(Region region){
        Random rng = new Random();
        float r = Mathf.InverseLerp(0.2f, 1f, rng.NextSingle());
        float g = Mathf.InverseLerp(0.2f, 1f, rng.NextSingle());
        float b = Mathf.InverseLerp(0.2f, 1f, rng.NextSingle());

        if (region.nation == null){
            Nation nation = new Nation(){
                name = NameGenerator.GenerateNationName(),
                color = new Color(r, g, b)
            };
            nation.AddRegion(region);
            return nation;
        }
        
        return null;
    }
}

public enum GovernmentTypes {
    REPUBLIC,
    MONARCHY,
    AUTOCRACY,
    FEDERATION,
}
