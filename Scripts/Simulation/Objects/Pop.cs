using System;
using System.Collections.Generic;
using System.Data.Common;
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
    public double GetRequiredNutrition() {
        return ((FromNativePopulation(workforce) * workforceNutritionNeed) + (FromNativePopulation(dependents) * dependentNutritionNeed)) * 1.0;
    }
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

    #endregion
    #region Jobs
    public void ProfessionUpdate()
    {
        switch (profession)
        {
            case Profession.FARMER:
                //GrowCrops();
                break;
            case Profession.MERCHANT:
                if (simManager.complexEconomy)
                {
                    ComplexTrade();
                }
                else
                {
                    
                }
                
                break;
        }
    }
    #region Simple Economy
    public void SimpleProfessionTransitions()
    {
        
    }
    public void SimpleTrade()
    {
        
    }
    #endregion
    #region Complex Economy
    public void ComplexTrade()
    {
        Region target = region.PickRandomBorder();
        if (rng.NextSingle() < 0.1 && target.tradeWeight < 100)
        {
            return;
        }
        if (target.pops.Count > 0)
        {
            if (region.economy.GetTotalFoodAmount() > target.economy.GetTotalFoodAmount())
            {
                target.tradeWeight += 1;
                region.economy.TransferResources(target.economy, region.economy.GetAllFood(), region.economy.GetTotalFoodAmount() * 0.05f);
            }
        }
    }
    public void ComplexProfessionTransitions()
    {
        // Profession Transitions
        switch (profession)
        {
            case Profession.FARMER:
                bool surplusMercantilism = region.fieldsFull || region.economy.GetTotalNutrition() > GetRequiredNutrition() * 2 && FromNativePopulation(population) > 1000;
                bool tradeMercantilism = region.tradeWeight >= 100;
                if (surplusMercantilism || tradeMercantilism)
                {
                    // Farmer To Merchant
                    ChangeProfession((long)(workforce * 0.01), (long)(dependents * 0.01), Profession.MERCHANT);
                    return;
                }
                break;
            case Profession.MERCHANT:
                if (region.economy.GetTotalNutrition() < GetRequiredNutrition() * 2 && !region.fieldsFull)
                {
                    // Merchant To Farmer
                    ChangeProfession((long)(workforce * 0.02), (long)(dependents * 0.02), Profession.FARMER);
                    return;
                }
                // Merchant To Soldier
                break;
        }
    }
    #endregion
    #endregion

    public void Migrate()
    {
        // Chance of pop to migrate
        float migrateChance = 0.0005f;

        if (simManager.complexEconomy)
        {
            // Complex Migration
            bool farmingFactor = FromNativePopulation(population) >= region.economy.GetTotalFoodAmount() * 1.2 || region.fieldsFull;
            // Pops are most likely to migrate if their region is overpopulated
            if (farmingFactor && FromNativePopulation(population) > 4000 * region.avgFertility || region.fieldsFull)
            {
                migrateChance = 1f;
            }
        }
        else
        {
            // Simple Migration
            if (region.population >= region.maxPopulation * 0.95f)
            {
                migrateChance = 1f;
            }
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

            if (target.Migrateable(this) && canMigrate)
            {
                region.MovePop(this, target, (long)(workforce * Mathf.Lerp(0.05, 0.5, rng.NextDouble())), (long)(dependents * Mathf.Lerp(0.05, 0.5, rng.NextDouble())));
            }
        }
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
