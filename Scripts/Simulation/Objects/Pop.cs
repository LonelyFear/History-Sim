using System;
using System.Linq;
using Godot;
using Godot.Collections;

public partial class Pop : GodotObject
{
    public long population = 0;
    public long workforce = 0;
    public long dependents = 0;

    public float baseBirthRate = 0.3f;
    public float baseDeathRate = 0.29f;
    public float deathRate = 0.29f;
    public float birthRate = 0.3f;
    public float targetDependencyRatio = 0.75f;
    public Region region;
    public Culture culture;
    public Profession profession;
    public double totalWealth;
    public double wealthPerCapita;
    public Tech tech;
    public int batchId;

    public bool canMove = true;
    public const double foodPerCapita = 1.0;
    public const double dependentNeedMultiplier = .8;
    public Array<Character> characters = new Array<Character>();

    public void ChangeWorkforce(long amount){
        if (workforce + amount < 0){
            amount = -workforce;
        }
        workforce += amount;
        population += amount;
        if (culture != null){
            culture.ChangePopulation(amount);
        }

    }
    public void ChangeDependents(long amount){
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
        ChangeWorkforce(workforceChange);
        ChangeDependents(dependentChange);
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

    public long GetConsumptionPopulation(){
        return (long)(FromNativePopulation(workforce) + (FromNativePopulation(dependents) * dependentNeedMultiplier));
    }

    public double ConsumeResources(ResourceType type, double needPerCapita, Economy economy){
        double needForPopulation = needPerCapita * GetConsumptionPopulation();
        double unsatisfiedNeed = needForPopulation;

        foreach (var pair in economy.resources){
            SimResource resource = pair.Key;
            if (resource.types.Contains(type)){

                double amount = pair.Value;

                if (amount > unsatisfiedNeed){
                    economy.ChangeResourceAmount(resource, -unsatisfiedNeed);
                    unsatisfiedNeed = 0;
                } else {
                    economy.ChangeResourceAmount(resource, -amount);
                    unsatisfiedNeed -= amount;
                }
                
            }
        }
        return unsatisfiedNeed;
    }

    public static bool CanPopsMerge(Pop a, Pop b){
        if (a == null || b == null){
            return false;
        }
        return a != b && a.profession == b.profession && Culture.CheckCultureSimilarity(a.culture, b.culture);
    }
    public void AddCharacter(Character character){
        try {
            if (!characters.Contains(character)){
                if (character.pop != null){
                    character.pop.RemoveCharacter(character);
                } 
                ChangePopulation(Pop.ToNativePopulation(1), 0);
                character.state = region.owner;
                character.pop = this;
                characters.Add(character);
            }
        } catch (Exception e){
            GD.Print(e);
        }
    }
    public void RemoveCharacter(Character character){
        if (characters.Contains(character)){
            if (workforce >= Pop.ToNativePopulation(1)){
                ChangePopulation(Pop.ToNativePopulation(-1), 0);
            }
            characters.Remove(character);
            character.pop = null;
        }
    }
}
public enum Profession{
    FARMER,
    MERCHANT,
    ARISTOCRAT
}
