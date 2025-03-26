using System;
using Godot;
using Godot.Collections;

public partial class State : GodotObject
{
    public string name = "Nation";
    public Color color;
    public Color displayColor;

    public GovernmentTypes government = GovernmentTypes.AUTOCRACY;
    public Array<Region> regions = new Array<Region>();
    public Region capital;
    public long population;

    Array<State> vassals;
    State liege;
    Dictionary<State, Relation> relations;
    Sovereignty sovereignty = Sovereignty.INDEPENDENT;


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
}

public enum GovernmentTypes {
    REPUBLIC,
    MONARCHY,
    AUTOCRACY,
    FEDERATION,
}

public enum Sovereignty {
    INDEPENDENT,
    DOMINION,
    PUPPET,
    COLONY, 
    FEDERAL_STATE,
    PROVINCE
}
