using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Godot;

public class Pop
{
    public long population = 0;
    public long workforce = 0;
    public long dependents = 0;

    public float baseBirthRate = 0.3f;
    public float baseDeathRate = 0.29f;
    public float deathRate = 0.29f;
    public float birthRate = 0.3f;
    public float starvingPercentage = 0;
    public float targetDependencyRatio = 0.75f;
    public Region region;
    public Culture culture;
    public Profession profession = Profession.FARMER;
    public double totalWealth;
    public double wealthPerCapita;
    public Tech tech;
    public int batchId;

    public bool canMove = true;
    public const float workforceNutritionNeed = 1f;
    public const float dependentNutritionNeed = .8f;
    public List<Character> characters = new List<Character>();
    public static SimManager simManager;
    public static Random rng = new Random();

    public void ChangeWorkforce(long amount)
    {
        if (workforce + amount < 0)
        {
            amount = -workforce;
        }
        workforce += amount;
        population += amount;
        SimManager.m.WaitOne();
        if (culture != null)
        {
            culture.ChangePopulation(amount, 0);
        }
        SimManager.m.ReleaseMutex();
    }
    public void ChangeDependents(long amount)
    {
        if (dependents + amount < 0)
        {
            amount = -dependents;
        }
        dependents += amount;
        population += amount;
        SimManager.m.WaitOne();
        if (culture != null)
        {
            culture.ChangePopulation(0, amount);
        }
        SimManager.m.ReleaseMutex();
    }
    public void ChangePopulation(long workforceChange, long dependentChange)
    {
        ChangeWorkforce(workforceChange);
        ChangeDependents(dependentChange);
    }

    public const long simPopulationMultiplier = 1000;
    public static long FromNativePopulation(long simPopulation)
    {
        return simPopulation / simPopulationMultiplier;
    }
    public static long ToNativePopulation(long population)
    {
        return population * simPopulationMultiplier;
    }
    public void CalcWealthPerCapita()
    {
        wealthPerCapita = totalWealth / FromNativePopulation(population);
    }
    public static bool CanPopsMerge(Pop a, Pop b)
    {
        if (a == null || b == null || a == b)
        {
            return false;
        }
        return a != b && a.profession == b.profession && Culture.CheckCultureSimilarity(a.culture, b.culture);
    }
    public void AddCharacter(Character character)
    {
        try
        {
            if (character == null)
            {
                GD.PushError("Error: Null character added, something is probably broken");
            }
            else if (character != null && !characters.Contains(character))
            {
                if (character.pop != null)
                {
                    character.pop.RemoveCharacter(character);
                }
                if (region == null)
                {
                    GD.Print("wtf");
                    GD.Print(Enum.GetName(typeof(Profession), profession));
                }
                character.state = region.owner;
                character.pop = this;
                characters.Add(character);
            }
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }
    }
    public void RemoveCharacter(Character character)
    {
        if (characters.Contains(character))
        {
            characters.Remove(character);
            character.pop = null;
        }
    }
    public Pop ChangeProfession(long workforceDelta, long dependentDelta, Profession newProfession)
    {
        // Makes sure the profession is actually changing
        // And that we arent just creating an empty pop
        if (newProfession == profession && (workforceDelta >= ToNativePopulation(1) || dependentDelta >= ToNativePopulation(1)))
        {
            return null;
        }
        // Clamping
        workforceDelta = Math.Clamp(workforceDelta, 0, workforce);
        dependentDelta = Math.Clamp(dependentDelta, 0, dependents);

        // If we are changing the whole pop just change the profession
        if (workforceDelta == workforce && dependentDelta == dependents)
        {
            profession = newProfession;
            return this;
        }
        // Makes a new pop with the new profession
        Pop newWorkers = simManager.CreatePop(workforceDelta, dependentDelta, region, tech, culture, newProfession);
        // And removes the people who switched to the new profession
        ChangePopulation(-workforceDelta, -dependentDelta);
        return newWorkers;
    }
    #region Farmin' & Starvin'
    public void ConsumeFood()
    {
        double nutritionRequired = ((FromNativePopulation(workforce) * workforceNutritionNeed) + (FromNativePopulation(dependents) * dependentNutritionNeed)) * 1.0;
        double nutritionSatisfied = 0;
        foreach (BaseResource resource in region.economy.resources.Keys)
        {
            if (resource.IsFood())
            {
                FoodResouce foodstuff = (FoodResouce)resource;
                double nutritionForFood = foodstuff.nutrition * region.economy.GetResourceAmount(foodstuff);

                double nutritionRemaining = Mathf.Clamp(nutritionForFood, 0, nutritionRequired - nutritionSatisfied);
                nutritionSatisfied += nutritionRemaining;

                region.economy.ChangeResourceAmount(foodstuff, -(nutritionRemaining / foodstuff.nutrition));
            }
        }

        if (nutritionSatisfied < nutritionRequired)
        {
            starvingPercentage = 1 - (float)(nutritionSatisfied / nutritionRequired);
            starvingPercentage = Mathf.Clamp(starvingPercentage, 0, 1);
        }
    }
    public Crop SelectCrop()
    {
        if (region.plantableCrops.Count > 0)
        {
            return region.plantableCrops[0];
        }
        else
        {
            return null;
        }
    }

    public void GrowCrops()
    {
        Crop crop = SelectCrop();
        if (crop != null)
        {
            double totalWork = FromNativePopulation(workforce) * 5f * Mathf.Lerp(rng.NextSingle(), 0.95f, 1f);
            foreach (BaseResource yield in crop.yields.Keys)
            {
                region.economy.ChangeResourceAmount(yield, crop.yields[yield] * totalWork);
            }
        }
    }
    #endregion
    #region Jobs
    public void ProfessionUpdate()
    {
        switch (profession)
        {
            case Profession.FARMER:
                GrowCrops();
                break;
        }
    }
    #endregion
}


public enum Profession
{
    FARMER,
    SOLDIER,
    ARTISAN,
    MERCHANT,
    ARISTOCRAT
}
