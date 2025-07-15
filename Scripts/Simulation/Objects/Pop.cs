using System;
using System.Collections.Generic;
using Godot;

public class Pop
{
    public long population = 0;
    public long workforce = 0;
    public long dependents = 0;

    public float baseBirthRate = 0.3f;
    public float baseDeathRate = 0.29f;
    public float starvingPercentage = 0f;
    public float targetDependencyRatio = 0.75f;
    public float netIncome = 0f;
    public Region region;
    public Culture culture;
    public Profession profession = Profession.FARMER;
    
    public Tech tech;
    public int batchId;

    public bool canMove = true;
    public const float workforceNutritionNeed = 1f;
    public const float dependentNutritionNeed = .8f;
    public List<Character> characters = new List<Character>();
    public static SimManager simManager;
    public static Random rng = new Random();
    public float income = 0f;
    public float expenses = 0f;
    public float wealth = 0f;
    public static Curve starvationCurve = GD.Load<Curve>("res://Curves/Simulation/StarvationCurve.tres");
    public static Curve farmerProductionCurve = GD.Load<Curve>("res://Curves/Simulation/FarmerProductivityCurve.tres");

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
    #region Starvin'
    public double GetRequiredNutrition()
    {
        return ((FromNativePopulation(workforce) * workforceNutritionNeed) + (FromNativePopulation(dependents) * dependentNutritionNeed)) * 1.0;
    }
    /*
    public void ConsumeFood()
    {
        starvingPercentage = 0;
        double nutritionRequired = GetRequiredNutrition();
        double nutritionSatisfied = 0;
        foreach (BaseResource resource in region.economy.GetResources())
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
            //GD.Print(starvingPercentage);
        }
    }
    */

    #endregion

    #region Economy
    public void ProfessionUpdate()
    {
        switch (profession)
        {
            case Profession.FARMER:
                // Farmer Stuff
                float baseFarmerIncome = 0.2f;
                float arableLandRatio = region.arableLand / region.landCount;
                float harvestMultiplier = Mathf.Lerp(1f, 1.5f, rng.NextSingle());
                float landUseRatio = 1f;
                if (FromNativePopulation(region.professions[Profession.FARMER]) > 0) {
                    landUseRatio = FromNativePopulation(workforce) / FromNativePopulation(region.professions[Profession.FARMER]);
                }
                income += arableLandRatio * (baseFarmerIncome * landUseRatio) * harvestMultiplier * farmerProductionCurve.Sample(workforce);
                break;
        }
    }
    public void Consumption()
    {
        starvingPercentage = 0;
        float expenses = FromNativePopulation(population) / 80000f;

        netIncome = income - expenses;
        wealth += netIncome;
    }    
    #endregion
    public void Migrate()
    {
        // Chance of pop to migrate
        float migrateChance = 0.0005f;
        // Simple Migration
        if (region.population >= region.maxPopulation * 0.95f)
        {
            migrateChance = 1f;
        }

        if (profession == Profession.ARISTOCRAT)
        {
            migrateChance *= 0.1f;
        }

        // If the pop migrates
        if (rng.NextSingle() <= migrateChance)
        {
            Region target = region.borderingRegions[rng.Next(0, region.borderingRegions.Count)];

            bool canMigrate = profession == Profession.FARMER || region.owner != null;
            if (region.owner != null && region.owner.rulingPop == this)
            {
                canMigrate = region.owner == target.owner;
            }

            if (target.Migrateable(this) && canMigrate && (rng.NextSingle() < target.navigability) && (rng.NextSingle() < target.arableLand/target.landCount))
            {
                long movedDependents = (long)(dependents * Mathf.Lerp(0.05, 0.5, rng.NextDouble()));
                long movedWorkforce = (long)(workforce * Mathf.Lerp(0.05, 0.5, rng.NextDouble()));
                float movedWorkforceRatio = movedWorkforce/(float)workforce;
                region.MovePop(this, target, movedWorkforce, movedDependents, wealth * movedWorkforceRatio);
            }
        }
    }
    public float GetDeathRate()
    {
        float deathRate = baseDeathRate;
        deathRate += Mathf.Clamp(starvationCurve.Sample(starvingPercentage * 0.8f), 0f, 1f);
        //GD.Print(rateInBracket); 
        return deathRate;
    }
    public float GetBirthRate()
    {
        float birthRate = baseBirthRate;
        return birthRate;
    }
}


public enum Profession
{
    FARMER,
    SOLDIER,
    ARTISAN,
    MERCHANT,
    ARISTOCRAT
}
