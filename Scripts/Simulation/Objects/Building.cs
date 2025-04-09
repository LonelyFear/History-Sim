using Godot;
using Godot.Collections;
using System;
public partial class Building : GodotObject
{
    public BuildingData data;
    public int level = 1;
    public long maxWorkforce;    
    public long workforce;
    public Array<Pop> pops = new Array<Pop>();

    public bool LevelUp(){
        if (level < data.maxLevel){
            level++;
            maxWorkforce = data.baseOccupancy * level;
            return true;
        }
        return false;
    }
    public void InitBuilding(BuildingData data){
        workforce = 0;
        level = 1;
        maxWorkforce = Pop.ToNativePopulation(data.baseOccupancy * level);
    }
}
