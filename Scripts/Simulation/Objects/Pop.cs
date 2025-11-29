using System;
using System.Collections.Generic;
using System.Security.Claims;
using Godot;
using MessagePack;
[MessagePackObject]
public class Pop
{
    [Key(0)] public ulong id;
    [Key(1)] public long maxPopulation { get; set; } = 0;
    [Key(2)] public long population { get; set; } = 0;
    [Key(3)] public long workforce { get; set; } = 0;
    [Key(4)] public long dependents { get; set; } = 0;

    [Key(5)] public float baseBirthRate { get; set; } = 0.3f;
    [Key(6)] public float baseDeathRate { get; set; } = 0.29f;

    [Key(7)] public float targetDependencyRatio { get; set; } = 0.75f;
    [Key(8)] public float netIncome { get; set; } = 0f;
    [Key(9)] public double happiness { get; set; } = 1;
    [Key(10)] public double loyalty { get; set; } = 1;
    [Key(11)] public double politicalPower { get; set; } = 1;
    
    [IgnoreMember] public Region region;
    [Key(12)] public ulong regionId { get; set; }
    [IgnoreMember] public Culture culture { get; set; }
    [Key(13)] public ulong cultureId { get; set; }
    [Key(14)] public SocialClass profession { get; set; } = SocialClass.FARMER;

    [Key(15)] public Tech Tech { get; set; }
    [Key(16)] public uint batchId { get; set; } = 1;

    [Key(17)] public List<Character> characters { get; set; } = new List<Character>();
    //[IgnoreMember] public static SimManager simManager;
    [IgnoreMember] public static ObjectManager objectManager;
    [IgnoreMember] public static Random rng = new Random();
    [Key(18)] public float wealth { get; set; } = 0f;
    [Key(19)] public int ownedLand { get; set; } = 0;

    public void PrepareForSave()
    {
        regionId = region.id;
        cultureId = culture.id;
    }
    public void LoadFromSave()
    {
        //GD.Print(regionId);
        //GD.Print(simManager.GetRegion(regionId));
        region = objectManager.GetRegion(regionId);
        culture = objectManager.GetCulture(cultureId);
    }
    public void ChangeWorkforce(long amount)
    {
        if (workforce + amount < 0)
        {
            amount = -workforce;
        }
        workforce += amount;
        population += amount;
        culture.ChangePopulation(amount, 0, profession, culture);
        region.ChangePopulation(amount, 0, profession, culture);
    }
    public void ChangeDependents(long amount)
    {
        if (dependents + amount < 0)
        {
            amount = -dependents;
        }
        dependents += amount;
        population += amount;
        culture.ChangePopulation(0, amount, profession, culture);
        region.ChangePopulation(0, amount, profession, culture);
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
        Pop newWorkers = objectManager.CreatePop(workforceDelta, dependentDelta, region, Tech, culture, newSocialClass);
        // And removes the people who switched to the new profession
        ChangePopulation(-workforceDelta, -dependentDelta);
        // Land Stuff

        ClaimLand(-landMoved);
        newWorkers.ClaimLand(landMoved);
        return newWorkers;
    }
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
        double militaryTechChance = 0.002;
        double societyTechChance = 0.002;
        double industryTechChance = 0.01;
        Tech t = Tech;
        if (rng.NextDouble() < militaryTechChance)
        {
            t.militaryLevel++;
        }
        if (rng.NextDouble() < societyTechChance)
        {
            t.societyLevel++;
        }
        if (Tech.societyLevel >= 20 && Tech.militaryLevel >= 20 && rng.NextDouble() < industryTechChance)
        {
            t.industryLevel++;
        }
        Tech = t;
    }
    public void SocialClassTransitions()
    {
        // TODO: Social Class Transitions
    }
    public double CalculatePoliticalPower()
    {
        double popSizePoliticalPower = FromNativePopulation(workforce) * 0.0005;
        double basePoliticalPower = 0;
        switch (profession)
        {
            case SocialClass.FARMER:
                basePoliticalPower = 0.5;
                break;
            case SocialClass.SOLDIER:
                basePoliticalPower = 1;
                break;
            case SocialClass.LABOURER:
                basePoliticalPower = 1;
                break;
            case SocialClass.MERCHANT:
                basePoliticalPower = 2;
                break;
            case SocialClass.ARISTOCRAT:
                basePoliticalPower = 4;
                break;
        }
        return basePoliticalPower * popSizePoliticalPower;
    }
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
            Pop npop = objectManager.CreatePop(movedWorkforce, movedDependents, destination, Tech, culture, profession);
            ChangePopulation(-movedWorkforce, -movedDependents);     
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
        double techFactor = 1 + (Tech.societyLevel * 0.5);
        double wealthFactor = wealth * 10;
        return ToNativePopulation((long)((Region.populationPerLand + wealthFactor) * techFactor * ownedLand * (region.arableLand / region.landCount)));
    }    
    
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
    FARMER,
    SOLDIER,
    LABOURER,
    MERCHANT,
    ARISTOCRAT
}
