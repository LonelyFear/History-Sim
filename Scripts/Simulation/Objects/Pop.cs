using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Claims;
using Godot;
using MessagePack;
[MessagePackObject]
public class Pop
{
    [Key(0)] public ulong id;
    [Key(2)] public int population { get; set; } = 0;
    [Key(3)] public int workforce { get; set; } = 0;
    [Key(4)] public int dependents { get; set; } = 0;

    [Key(5)] public float baseBirthRate { get; set; } = 0.3f;
    [Key(6)] public float baseDeathRate { get; set; } = 0.29f;

    [Key(7)] public float targetDependencyRatio { get; set; } = 0.6f;
    [Key(8)] public float netIncome { get; set; } = 0f;
    [Key(9)] public double happiness { get; set; } = 1;
    [Key(10)] public double loyalty { get; set; } = 1;
    [Key(11)] public double politicalPower { get; set; } = 1;
    
    [IgnoreMember] public Region region;
    [Key(12)] public ulong regionId { get; set; }
    [IgnoreMember] public Culture culture { get; set; }
    [Key(13)] public ulong cultureId { get; set; }
    [Key(14)] public SocialClass profession { get; set; } = SocialClass.FARMER;

    [Key(15)] public Tech tech { get; set; } = new Tech();
    [Key(16)] public uint batchId { get; set; } = 1;

    [Key(17)] public List<Character> characters { get; set; } = new List<Character>();
    //[IgnoreMember] public static SimManager simManager;
    [IgnoreMember] public static ObjectManager objectManager;
    [IgnoreMember] public static Random rng = new Random();
    [Key(18)] public float wealth { get; set; } = 0f;
    [Key(19)] public int ownedLand { get; set; } = 0;
    [Key(20)] public bool shipborne { get; set; } = false;
    [Key(21)] public Direction lastDirection = Direction.RIGHT;

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
    public void ChangePopulation(int wfChange, int dfChange)
    {
        wfChange = Math.Max(wfChange, -workforce);
        dfChange = Math.Max(dfChange, -dependents);

        workforce += wfChange;
        dependents += dfChange;
        population += wfChange + dfChange;    

        culture.ChangePopulation(wfChange, dfChange, profession, culture);
        region.ChangePopulation(wfChange, dfChange, profession, culture);
    }
    public static bool CanPopsMerge(Pop a, Pop b)
    {
        if (a == null || b == null || a == b)
        {
            return false;
        }
        return a != b && a.profession == b.profession && Culture.CheckCultureSimilarity(a.culture, b.culture);
    }
    public Pop ChangeSocialClass(int workforceDelta, int dependentsDelta, SocialClass newSocialClass)
    {
        // Makes sure the profession is actually changing
        // And that we arent just creating an empty pop
        if (newSocialClass == profession || (workforceDelta < 1 && dependentsDelta < 1))
        {
            return null;
        }
        // Clamping
        workforceDelta = Math.Clamp(workforceDelta, 0, workforce);
        dependentsDelta = Math.Clamp(dependentsDelta, 0, dependents);

        // If we are changing the whole pop just change the profession
        if (workforceDelta == workforce && dependentsDelta == dependents)
        {
            profession = newSocialClass;
            return this;
        }
        // Makes a new pop with the new profession
        Pop newWorkers = objectManager.CreatePop(workforceDelta, dependentsDelta, region, tech, culture, newSocialClass);
        // And removes the people who switched to the new profession
        ChangePopulation(-workforceDelta, -dependentsDelta);
        // Land Stuff
        return newWorkers;
    }

