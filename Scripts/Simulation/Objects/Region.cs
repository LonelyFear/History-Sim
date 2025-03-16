using System;
using System.Linq;
using Godot;
using Godot.Collections;
using Dictionary = Godot.Collections.Dictionary;

public partial class Region : GodotObject
{
	public Dictionary<Vector2I, GodotObject> tiles = new Dictionary<Vector2I, GodotObject>();
    public Dictionary<Vector2I, Dictionary> biomes = new Dictionary<Vector2I, Dictionary>();
    public bool habitable;
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

    Random rng = new Random();

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

    public void CheckHabitability(){
        if (landCount > 0){
            habitable = true;
        }
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

        simManager.worldDependents += dependentChange;
        simManager.worldWorkforce += workforceChange;
    }

    public void RemovePop(Pop pop){
        if (pops.Contains(pop)){
            pops.Remove(pop);
            pop.region = null;            
            ChangePopulation(-pop.workforce, -pop.dependents);
        }
    }
    public void AddPop(Pop pop){
        if (!pops.Contains(pop)){
            pops.Add(pop);
            pop.region = this;            
            ChangePopulation(pop.workforce, pop.dependents);
        }
    }

    public void CheckPopulation(){
        long countedPopulation = 0;
        long countedDependents = 0;
        long countedWorkforce = 0;
        foreach (Pop pop in pops.ToArray()){
            if (pop.population < Pop.toNativePopulation(1)){
                simManager.DestroyPop(pop);
            }
            countedPopulation += pop.population;
            countedDependents += pop.dependents;
            countedWorkforce += pop.workforce;
        }

        if (countedPopulation != population){
            GD.PushWarning("Warning: Regional population mismatch");
        }
        if (countedDependents != dependents){
            GD.PushWarning("Warning: Regional dependents mismatch");
        }
        if (countedWorkforce != workforce){
            GD.PushWarning("Warning: Regional workforce mismatch");
        }
    }

    public void GrowPops(){
        long twc = 0;
        long tdc = 0;
        foreach (Pop pop in pops.ToArray()){
            float bRate;
            if (pop.population < Pop.toNativePopulation(2)){
                bRate = 0;
            } else {
                bRate = pop.birthRate;
            }
            if (population > maxPopulation){
                bRate *= 0.75f;
            }
            float NIR =  (bRate - pop.deathRate)/12;
            long change = Mathf.RoundToInt((pop.workforce + pop.dependents) * NIR);
            long dependentChange = Mathf.RoundToInt(change * pop.targetDependencyRatio);
            long workforceChange = change - dependentChange;
            pop.changeWorkforce(workforceChange);
            pop.changeDependents(dependentChange);

            twc += workforceChange;
            tdc += dependentChange;
            
        }
        ChangePopulation(twc, tdc);
    }

    public void MovePops(){

    }

    public void MovePop(Pop pop, Region destination, long movedWorkforce, long movedDependents){
        if (movedWorkforce > pop.workforce){
            movedWorkforce = pop.workforce;
        }
        if (movedDependents > pop.dependents){
            movedDependents = pop.dependents;
        }
        Pop merger = null;
        foreach (Pop resident in destination.pops){
            if (Culture.CheckCultureSimilarity(pop.culture, resident.culture)){
                merger = resident;
                break;
            }
        }
        if (merger != null){
            merger.changePopulation(movedWorkforce, movedDependents);
        } else {
            Pop newPop = simManager.CreatePop(movedWorkforce, movedDependents, destination, pop.tech, pop.culture, pop.profession);
        }
        pop.changePopulation(-movedWorkforce, -movedDependents);
    }

    //public void MergePops
}
