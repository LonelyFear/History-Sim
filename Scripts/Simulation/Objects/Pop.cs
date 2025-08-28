using System;
using System.Collections.Generic;
using System.Security.Claims;
using Godot;
using MessagePack;
[MessagePackObject(keyAsPropertyName: true)]
public class Pop
{
    public ulong id;
    public long maxPopulation { get; set; } = 0;
    public long population { get; set; } = 0;
    public long workforce { get; set; } = 0;
    public long dependents { get; set; } = 0;

    public float baseBirthRate { get; set; } = 0.3f;
    public float baseDeathRate { get; set; } = 0.29f;

    public float targetDependencyRatio { get; set; } = 0.75f;
    public float netIncome { get; set; } = 0f;
    public double happiness { get; set; } = 1;
    public double loyalty { get; set; } = 1;
    public double politicalPower { get; set; } = 1;
    [IgnoreMember]
    public Region region { get; set; }
    public ulong regionID;
    [IgnoreMember] public Culture culture { get; set; }
    public ulong cultureId { get; set; }
    public SocialClass profession { get; set; } = SocialClass.FARMER;

    public Tech tech { get; set; } = new Tech();
    public uint batchId { get; set; } = 1;

    public List<Character> characters { get; set; } = new List<Character>();
    [IgnoreMember]
    public static SimManager simManager;
    [IgnoreMember]
    public static Random rng = new Random();
    public float wealth { get; set; } = 0f;
    public int ownedLand { get; set; } = 0;

    public void PrepareForSave()
    {
        regionID = region.id;
        cultureId = culture.id;
    }
    public void LoadFromSave()
    {
        region = simManager.regionsIds[regionID];
        culture = simManager.culturesIds[cultureId];
    }
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
    public Pop ChangeSocialClass(long workforceDelta, long dependentDelta, SocialClass newSocialClass, int landMoved)
    {
        // Makes sure the profession is actually changing
        // And that we arent just creating an empty pop
        if (newSocialClass == profession && (workforceDelta >= ToNativePopulation(1) || dependentDelta >= ToNativePopulation(1)))
        {
            return null;
        }
        // Clamping
        workforceDelta = Math.Clamp(workforceDelta, 0, workforce);
        dependentDelta = Math.Clamp(dependentDelta, 0, dependents);

        // If we are changing the whole pop just change the profession
        if (workforceDelta == workforce && dependentDelta == dependents)
        {
            profession = newSocialClass;
            return this;
        }
        // Makes a new pop with the new profession
        SimManager.m.WaitOne();
        Pop newWorkers = simManager.CreatePop(workforceDelta, dependentDelta, region, tech, culture, newSocialClass);
        // And removes the people who switched to the new profession
        ChangePopulation(-workforceDelta, -dependentDelta);
        // Land Stuff

        ClaimLand(-landMoved);
        newWorkers.ClaimLand(landMoved);
        SimManager.m.ReleaseMutex();
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
        maxPopulation = GetMaxPopulation();
    }

