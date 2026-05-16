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
    [IgnoreMember] public static ObjectManager objectManager;
    [IgnoreMember] public static Random rng = new Random();
    [Key(18)] public float wealth { get; set; } = 0f;
    [Key(19)] public int ownedLand { get; set; } = 0;
    [Key(20)] public bool shipborne { get; set; } = false;
    [Key(21)] public Direction lastDirection = Direction.RIGHT;
    [Key(22)] string professionId = "farmer";
    [IgnoreMember] public Dictionary<string, float> goodsDemands = [];
    // Reference Types
    [IgnoreMember] Profession _profession;
    [IgnoreMember] public Profession profession
    {
        get
        {
            if (_profession == null)
            {
                _profession = AssetManager.GetProfession(professionId);
            }
            return _profession;
        } set
        {
            if (value == null)
            {
                return;
            }
            professionId = value.id;
            _profession = value;
        }
    }
    [IgnoreMember] Culture _culture;
    [IgnoreMember] public Culture culture { 
        get
        {
            if (_culture == null) 
                _culture = objectManager.GetCulture(cultureId);
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
            if (_region == null) 
                _region = objectManager.GetRegion(regionId);
            return _region;
        }
        set
        {
            regionId = value?.id;
            _region = value;
        }
    }

    [IgnoreMember] public const float maxGoodMarketShare = 0.75f;

    public void ChangePopulation(int wfChange, int dfChange)
    {
        wfChange = Math.Max(wfChange, -workforce);
        dfChange = Math.Max(dfChange, -dependents);

        workforce += wfChange;
        dependents += dfChange;
        population += wfChange + dfChange;    

        culture.ChangePopulation(wfChange, dfChange, profession.id, culture);
        region.ChangePopulation(wfChange, dfChange, profession.id, culture);
    }
    public static bool CanPopsMerge(Pop a, Pop b)
    {
        if (a == null || b == null || a == b)
        {
            return false;
        }
        return a != b && a.profession == b.profession && Culture.CheckCultureSimilarity(a.culture, b.culture);
    }
    public Pop ChangeSocialClass(int workforceDelta, int dependentsDelta, Profession newProfession)
    {
        // Makes sure the profession is actually changing
        // And that we arent just creating an empty pop
        if (newProfession == profession || (workforceDelta < 1 && dependentsDelta < 1))
        {
            return null;
        }
        // Clamping
        workforceDelta = Math.Clamp(workforceDelta, 0, workforce);
        dependentsDelta = Math.Clamp(dependentsDelta, 0, dependents);

        // If we are changing the whole pop just change the socialClass
        if (workforceDelta == workforce && dependentsDelta == dependents)
        {
            profession = newProfession;
            return this;
        }
        // Makes a new pop with the new socialClass
        Pop newWorkers = objectManager.CreatePop(workforceDelta, dependentsDelta, region, tech, culture, newProfession.id);
        // And removes the people who switched to the new socialClass
        ChangePopulation(-workforceDelta, -dependentsDelta);
        // Land Stuff
        return newWorkers;
    }

    public void TechnologyUpdate()
    {
        float militaryTechChance = 0.0025f;
        float societyTechChance = 0.0025f;
        float industryTechChance = 0.05f;
        if (rng.NextSingle() < militaryTechChance && tech.militaryLevel < 20)
        {
            tech.militaryLevel += 1;
        }
        if (rng.NextSingle() < societyTechChance && tech.societyLevel < 20)
        {
            tech.societyLevel += 1;
        }
        if (tech.societyLevel >= 20 && tech.militaryLevel >= 20 && tech.industryLevel < 20 && rng.NextSingle() < industryTechChance)
        {
            tech.industryLevel += 1;
        }
    }
    public float CalculatePoliticalPower()
    {
        float popSizePoliticalPower = workforce * 0.005f;
        float basePoliticalPower = profession.politicalPower;
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
                //GD.Print("Migrate");
                migrateChance = 1f;
            }            
        }

        if (profession.id == "aristocrat" && !shipborne)
        {
            migrateChance *= 0.1f;
        }
        // If the pop migrates
        if (rng.NextSingle() > migrateChance) return;

        Region target = target = region.PickRandomBorder();

        bool socialClassAllows = true;

        // If the socialClass allows migration
        if (!shipborne)
        {
            switch (profession.id)
            {
                case "aristocrat":
                    if (target.owner != region.owner)
                    {
                        socialClassAllows = false;
                    }
                    break;
            }
            if (!socialClassAllows) return;            
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
            Pop newPop = objectManager.CreatePop(movedWorkforce, movedDependents, destination, tech, culture, profession.id);
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
        //GD.Print(workforceChange + dependentChange);
        ChangePopulation(workforceChange, dependentChange);
    }

    public void GetDemands()
    {
        goodsDemands = [];

        foreach (PopNeeds need in profession.needs)
        {
            float demandForNeed = (workforce * need.demandPerWorker) + (dependents * need.demandPerDependent);
            string stringNeedsType = need.type.ToString().ToLower();

            float totalSupply = 0;
            Dictionary<Item, float> itemsPresentInMarket = [];

            if (AssetManager.itemTags.TryGetValue(stringNeedsType, out List<Item> itemsInTag))
            {
                foreach (Item item in itemsInTag)
                {
                    float supply = Mathf.Max(region.economy.supply[item.id], 1);
                    if (itemsPresentInMarket.Count == 0|| region.economy.supply[item.id] > 0)
                    {
                        itemsPresentInMarket.Add(item, supply);
                        totalSupply += supply;
                    }
                }                
            }

            float remainingMarketShare = 1f;

            foreach (var pair in itemsPresentInMarket.OrderByDescending(x => x.Value))
            {
                Item item = pair.Key;

                // Calculates market share
                float marketShare = Mathf.Max(pair.Value, 1)/totalSupply * remainingMarketShare;

                // Makes sure demand isnt fully proportional
                if (marketShare < 1f && marketShare > maxGoodMarketShare)
                {
                    marketShare = maxGoodMarketShare;
                    remainingMarketShare = 1f - maxGoodMarketShare;
                    totalSupply -= pair.Value;
                }
                
                // Makes sure demand logging has items
                if (!goodsDemands.ContainsKey(item.id))
                {
                    goodsDemands[item.id] = 0;
                }

                // Adds item to demand
                goodsDemands[item.id] = demandForNeed * marketShare/item.basePrice;
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

public enum Direction{
    UP = 0,
	RIGHT = 1,
	DOWN = 2,
	LEFT = 3
}
