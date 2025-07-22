using System;
using System.Collections.Generic;
using System.Security.Claims;
using Godot;

public class Pop
{
    public long maxPopulation = 0;
    public long population = 0;
    public long workforce = 0;
    public long dependents = 0;

    public float baseBirthRate = 0.30f;
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
    public int ownedLand = 0;
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
    public Pop ChangeProfession(long workforceDelta, long dependentDelta, Profession newProfession, int landMoved)
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
        // Land Stuff

        ClaimLand(-landMoved);
        newWorkers.ClaimLand(landMoved);
        
        return newWorkers;
    }

    #region Economy
    public void EconomyUpdate()
    {
        if (population > maxPopulation && region.freeLand > 0)
        {
            ClaimLand(1);
        }
        if (population < maxPopulation / 2 && ownedLand > 1)
        {
            ClaimLand(-1);
        }
        CalcMaxPopulation();
    }

    public void ProfessionTransitions()
    {
        bool regionHasAristocrats = region.professions[Profession.ARISTOCRAT] > 1000;
        bool takeableAristocracy = !regionHasAristocrats && (region.owner == null || (region.owner != null && region.owner.capital == region));

        long regionProductiveWorkforce = region.workforce - region.professions[Profession.ARISTOCRAT];
        long farmersRequiredOfPop = (long)((region.maxFarmers - region.professions[Profession.FARMER]) * (workforce/(float)regionProductiveWorkforce));
        if (profession == Profession.ARISTOCRAT)
        {
            farmersRequiredOfPop = 0;
        }

        // Military Transitions
        long soldieringWorkforce = region.workforce - region.professions[Profession.ARISTOCRAT] - region.professions[Profession.MERCHANT];
        long soldiersRequiredOfPop = (long)((region.maxSoldiers - region.professions[Profession.SOLDIER]) * (workforce/(float)soldieringWorkforce));
        if (region.owner == null)
        {
            soldiersRequiredOfPop = -workforce;
        }

        switch (profession)
        {
            case Profession.FARMER:
                long excessPopulation = -farmersRequiredOfPop;
                // Converts farmers to more advanced professions
                if (farmersRequiredOfPop < -ToNativePopulation(100))
                {
                    // To Merchant
                    float changedPercent = excessPopulation / ((float)workforce);
                    ChangeProfession((long)(workforce * changedPercent), (long)(dependents * changedPercent), Profession.MERCHANT, Mathf.Clamp((int)(ownedLand * changedPercent), 1, 64));
                    break;
                }
                if (soldiersRequiredOfPop > ToNativePopulation(100))
                {
                    float changedPercent = soldiersRequiredOfPop / ((float)workforce);
                    ChangeProfession((long)(workforce * changedPercent), (long)(dependents * changedPercent), Profession.SOLDIER, Mathf.Clamp((int)(ownedLand * changedPercent), 1, 64));
                    break;
                }
                break;
            case Profession.MERCHANT:
                // To Farmer
                bool farmersNeeded = farmersRequiredOfPop > 0;
                if (farmersNeeded && rng.NextSingle() < 0.01f)
                {
                    //GD.Print("Merchants Became Farmers");
                    float changedPercent = farmersRequiredOfPop / ((float)workforce);
                    ChangeProfession((long)(workforce * changedPercent), (long)(dependents * changedPercent), Profession.FARMER, Mathf.Clamp((int)(ownedLand * changedPercent), 1, 64));
                    break;
                }

                if (takeableAristocracy && rng.NextSingle() < 0.005f && region.wealth >= 30f)
                {
                    float changedPercent = 0.2f;
                    ChangeProfession((long)(workforce * changedPercent), (long)(dependents * changedPercent), Profession.ARISTOCRAT, Mathf.Clamp((int)(ownedLand * changedPercent), 1, 64));
                    break;
                }
                break;
            case Profession.SOLDIER:
                excessPopulation = -soldiersRequiredOfPop;
                farmersNeeded = farmersRequiredOfPop > ToNativePopulation(100);
                // Soldiers go back to their fields

                // TODO: Add morale, make it fall with sustained casualties, unpopular leadership, defeats, etc
                // TODO: Make the chance of soldiers leaving rise as morale falls. 

                if (farmersNeeded && rng.NextSingle() < 0.001f)
                {
                    //GD.Print("Merchants Became Farmers");
                    float changedPercent = farmersRequiredOfPop * 0.5f / workforce;
                    ChangeProfession((long)(workforce * changedPercent), (long)(dependents * changedPercent), Profession.FARMER, Mathf.Clamp((int)(ownedLand * changedPercent), 1, 64));
                    break;
                }
                // Excess soldiers go back to tending their fields/doing other work
                if (soldiersRequiredOfPop < 0)
                {
                    float changedPercent = excessPopulation / ((float)workforce);
                    ChangeProfession((long)(workforce * changedPercent), (long)(dependents * changedPercent), Profession.FARMER, Mathf.Clamp((int)(ownedLand * changedPercent), 1, 64));
                }
                break;
        }
    }
    #endregion
    #region Demographics
    public void Migrate()
    {
        // Chance of pop to migrate
        float migrateChance = 0f;
        // Simple Migration
        if (population >= maxPopulation)
        {
            migrateChance = 0.7f;
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
            bool professionAllows = true;
            if (region.owner != null && region.owner.rulingPop == this)
            {
                canMigrate = region.owner == target.owner;
            }
            switch (profession)
            {
                case Profession.SOLDIER:
                    if (target.owner != region.owner)
                    {
                        professionAllows = false;
                    }
                    break;
                case Profession.ARISTOCRAT:
                    if (target.owner != region.owner)
                    {
                        professionAllows = false;
                    }
                    break;
            }

            if (target.Migrateable(this) && canMigrate && professionAllows && (rng.NextSingle() < target.navigability) && (rng.NextSingle() < target.arableLand / target.landCount))
            {
                long movedDependents = (long)(dependents * Mathf.Lerp(0.05, 0.5, rng.NextDouble()));
                long movedWorkforce = (long)(workforce * Mathf.Lerp(0.05, 0.5, rng.NextDouble()));

                region.MovePop(this, target, movedWorkforce, movedDependents);
            }
        }
    }
    public float GetDeathRate()
    {
        float deathRate = baseDeathRate;
        return deathRate;
    }
    public float GetBirthRate()
    {
        float birthRate = baseBirthRate;
        if (population < maxPopulation * 0.5f)
        {
            birthRate *= 1.5f;
        }
        return birthRate;
    }
    public void GrowPop()
    {

        float bRate;
        if (population < ToNativePopulation(2))
        {
            bRate = 0;
        }
        else
        {
            bRate = GetBirthRate();
        }
        if (population > maxPopulation)
        {
            bRate *= FromNativePopulation(maxPopulation)/(float)FromNativePopulation(population);
        }

        float NIR = (bRate - GetDeathRate()) / 12f;

        long change = Mathf.RoundToInt((workforce + dependents) * NIR);
        long dependentChange = Mathf.RoundToInt(change * targetDependencyRatio);
        long workforceChange = change - dependentChange;
        ChangeWorkforce(workforceChange);
        ChangeDependents(dependentChange);
    }
    public void CalcMaxPopulation()
    {
        float techFactor = 1 + (tech.societyLevel * 0.1f);
        maxPopulation = ToNativePopulation((long)(1000f * techFactor * (1 + ownedLand) * (region.arableLand/region.landCount)));
    }    
    #endregion
    public void ClaimLand(int amount)
    {
        int fixedAmount;
        if (amount >= 0)
        {
            fixedAmount = Mathf.Clamp(amount, 0, region.freeLand);
            ownedLand += fixedAmount;

            SimManager.m.WaitOne();
            region.freeLand -= fixedAmount;
            SimManager.m.ReleaseMutex();
        }
        else
        {
            fixedAmount = Mathf.Clamp(Mathf.Abs(amount), 0, ownedLand);
            ownedLand -= fixedAmount;

            SimManager.m.WaitOne();
            region.freeLand += fixedAmount;
            SimManager.m.ReleaseMutex();

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
