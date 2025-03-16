using System;
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
    public SimManager simManager;

    // Demographics
    public long maxPopulation = 0;
    public long population = 0;
    public long dependents = 0;    
    public long workforce = 0;

    public int currentMonth;
    public void CalcAvgFertility(){
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

    public void CalcMaxPopulation(){
        foreach (Vector2I bpos in biomes.Keys){
            Dictionary biome = biomes[bpos];
            GodotObject tile = tiles[bpos];

            tile.Set("maxPopulation", 0);
            if ((float)biome["terrainType"] == 0){
                maxPopulation += (long)(Pop.toNativePopulation(1000) * (float)biome["fertility"]);
                tile.Set("maxPopulation", (long)(Pop.toNativePopulation(1000) * (float)biome["fertility"])) ;
            }
        }
    }

    public void ChangePopulation(long workforceChange, long dependentChange){
        workforce += workforceChange;
        dependents += dependentChange;
        population += workforceChange + dependentChange;

        simManager.worldPopulation += workforceChange + dependentChange;
    }

    public void RemovePop(Pop pop){
        if (pops.Contains(pop)){
            ChangePopulation(pop.workforce, pop.dependents);
            pops.Remove(pop);
            pop.region = null;
        }
    }
    public void addPop(Pop pop){
        if (!pops.Contains(pop)){
            ChangePopulation(pop.workforce, pop.dependents);
            pops.Add(pop);
            pop.region = this;
        }
    }

    public void growPops(){
        long twc = 0;
        long tdc = 0;
        foreach (Pop pop in pops){
            float bRate;
            if (pop.population < 2){
                bRate = 0;
            } else {
                bRate = pop.birthRate;
            }
            if (population > maxPopulation){
                bRate *= 0.75f;
            }
            float NIR =  (bRate - pop.deathRate)/12;
            long increase = Mathf.RoundToInt((pop.workforce + pop.dependents) * NIR);
            long dependentIncrease = Mathf.RoundToInt(increase * pop.targetDependencyRatio);

            pop.changeWorkforce(increase - dependentIncrease);
            pop.changeDependents(dependentIncrease);
            twc += increase - dependentIncrease;
            tdc += dependentIncrease;
            
        }
        ChangePopulation(twc, tdc);
    }
}