    public void TechnologyUpdate()
    {
        double militaryTechChance = 0.0001;
        double societyTechChance = 0.0001;

        if (rng.NextDouble() < militaryTechChance)
        {
            tech.militaryLevel++;
        }
        if (rng.NextDouble() < societyTechChance)
        {
            tech.societyLevel++;
        }       
    }
    public void SocialClassTransitions()
    {
        bool regionHasAristocrats = region.professions[SocialClass.ARISTOCRAT] > 1000;
        bool takeableAristocracy = !regionHasAristocrats && (region.owner == null || (region.owner != null && region.owner.capital == region));

        long regionProductiveWorkforce = region.workforce - region.professions[SocialClass.ARISTOCRAT];
        long farmersRequiredOfPop = (long)((region.maxFarmers - region.professions[SocialClass.FARMER]) * (workforce / (float)regionProductiveWorkforce));
        if (profession == SocialClass.ARISTOCRAT)
        {
            farmersRequiredOfPop = 0;
        }

        // Military Transitions
        long soldieringWorkforce = region.workforce - region.professions[SocialClass.ARISTOCRAT] - region.professions[SocialClass.MERCHANT];
        long soldiersRequiredOfPop = (long)((region.maxSoldiers - region.professions[SocialClass.SOLDIER]) * (workforce / (float)soldieringWorkforce));

        switch (profession)
        {
            case SocialClass.FARMER:
                long excessPopulation = -farmersRequiredOfPop;
                // Converts farmers to more advanced professions
                if (farmersRequiredOfPop < -ToNativePopulation(100))
                {
                    // To Merchant
                    float changedPercent = excessPopulation / ((float)workforce);
                    ChangeSocialClass((long)(workforce * changedPercent), (long)(dependents * changedPercent), SocialClass.MERCHANT, Mathf.Clamp((int)(ownedLand * changedPercent), 1, 64));
                    break;
                }
                if (soldiersRequiredOfPop > ToNativePopulation(100))
                {
                    float changedPercent = soldiersRequiredOfPop / ((float)workforce);
                    ChangeSocialClass((long)(workforce * changedPercent), (long)(dependents * changedPercent), SocialClass.SOLDIER, Mathf.Clamp((int)(ownedLand * changedPercent), 1, 64));
                    break;
                }
                break;
            case SocialClass.MERCHANT:
                // To Farmer
                bool farmersNeeded = farmersRequiredOfPop > 0;
                if (farmersNeeded && rng.NextSingle() < 0.005f)
                {
                    //GD.Print("Merchants Became Farmers");
                    float changedPercent = farmersRequiredOfPop / ((float)workforce);
                    ChangeSocialClass((long)(workforce * changedPercent), (long)(dependents * changedPercent), SocialClass.FARMER, Mathf.Clamp((int)(ownedLand * changedPercent), 1, 64));
                    break;
                }
                float bestAristocracyWealth = 40f;
                float clampedWealth = Mathf.Clamp(region.wealth, 0, bestAristocracyWealth) / bestAristocracyWealth;
                if (takeableAristocracy && rng.NextSingle() < 0.01f * clampedWealth)
                {
                    float changedPercent = 0.2f;
                    ChangeSocialClass((long)(workforce * changedPercent), (long)(dependents * changedPercent), SocialClass.ARISTOCRAT, Mathf.Clamp((int)(ownedLand * changedPercent), 1, 64));
                    break;
                }
                break;
            case SocialClass.SOLDIER:
                excessPopulation = -soldiersRequiredOfPop;
                farmersNeeded = farmersRequiredOfPop > ToNativePopulation(100);
                // Soldiers go back to their fields

                // TODO: Add morale, make it fall with sustained casualties, unpopular leadership, defeats, etc
                // TODO: Make the chance of soldiers leaving rise as morale falls. 

                if (farmersNeeded && rng.NextSingle() < 0.001f)
                {
                    //GD.Print("Merchants Became Farmers");
                    float changedPercent = farmersRequiredOfPop * 0.5f / workforce;
                    ChangeSocialClass((long)(workforce * changedPercent), (long)(dependents * changedPercent), SocialClass.FARMER, Mathf.Clamp((int)(ownedLand * changedPercent), 1, 64));
                    break;
                }
                // Excess soldiers go back to tending their fields/doing other work
                if (soldiersRequiredOfPop < 0)
                {
                    float changedPercent = excessPopulation / ((float)workforce);
                    ChangeSocialClass((long)(workforce * changedPercent), (long)(dependents * changedPercent), SocialClass.FARMER, Mathf.Clamp((int)(ownedLand * changedPercent), 1, 64));
                }
                break;
        }
    }
    #endregion
    #region Nations
    public void CalculatePoliticalPower()
    {
        switch (profession)
        {
            
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
            migrateChance = 1f;
        }
        if (profession == SocialClass.ARISTOCRAT)
        {
            migrateChance *= 0.1f;
        }
        // If the pop migrates
        if (rng.NextSingle() <= migrateChance)
        {
            lock (region)
            {
                Region target = region.PickRandomBorder();
                bool professionAllows = true;
                switch (profession)
                {
                    case SocialClass.SOLDIER:
                        if (target.owner != region.owner)
                        {
                            professionAllows = false;
                        }
                        break;
                    case SocialClass.ARISTOCRAT:
                        if (target.owner != region.owner)
                        {
                            professionAllows = false;
                        }
                        break;
                }
                lock (target)
                {
                    if (target.Migrateable(this) && professionAllows && (rng.NextSingle() < target.navigability))
                    {
                        float movedPercentage = (population - maxPopulation) / (float)population;
                        long movedDependents = (long)(dependents * movedPercentage);
                        long movedWorkforce = (long)(workforce * movedPercentage);

                        MovePop(target, movedWorkforce, movedDependents);
                    }
                }

            }
        }
    }
    public void MovePop(Region destination, long movedWorkforce, long movedDependents)
    {
        if (destination == null || destination == region)
        {
            return;
        }
        if (movedWorkforce >= ToNativePopulation(1) || movedDependents >= ToNativePopulation(1))
        {
            if (movedWorkforce > workforce)
            {
                movedWorkforce = workforce;
            }
            if (movedDependents > dependents)
            {
                movedDependents = dependents;
            }
            lock (simManager)
            {
                Pop npop = simManager.CreatePop(movedWorkforce, movedDependents, destination, tech, culture, profession);
                ChangePopulation(-movedWorkforce, -movedDependents);     
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
    public long GetMaxPopulation()
    {
        float techFactor = 1 + (tech.societyLevel * 0.1f);
        float wealthFactor = wealth * 1;
        return ToNativePopulation((long)((Region.populationPerLand + wealthFactor) * techFactor * ownedLand * (region.arableLand / region.landCount)));
    }    
    #endregion
    public void ClaimLand(int amount)
    {
        lock (region) {
            int fixedAmount;
            if (amount >= 0)
            {
                fixedAmount = Mathf.Clamp(amount, 0, region.freeLand);
                ownedLand += fixedAmount;

                region.freeLand -= fixedAmount;
            }
            else
            {
                fixedAmount = Mathf.Clamp(Mathf.Abs(amount), 0, ownedLand);
                ownedLand -= fixedAmount;

                region.freeLand += fixedAmount;
            }            
        }
    }
}


public enum SocialClass
{
    POP,
    FARMER,
    SOLDIER,
    ARTISAN,
    MERCHANT,
    ARISTOCRAT
}
