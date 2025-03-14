using Godot;
using Godot.Collections;
using Dictionary = Godot.Collections.Dictionary;

public partial class Region : GodotObject
{
	public Dictionary<Vector2I, GodotObject> tiles = new Dictionary<Vector2I, GodotObject>();
    public Dictionary<Vector2I, Dictionary> biomes = new Dictionary<Vector2I, Dictionary>();
    public bool claimable = false;
    public Array<Pop> pops = new Array<Pop> ();

    public Vector2I pos;
    public float avgFertility;
    public int landCount;

    // Demographics
    public int maxPopulation = 0;
    public int population = 0;
    public int dependents = 0;    
    public int workforce = 0;

    public void calcAvgFertility(){
        landCount = 0;
        float f = 0;
        foreach (Dictionary biome in biomes.Values){
            if ((float)biome["terrainType"] == 0){
                landCount++;
                f += (float)biome["fertility"];
            }
        }
        avgFertility = (f/landCount);
    }
    public void calcMaxPopulation(){
        foreach (Vector2I bpos in biomes.Keys){
            Dictionary biome = biomes[bpos];
            GodotObject tile = tiles[bpos];

            tile.Set("maxPopulation", 0);
            if ((float)biome["terrainType"] == 0){
                maxPopulation += (int)(Pop.toSimPopulation(1000) * (float)biome["fertility"]);
                tile.Set("maxPopulation", (int)(Pop.toSimPopulation(1000) * (float)biome["fertility"])) ;
            }
        }
    }

    public void changePopulation(int workforceChange, int dependentChange){
        workforce += workforceChange;
        dependents += dependentChange;
        population += (workforceChange + dependentChange);
    }

    public void removePop(Pop pop){
        if (pops.Contains(pop)){
            changePopulation(-(int)pop.Get("workforce"), -(int)pop.Get("dependents"));
            pops.Remove(pop);
            pop.region = null;
        }
    }
    public void addPop(Pop pop){
        if (!pops.Contains(pop)){
            changePopulation((int)pop.Get("workforce"), (int)pop.Get("dependents"));
            pops.Add(pop);
            pop.region = this;
        }
    }

    void growPops(){
        foreach (Pop pop in pops){
            float bRate;
            if ((int)pop.Get("population") > 2){
                bRate = 0;
            } else {
                bRate = (float)pop.Get("birthRate");
            }
            if (population > maxPopulation){
                bRate *= 0.75f;
            }
            float NIR =  bRate - (float)pop.Get("deathRate");
            int increase = Mathf.RoundToInt(((int)pop.Get("workforce") + (int)pop.Get("dependents")) * NIR);
            int dependentIncrease = Mathf.RoundToInt(increase * (float)pop.Get("targetDependencyRatio"));

            pop.Call("changeWorkforce", increase - dependentIncrease);
            pop.Call("changeDependents", dependentIncrease);
        }
    }
}
