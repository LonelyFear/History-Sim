using System;
using System.Linq;
using System.Threading.Tasks;
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
    public long workforce;
    public long military;
    Array<State> vassals;
    State liege;
    Dictionary<State, Relation> relations;
    public Array<State> borderingStates;
    Sovereignty sovereignty = Sovereignty.INDEPENDENT;
    public Economy economy = new Economy();

    public void CountPopulation(){
        long countedP = 0;
        long countedW = 0;
        foreach (Region region in regions.ToArray()){
            countedP += region.population;
            countedW += region.workforce;
        }
        population = countedP;
        workforce = countedW;
    }
    public void AddRegion(Region region){
        if (!regions.Contains(region)){
            if (region.owner != null){
                region.owner.RemoveRegion(region);
            }
            region.owner = this;
            regions.Add(region);
        }
    }

    public void RemoveRegion(Region region){
        if (regions.Contains(region)){
            region.owner = null;
            regions.Remove(region);
        }
    }

    public void SetStateSovereignty(State state, Sovereignty sovereignty){
        if (state != this){
            if (sovereignty == Sovereignty.INDEPENDENT){
                if (vassals.Contains(state)){
                    state.sovereignty = Sovereignty.INDEPENDENT;
                    vassals.Remove(state);
                }                 
            } else {
                if (vassals.Contains(state)){
                    state.sovereignty = sovereignty;
                } else {
                    AddVassal(state, sovereignty);
                }                
            }
        }
    }

    public void AddVassal(State state, Sovereignty sovereignty = Sovereignty.PUPPET){
        if (sovereignty != Sovereignty.INDEPENDENT){
            if (state.liege != null){
                state.liege.RemoveVassal(state);
            }
            state.liege = this;
            state.sovereignty = sovereignty;
            vassals.Add(state);                
        }
    }

    public void RemoveVassal(State state){
        if (vassals.Contains(state)){
            state.liege = null;
            state.sovereignty = Sovereignty.INDEPENDENT;
            vassals.Remove(state);
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
