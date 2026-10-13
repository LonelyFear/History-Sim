using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;
[MessagePackObject(AllowPrivate = true)]
public partial class Pop
{
    [Key(1)] public ulong id;
    [Key(2)] public int population { get; set; } = 0;
    [Key(3)] public int workforce { get; set; } = 0;
    [Key(4)] public int dependents { get; set; } = 0;

    [Key(5)] public float baseBirthRate { get; set; } = 0.31f;
    [Key(6)] public float baseDeathRate { get; set; } = 0.29f;

    [Key(7)] public float targetDependencyRatio { get; set; } = 0.6f;
    [Key(8)] public float netIncome { get; set; } = 0f;
    [Key(9)] public double happiness { get; set; } = 1;
    [Key(10)] public double loyalty { get; set; } = 1;
    [Key(11)] public double politicalPower { get; set; } = 1;
    
    [Key(12)] public ulong? regionId { get; set; }

    [Key(13)] public ulong? cultureId { get; set; }
    //[Key(14)] public SocialClass socialClass { get; set; } = SocialClass.FARMER;
    
    [Key(15)] public Tech tech = new();
    [Key(16)] public uint batchId { get; set; } = 1;

    //[IgnoreMember] public static SimManager simManager;
    [IgnoreMember] public static Random rng = null;
    [Key(18)] public float wealth { get; set; } = 0f;
    [Key(19)] public int ownedLand { get; set; } = 0;
    [Key(22)] string socialClassId = "farmer";
    [IgnoreMember] public Dictionary<string, float> goodsDemands = [];
    // Reference Types
    [IgnoreMember] SocialClass _socialClass;
    [IgnoreMember] public SocialClass socialClass
    {
        get
        {
            _socialClass ??= AssetManager.GetSocialClass(socialClassId);
            return _socialClass;
        } set
        {
            if (value == null)
            {
                return;
            }
            socialClassId = value.id;
            _socialClass = value;
        }
    }
    [IgnoreMember] Culture _culture;
    [IgnoreMember] public Culture culture { 
        get
        {
            _culture ??= ObjectManager.GetCulture(cultureId);
            return _culture;
        }
        set
        {
            cultureId = value?.id;
            _culture = value;
        }
    }
    [IgnoreMember] Region _region;
    [IgnoreMember] public Region region { 
        get
        {
            _region ??= ObjectManager.GetRegion(regionId);
            return _region;
        }
        set
        {
            regionId = value?.id;
            _region = value;
        }
    }

    [IgnoreMember] public const float maxGoodMarketShare = 0.9f;

    public void ChangePopulation(int wfChange, int dfChange)
    {
        wfChange = Math.Max(wfChange, -workforce);
        dfChange = Math.Max(dfChange, -dependents);

        workforce += wfChange;
        dependents += dfChange;
        population += wfChange + dfChange;    

        culture.ChangePopulation(wfChange, dfChange, socialClass.id, culture);
        region.ChangePopulation(wfChange, dfChange, socialClass.id, culture);
    }
    public static bool CanPopsMerge(Pop a, Pop b)
    {
        if (a == null || b == null || a == b)
        {
            return false;
        }
        return a != b && a.socialClass == b.socialClass && Culture.CheckCultureSimilarity(a.culture, b.culture);
    }
    public Pop ChangeSocialClass(int convertedWorkforce, int convertedDependents, SocialClass newSocialClass)
    {
        // Makes sure the socialClass is actually changing
        // And that we arent just creating an empty pop
        if (newSocialClass == socialClass || (convertedWorkforce < 1 && convertedDependents < 1))
        {
            return null;
        }
        // Clamping
        convertedWorkforce = Math.Clamp(convertedWorkforce, 0, workforce);
        convertedDependents = Math.Clamp(convertedDependents, 0, dependents);

        // If we are changing the whole pop just change the socialClass
        if (convertedWorkforce == workforce && convertedDependents == dependents)
        {
            socialClass = newSocialClass;
            return this;
        }
        // Makes a new pop with the new socialClass
        Pop newWorkers = ObjectManager.CreatePop(convertedWorkforce, convertedDependents, region, tech, culture, newSocialClass.id);
        // And removes the people who switched to the new socialClass
        ChangePopulation(-convertedWorkforce, -convertedDependents);
        // Land Stuff
        return newWorkers;
    }