    public void TechnologyUpdate()
    {
        double militaryTechChance = 0.002;
        double societyTechChance = 0.002;
        double industryTechChance = 0.01;
        Tech t = tech;
        if (rng.NextDouble() < militaryTechChance)
        {
            t.militaryLevel++;
        }
        if (rng.NextDouble() < societyTechChance)
        {
            t.societyLevel++;
        }
        if (tech.societyLevel >= 20 && tech.militaryLevel >= 20 && rng.NextDouble() < industryTechChance)
        {
            t.industryLevel++;
        }
        tech = t;
    }
    /*
    public void SocialClassTransitions()
    {
        // TODO: Social Class Transitions
        float percentageOfRegionWorkforce = region.workforce / (float)workforce;
        long requiredWorkersInField = 0;
        try
        {
           requiredWorkersInField = region.settlement.requiredWorkers[profession];
        } catch (Exception) {}
        
        switch (profession)
        {
            case SocialClass.ARISTOCRAT:
                if (requiredWorkersInField < 0)
                {
                    ChangeSocialClass((long)Mathf.Abs(requiredWorkersInField * percentageOfRegionWorkforce), 0, SocialClass.FARMER);
                }
                break;
            case SocialClass.MERCHANT:
                if (requiredWorkersInField < 0)
                {
                    ChangeSocialClass((long)Mathf.Abs(requiredWorkersInField * percentageOfRegionWorkforce), 0, SocialClass.FARMER);
                }
                break;
            case SocialClass.FARMER:
                if (requiredWorkersInField > 0) break;

                // If there are extra farmers
                foreach (var classPair in region.settlement.requiredWorkers)
                {
                    SocialClass socialClass = classPair.Key;
                    long requiredWorkers = classPair.Value;
                    if (requiredWorkers < 1)
                    {
                        continue;
                    }
                    // If there is an opening
                    long workersTransferred = (long)(Math.Clamp(requiredWorkers, 0, Math.Abs(requiredWorkersInField)) * 0.1);
                    ChangeSocialClass(workersTransferred, 0, socialClass);
                }
                break;
        }
    }
    */
    public double CalculatePoliticalPower()
    {
        double popSizePoliticalPower = workforce * 0.0005;
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
        lock (region)
        {
            if (region.population >= region.maxPopulation || shipborne)
            {
                migrateChance = 1f;
            }            
        }

        if (profession == SocialClass.ARISTOCRAT && !shipborne)
        {
            migrateChance *= 0.1f;
        }
        // If the pop migrates
        if (rng.NextSingle() > migrateChance) return;

        Region target = target = region.PickRandomBorder();

        bool professionAllows = true;

        // If the profession allows migration
        if (!shipborne)
        {
            switch (profession)
            {
                case SocialClass.ARISTOCRAT:
                    if (target.owner != region.owner)
                    {
                        professionAllows = false;
                    }
                    break;
            }
            if (!professionAllows) return;            
        }

        float chanceToMoveOnTile = target.isWater ? 0.1f : target.navigability;
        if (shipborne)
        {
            chanceToMoveOnTile = 1f;
        }   

        lock (target)
        {
            if (target.population > target.maxPopulation)
            {
                chanceToMoveOnTile *= 0.1f;
            }

            if (!target.Migrateable(this))
            {
                chanceToMoveOnTile *= 0;
            }
        }

        if (rng.NextSingle() < chanceToMoveOnTile)
        {
            if (shipborne)
            {
                MovePop(target, workforce, dependents);
                return;
            }

            float movedPercentage = 0;
            lock (region)
            {
                movedPercentage = (region.population - region.maxPopulation) / (float)population;
            }
            
            int movedDependents = (int)(dependents * movedPercentage);
            int movedWorkforce = (int)(workforce * movedPercentage);

            MovePop(target, movedWorkforce, movedDependents);
        }
    }
    public void MovePop(Region destination, int movedWorkforce, int movedDependents)
    {
        if (destination == null || destination == region)
        {
            return;
        }
        movedWorkforce = Math.Clamp(movedWorkforce, 0, workforce);
        movedDependents = Math.Clamp(movedDependents, 0, dependents);

        lock (objectManager)
        {
            Pop newPop = objectManager.CreatePop(movedWorkforce, movedDependents, destination, tech, culture, profession);
            newPop.lastDirection = lastDirection;
        }
        ChangePopulation(-movedWorkforce, -movedDependents);     
    }
    public float GetDeathRate()
    {
        float deathRate = baseDeathRate;
        if (shipborne)
        {
            deathRate *= 3f;
        }
        return deathRate;
    }
    public float GetBirthRate()
    {
        float birthRate = baseBirthRate;
        lock (region)
        {
            if (region.population < region.maxPopulation * 0.5f)
            {
                birthRate *= 1.5f;
            }            
        }

        return birthRate;
    }
    public void GrowPop()
    {

        float bRate;
        if (population < 2)
        {
            bRate = 0;
        }
        {
            bRate = GetBirthRate();
        }
        if (!shipborne)
        {
            lock (region)
            {
                if (region.population > region.maxPopulation)
                {
                    bRate *= region.maxPopulation/(float)region.population;
                }            
            }            
        }
        float NIR = bRate - GetDeathRate();

        int change = Mathf.RoundToInt((workforce + dependents) * NIR);
        int dependentChange = (int)(change * targetDependencyRatio);
        int workforceChange = change - dependentChange;   

        // Chance of an extra person
        if (rng.NextSingle() < ((workforce + dependents) * NIR) - (int)((workforce + dependents) * NIR))
        {
            if (rng.NextSingle() < targetDependencyRatio)
            {
                dependentChange++;
            } else
            {
                workforceChange++;
            }
        }

        ChangePopulation(workforceChange, dependentChange);
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
