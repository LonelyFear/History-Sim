using Godot;

public partial class Pop : GodotObject
{
    public long population = 0;
    public long workforce = 0;
    public long dependents = 0;

    public float birthRate = 0.3f;
    public float deathRate = 0.29f;
    public float targetDependencyRatio = 0.75f;
    public Region region;
    public Culture culture;
    public Professions profession;
    public Tech tech;
    public int batchId;

    public bool canMove = true;

    public void changeWorkforce(long amount){
        if (workforce + amount < 0){
            amount = -workforce;
        }
        workforce += amount;
        population += amount;
        if (culture != null){
            culture.ChangePopulation(amount);
        }

    }
    public void changeDependents(long amount){
        if (dependents + amount < 0){
            amount = -dependents;
        }
        dependents += amount;
        population += amount;
        if (culture != null){
            culture.ChangePopulation(amount);
        }
    }
    public void ChangePopulation(long workforceChange, long dependentChange){
        changeWorkforce(workforceChange);
        changeDependents(dependentChange);
    }

    public const long simPopulationMultiplier = 1000;
    public static long fromNativePopulation(long simPopulation){
        return simPopulation/simPopulationMultiplier;
    }
    public static long toNativePopulation(long population){
        return population * simPopulationMultiplier;
    }
}
public enum Professions{
    TRIBESPEOPLE,
	PEASANT,
	UNEMPLOYED,
	WORKERS,
	SKILLED,
	ARISTOCRATS
}
