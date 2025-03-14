using Godot;

public partial class Pop : GodotObject
{
    public int population = 0;
    public int workforce = 0;
    public int dependents = 0;

    public Region region;
    public Culture culture;

    public void changeWorkforce(int amount){
        if (workforce + amount < 0){
            amount = -workforce;
        }
        workforce += amount;
        population += amount;
        if (region != null){
            region.changePopulation(amount, 0);
        }
    }
    public void changeDependents(int amount){
        if (dependents + amount < 0){
            amount = -dependents;
        }
        dependents += amount;
        population += amount;
        if (region != null){
            region.changePopulation(amount, 0);
        }
    }

    public const int simPopulationMultiplier = 1000;
    public static int fromSimPopulation(int simPopulation){
        return simPopulation/simPopulationMultiplier;
    }
    public static int toSimPopulation(int population){
        return population * simPopulationMultiplier;
    }
}
