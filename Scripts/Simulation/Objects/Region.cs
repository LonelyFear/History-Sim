using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Godot;
using Godot.Collections;
using Dictionary = Godot.Collections.Dictionary;

public partial class Region : GodotObject
{
	public Tile[,] tiles;
    public Biome[,] biomes;
    public bool habitable;
    public bool coastal;
    public Array<Pop> pops = new Array<Pop> ();

    public Vector2I pos;
    public float avgFertility;
    public int landCount;
    public SimManager simManager;
    public State owner;

    // Demographics
    public long maxPopulation = 0;
    public long population = 0;
    public long dependents = 0;    
    public long workforce = 0;

    Random rng = new Random();

    public int currentMonth;
    public bool border;
    public bool frontier;
    public void CalcAvgFertility(){
        landCount = 0;
        float f = 0;
        for (int x = 0; x < simManager.tilesPerRegion; x++){
            for (int y = 0; y < simManager.tilesPerRegion; y++){
                Biome biome = biomes[x,y];
                if (biome.terrainType == Biome.TerrainType.LAND){
                    landCount++;
                    f += biome.fertility;
                } else if (biome.terrainType == Biome.TerrainType.WATER){
                    coastal = true;
                }               
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
        for (int x = 0; x < simManager.tilesPerRegion; x++){
            for (int y = 0; y < simManager.tilesPerRegion; y++){
                Biome biome = biomes[x, y];
                Tile tile = tiles[x,y];

                tile.maxPopulation = 0;
                if (biome.terrainType == Biome.TerrainType.LAND){
                    maxPopulation += (long)(Pop.ToNativePopulation(1000) * biome.fertility);
                    tile.maxPopulation = (long)(Pop.ToNativePopulation(1000) * biome.fertility);
                }
            }
        }
    }

    public void ChangePopulation(long workforceChange, long dependentChange){
        workforce += workforceChange;
        dependents += dependentChange;
        population += workforceChange + dependentChange;
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
            if (pop.region != null){
                pop.region.RemovePop(pop);
            }
            pops.Add(pop);
            pop.region = this;            
            ChangePopulation(pop.workforce, pop.dependents);
        }
    }
    #region Nations
    public void RandomStateFormation(){
        if (owner == null && population > Pop.ToNativePopulation(1000) && rng.NextDouble() <= 0.0001){
            simManager.CreateNation(this);
        }
    }

    public void StateBordering(){
        border = false;
        frontier = false;
        for (int dx = -1; dx < 2; dx++){
            for (int dy = -1; dy < 2; dy++){
                if ((dx != 0  && dy != 0) || (dx == 0 && dy == 0)){
                    continue;
                }
                Region region = simManager.GetRegion(pos.X + dx, pos.Y + dy);
                if (region.owner == null){
                    frontier = true;
                }
                if (region.owner != null && region.owner != owner){
                    border = true;
                    if (!owner.borderingStates.Contains(region.owner)){
                        owner.borderingStates.Append(region.owner);                        
                    }
                }
            }
        }
    }
    #endregion

    public void CheckPopulation(){
        long countedPopulation = 0;
        long countedDependents = 0;
        long countedWorkforce = 0;
        foreach (Pop pop in pops.ToArray()){
            if (pop.population < Pop.ToNativePopulation(1)){
                pops.Remove(pop);
                simManager.pops.Remove(pop);
                continue;
            }
            countedPopulation += pop.population;
            countedWorkforce += pop.workforce;
            countedDependents += pop.dependents;
        }
        if (countedPopulation < Pop.ToNativePopulation(1) && owner != null){
            owner.RemoveRegion(this);
        }
        population = countedPopulation;
        dependents = countedDependents;
        workforce = countedWorkforce;
    }

    public void MergePops(){
        foreach (Pop pop in pops){
            if (pop.population >= Pop.ToNativePopulation(1)){
                foreach (Pop merger in pops){
                    if (pop != merger && Culture.CheckCultureSimilarity(pop.culture, merger.culture)){
                        merger.ChangePopulation(pop.workforce, pop.dependents);
                        pop.ChangePopulation(-pop.workforce, -pop.dependents);
                        break;
                    }
                }                
            }

        }
    }

    public void ClearEmptyPops(){
        foreach (Pop pop in pops.ToArray()){
            if (pop.population < Pop.ToNativePopulation(1)){
                simManager.DestroyPop(pop);
            }
        }
    }

    public void GrowPops(){
        long twc = 0;
        long tdc = 0;
        foreach (Pop pop in pops.ToArray()){
            pop.canMove = true;

            float bRate;
            if (pop.population < Pop.ToNativePopulation(2)){
                bRate = 0;
            } else {
                bRate = pop.birthRate;
            }
            if (population > maxPopulation){
                bRate *= 0.75f;
            }
            float NIR =  (bRate - pop.deathRate)/12f;
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
        foreach (Pop pop in pops.ToArray()){
            // Chance of pop to migrate
            double migrateChance = 0.0005;

            // Pops are most likely to migrate if their region is overpopulated
            if (population >= maxPopulation * 0.95f){
                migrateChance = 0.01;
            }

            // If the pop migrates
            if (rng.NextDouble() <= migrateChance){
                for (int dx = -1; dx < 2; dx++){
                    for (int dy = -1; dy < 2; dy++){
                        // Removes our region to avoid any messy behavior
                        if (dx == 0 && dy == 0 || dx != 0 && dy != 0){
                            continue;
                        }
                        // Gets the tested region
                        Region region = simManager.GetRegion(pos.X + dx, pos.Y + dy);

                        if (region.habitable && rng.NextDouble() <= 0.25d){
                            MovePop(pop, region, (long)(pop.workforce * Mathf.Lerp(0.05, 0.5, rng.NextDouble())), (long)(pop.dependents * Mathf.Lerp(0.05, 0.5, rng.NextDouble())));
                            return;
                        }
                    }
                } 
            }
        }
    }

    public void MovePop(Pop pop, Region destination, long movedWorkforce, long movedDependents){
        if (destination != this && movedWorkforce >= Pop.ToNativePopulation(1) || movedDependents >= Pop.ToNativePopulation(1)){
            if (movedWorkforce > pop.workforce){
                movedWorkforce = pop.workforce;
            }
            if (movedDependents > pop.dependents){
                movedDependents = pop.dependents;
            }
            Pop merger = null;
            foreach (Pop resident in destination.pops.ToArray()){
                if (Culture.CheckCultureSimilarity(pop.culture, resident.culture)){
                    merger = resident;
                    break;
                }
            }
            if (merger != null){
                merger.ChangePopulation(movedWorkforce, movedDependents);
                merger.canMove = false;
            } else {
                Pop npop = simManager.CreatePop(movedWorkforce, movedDependents, destination, pop.tech, pop.culture, pop.strata);
                npop.canMove = false;
            }
            pop.ChangePopulation(-movedWorkforce, -movedDependents);     
        }

    }

    //public void MergePops
}
