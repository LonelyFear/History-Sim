using System.Collections.Generic;
public class Building
{
    public BuildingData data;
    public int level = 1;
    public long maxWorkforce;    
    public long workforce;
    public List<Pop> pops = new List<Pop>();

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
