using System.Linq;
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
    public Strata strata;
    public double totalWealth;
    public double wealthPerCapita;
    public Tech tech;
    public int batchId;

    public bool canMove = true;

    const double foodNeedPerYear = 1;

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
    public static long FromNativePopulation(long simPopulation){
        return simPopulation/simPopulationMultiplier;
    }
    public static long ToNativePopulation(long population){
        return population * simPopulationMultiplier;
    }
    public void CalcWealthPerCapita(){
        wealthPerCapita = totalWealth / FromNativePopulation(population);
    }
    public double ConsumeResources(ResourceType type, double needCapitaPerYear, Economy economy = null){
        double needForPopulation = needCapitaPerYear/12d * FromNativePopulation(population);
        double unsatisfiedNeed = needForPopulation;

        foreach (var pair in economy.resources){
            SimResource resource = pair.Key;
            if (resource.types.Contains(type)){

                double amount = pair.Value;

                if (amount > unsatisfiedNeed){
                    economy.RemoveResources(resource, unsatisfiedNeed);
                    unsatisfiedNeed = 0;
                } else {
                    economy.RemoveResources(resource, amount);
                    unsatisfiedNeed -= amount;
                }
                
            }
        }
        if (unsatisfiedNeed > 0){
            GD.Print("Need unsatisfied");
            return unsatisfiedNeed;
        }
        return 0;
    }

}
public enum Strata{
    TRIBAL,
    LOW,
    SOLDIER,
    MIDDLE,
    HIGH
}