    public void TechnologyUpdate()
    {      
        float militaryTechChance = Defines.BaseRedTechSeedChance * region.fertility;
        float societyTechChance = Defines.BaseBlueTechSeedChance * region.fertility + (tech.militaryLevel * Defines.BlueTechBonus);
        float industryTechChance = Defines.BaseIndTechSeedChance * region.fertility;

        // Tech Seeding
        
        // Military Advancement
        if (rng.NextSingle() < militaryTechChance && region.owner?.capital == region && region.owner.enemies.Count > 0)
        {
            tech.militaryLevel = Mathf.Clamp(tech.militaryLevel + 1, 0, 20);                
        }
        // Society Advancement
        if (rng.NextSingle() < societyTechChance && tech.societyLevel < Mathf.Min(3 + tech.militaryLevel, 20))
        {
            tech.societyLevel = Mathf.Clamp(tech.societyLevel + 1, 0, 20);
        }
        // Industrial Advancement
        if (tech.societyLevel >= 20 && tech.militaryLevel >= 20 && rng.NextSingle() < industryTechChance)
        {
            tech.industryLevel = Mathf.Clamp(tech.industryLevel + 1, 0, 20);
        }

        // Tech Spread
        
        foreach (Pop linkedPop in region.GetLinkedPops())
        {
            if (linkedPop == this || linkedPop == null) continue;

            if (linkedPop.region == null)
            {
                //GD.Print("The pop has no region?");
                continue;
            } 

            float indTechSpreadChance = Defines.BaseIndTechSpreadChance * linkedPop.region.navigability;
            float redTechSpreadChance = Defines.BaseRedTechSpreadChance * linkedPop.region.navigability;
            if (linkedPop.culture != culture)
            {
                redTechSpreadChance *= 0.5f;
                indTechSpreadChance *= 0.25f;
            } 
            if (region.owner != null)
            {
                if (linkedPop.region.owner != region.owner)
                {
                    redTechSpreadChance *= 0.5f;
                    indTechSpreadChance *= 0.5f;
                } 
                if (linkedPop.region.owner == region.owner) indTechSpreadChance *= 2f;
                
                if (region.owner.enemies.Contains(linkedPop.region.owner))
                {
                    redTechSpreadChance *= 0.5f;
                    indTechSpreadChance *= 0.5f;
                } 
            }

            // Innovation Spread
            if (rng.NextSingle() < redTechSpreadChance)
            {
                linkedPop.tech.militaryLevel = Mathf.Max(tech.militaryLevel, linkedPop.tech.militaryLevel);
            }

            // Industry Spread
            if (rng.NextSingle() < indTechSpreadChance && linkedPop.tech.societyLevel >= 20 && linkedPop.tech.militaryLevel >= 20)
            {
                linkedPop.tech.industryLevel = Mathf.Max(tech.industryLevel, linkedPop.tech.industryLevel);
            }    

            // Blue Tech Transfer
            if (rng.NextSingle() < Defines.BlueTechSpreadChance)
            {
                float culturalPower = 1f + (population * 0.01f * tech.societyLevel);
                float culturalDefense = 1f + (linkedPop.population * 0.01f * linkedPop.tech.societyLevel)/linkedPop.region.navigability;
                if (culture != linkedPop.culture) culturalDefense *= 1.2f;
                
                float succesfulTransferChance = (culturalPower - culturalDefense)/(culturalPower + culturalDefense);

                if (rng.NextSingle() < succesfulTransferChance)
                {
                    linkedPop.tech.societyLevel = tech.societyLevel;
                }                
            }

        }
    }
    public float CalculatePoliticalPower()
    {
        float popSizePoliticalPower = workforce * 0.005f;
        return socialClass.basePoliticalPower * popSizePoliticalPower;
    }
    public void Migrate()
    {
        // Chance of pop to migrate
        float migrateChance = 0f;
        // Simple Migration
        lock (region)
        {
            if (region.population >= region.maxPopulation)
            {
                //GD.Print("Migrate");
                migrateChance = 1f;
            }            
        }

        if (socialClass.id == "aristocrat")
        {
            migrateChance *= 0.1f;
        }
        // If the pop migrates
        if (rng.NextSingle() > migrateChance) return;

        Region target = region.PickRandomBorder();

        bool socialClassAllows = true;

        // If the socialClass allows migration
        switch (socialClass.id)
        {
            case "aristocrat":
                if (target.owner != region.owner)
                {
                    socialClassAllows = false;
                }
                break;
        }
        if (!socialClassAllows) return;            

        float chanceToMoveOnTile = target.navigability;

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

        ObjectManager.CreatePop(movedWorkforce, movedDependents, destination, tech, culture, socialClass.id);
        ChangePopulation(-movedWorkforce, -movedDependents);     
    }
    public float GetDeathRate()
    {
        float deathRate = baseDeathRate;
        return deathRate;
    }
    public float GetBirthRate()
    {
        float birthRate = baseBirthRate;
        if (population <= 2)
        {
            return 0;
        }
        lock (region)
        {
            if (region.population < region.maxPopulation * 0.5f)
            {
                //birthRate *= 1.5f;
            }            
        }

        return birthRate;
    }
    public void GrowPop()
    {

        float bRate = GetBirthRate();
        lock (region)
        {
            if (region.population > region.maxPopulation)
            {
                bRate *= region.maxPopulation/(float)region.population;
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
        //GD.Print(workforceChange + dependentChange);
        ChangePopulation(workforceChange, dependentChange);
    }
}
public enum Direction{
    UP = 0,
	RIGHT = 1,
	DOWN = 2,
	LEFT = 3
}
