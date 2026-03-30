using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using MessagePack;

[MessagePackObject(AllowPrivate = true)]

public class State : Polity, ISaveable
{
    [IgnoreMember] public string baseName = "Nation";
    [IgnoreMember] public string govtName;
    [IgnoreMember] public string leaderTitle { get; set; } = "King";
    [Key(1)] public StateAIManager AIManager;
    [Key(3)] public Color displayColor;
    [Key(4)] public Color capitalColor;
    [Key(5)] public bool capitualated = false;

    [Key(6)] public GovernmentType government { get; set; } = GovernmentType.MONARCHY;
    
    //[IgnoreMember] public HashSet<Region> regions { get; set; } = new HashSet<Region>();
    //[Key(7)] public HashSet<ulong> regionsIds { get; set; } = new HashSet<ulong>();
    [Key(8)] public ulong capitalId;
    
    // Taxes & Wealth
    [Key(12)] public float mobilizationRate { get; set; } = 0.3f;
    [Key(13)] public float poorTaxRate { get; set; } = 0.3f;
    [Key(14)] public float middleTaxRate { get; set; } = 0.1f;
    [Key(15)] public float richTaxRate { get; set; } = 0.05f;
    [Key(16)] public float tributeRate { get; set; } = 0.1f;
    // Lieges & Vassals
    /*
    [IgnoreMember] public List<State> vassals { get; set; } = new List<State>();
    [Key(17)] public List<ulong> vassalsIDs { get; set; }
    [IgnoreMember] public State liege { get; set; } = null;
    [Key(18)] public ulong liegeID;
    */
    // Alliances
    [Key(21)] public Sovereignty sovereignty = Sovereignty.INDEPENDENT;
    [Key(36)] public ulong? realmId;
    [Key(40)] public StateDiplomacyManager diplomacy = new StateDiplomacyManager();
    //[Key(41)] public StateVassalManager vassalManager = new StateVassalManager();
    //[Key(22)] public Dictionary<ulong, int> borderingStatesIDs { get; set; }

    [Key(27)] public Tech tech = new Tech();

    [Key(28)] public int maxSize = 1;
    // Government
    [Key(30)] public Pop rulingPop;
    [Key(31)] public ulong? lastLeaderId = null;
    [Key(32)] public ulong? leaderId = null;
    [Key(33)] public List<ulong?> characterIds = new List<ulong?>();
    [Key(34)] public double stability = 1;
    [Key(35)] public double loyalty = 1;
    [IgnoreMember] public const double minRebellionLoyalty = 0.25;
    [IgnoreMember] public const double minCollapseStability = 0.75;
    [Key(367)] public uint timeAsVassal = 0;
    public void PrepareForSave()
    {
        PreparePopObjectForSave();
        //regionsIDs = [.. regions.Select(r => r.id)];
    }
    public void LoadFromSave()
    {
        AIManager.InitAI();
        LoadPopObjectFromSave();
        //regions = [.. regionsIDs.Select(r => objectManager.GetRegion(r))];
        diplomacy.Init(this);
    }
    public void UpdateCapital()
    {
        switch (sovereignty)
        {
            case Sovereignty.INDEPENDENT:
                capitalColor = new Color(1, 0, 0);
                break;
            case Sovereignty.PUPPET:
                capitalColor = new Color(0.25f, 0, 0);
                break;
            case Sovereignty.PROVINCE:
                capitalColor = new Color(0.5f, 0, 0.5f);
                break;
            case Sovereignty.COLONY:
                capitalColor = new Color(1, 0, 1);
                break;
        }
    }
    public void Capitualate()
    {
        Region capital = objectManager.GetRegion(capitalId);
        if (capital.occupier != null)
        {
            if (!capitualated)
            {
                foreach (ulong regionId in regionIds)
                {
                    Region region = objectManager.GetRegion(regionId);
                    if (capital.occupier != this && capital.occupier != null)
                    {
                        region.occupier = capital.occupier;
                    }
                }
            }
            capitualated = true;
        }
        else
        {
            capitualated = false;
        }
    }        
    public bool StateCollapse()
    {
        if (stability > minCollapseStability)
        {
            return false;
        }
        double stabilityFactor = 1d - (stability / minCollapseStability);
        if (rng.NextDouble() < stabilityFactor * 0.1)
        {
            if (rng.NextDouble() < 0.001)
            {
                //return true;
            }
        }
        return false;
    }
    public void SuccessionUpdate()
    {
        Character lastLeader = objectManager.GetCharacter(lastLeaderId);
        Character leader = objectManager.GetCharacter(leaderId);
        Character newLeader = null;
        switch (government)
        {
            case GovernmentType.REPUBLIC:
                // Republic TODO
                break;
            case GovernmentType.MONARCHY:
                // Monarchy
                // TODO: Make it relate to families
                // Right now just has a character with the same last name of the last guy
                if (lastLeader != null && lastLeader.dead)
                {
                    newLeader = objectManager.CreateCharacter(NameGenerator.GenerateCharacterName(), lastLeader.lastName, TimeManager.YearsToTicks(rng.Next(18, 25)), this, CharacterRole.LEADER);
                }
                break;
            case GovernmentType.AUTOCRACY:
                // Autocracy TODO
                break;
        }
        if (newLeader != null)
        {
            SetLeader(newLeader.id);
            objectManager.CreateHistoricalEvent([this, newLeader], EventType.SUCCESSION);
        }
    }
    public void SetLeader(ulong? characterId)
    {
        if (leaderId != null)
        {
            RemoveLeader();
        }
        leaderId = characterId;
    }
    public void RemoveLeader()
    {
         
        if (leaderId != null)
        {
            lastLeaderId = leaderId;
            Character lastLeader = objectManager.GetCharacter(lastLeaderId);
            lastLeader.role = CharacterRole.FORMER_LEADER;             
        }
        leaderId = null;
    }
    public void UpdateDisplayColor()
    {
        displayColor = color;
        switch (sovereignty)
        {
            case Sovereignty.COLONY:
                displayColor = diplomacy.GetOverlord().color;
                break;
            case Sovereignty.PROVINCE:
                displayColor = diplomacy.GetOverlord().color;
                break;
            case Sovereignty.PUPPET:
                displayColor = diplomacy.GetOverlord().color;
                break;
        }
    }
    public void AddRegion(Region region)
    {
        if (!regionIds.Contains(region.id))
        {
            region.owner?.RemoveRegion(region);
            region.owner = this;

            regionIds.Add(region.id);

            foreach (Pop pop in region.pops)
            {
                pops.Add(pop);
            }
            region.conquered = true;
        }
    }
    public void RemoveRegion(Region region)
    {
        if (!regionIds.Contains(region.id)) return;

        region.owner = null;
        regionIds.Remove(region.id);
        foreach (Pop pop in region.pops)
        {
            pops.Remove(pop);
        }
        region.conquered = true;
    }
    
    public long GetArmyPower(bool realmPower = false)
    {
        if (realmId != null && sovereignty == Sovereignty.INDEPENDENT && realmPower)
        {
            return objectManager.GetAlliance(realmId).GetAllianceArmyPower();
        }
        float interiorArmyPower = GetManpower();
        return (long)interiorArmyPower;
    }

    public long GetManpower()
    {
        return (long)(workforce * mobilizationRate);
    }
    public int GetSize(bool includeRealm)
    {
        int size = regionIds.Count;
        if (realmId != null && sovereignty == Sovereignty.INDEPENDENT && includeRealm)
        {
            size = 0;
            Alliance realm = objectManager.GetAlliance(realmId);
            foreach (ulong memberId in realm.memberStateIds)
            {
                State memberState = objectManager.GetState(memberId);
                size += memberState.regionIds.Count;
            }
        }
        return size;
    }
    public int GetMaxRegionsCount()
    {
        return 10 + (tech.societyLevel * 2);
    }
    public int GetMaxVassals() {
        return 5;
    } 
    public Culture GetRulingCulture()
    {
        if (rulingPop != null)
        {
            return rulingPop.culture;
        }
        return null;
    }
    public override string GenerateDescription()
    {
        Region capital = objectManager.GetRegion(capitalId);
        string desc = $"The {name} is a {govtName.ToLower()} in the simulation. It is ";
        if ("aeiou".Contains(govtName[0]))
        {
            desc = $"The {name} is an {govtName.ToLower()} in the simulation. It is ";
        }
        switch (sovereignty)
        {
            case Sovereignty.INDEPENDENT:
                desc += "an independent state";
                break;
            default:
                desc += $"a vassal of the {GenerateUrlText(diplomacy.GetLiege(), diplomacy.GetLiege().name)}";
                break;
        }
        desc += $" lead by {GenerateUrlText(objectManager.GetCharacter(leaderId), objectManager.GetCharacter(leaderId).name)}. "
        + $"It's capital is {GenerateUrlText(capital, capital.name)}, located at {capital.pos.X}, {capital.pos.Y}. ";
        return desc;
    }
    public override string GenerateStatsText()
    {
        string text = $"Name: {name}";
        text += $"\nPopulation: {population:#,###0}\n";
        
        if (population > 0)
        {
            text += $"Cultures Breakdown:\n";

            foreach (var cultureSizePair in cultureIds.OrderByDescending(pair => pair.Value))
            {
                Culture culture = objectManager.GetCulture(cultureSizePair.Key);
                long localPopulation = cultureSizePair.Value;
                
                // Skips if the culture is too small
                if (localPopulation < 1) continue;

                text += GenerateUrlText(culture, culture.name) + ":\n";
                text += $"  Population: {localPopulation:#,###0} ";

                float culturePercentage = localPopulation/(float)population;
                text += $"({culturePercentage:P0})\n";
            }    
            text += $"\nWorkforce: {workforce:#,###0}\n";
            /*
            text += $"Professions Breakdown:\n";     

            foreach (var professionSizePair in professions.OrderByDescending(pair => pair.Key))
            {
                SocialClass socialClass = professionSizePair.Key;
                long localPopulation = professionSizePair.Value;
                
                // Skips if the culture is too small
                if (Pop.FromNativePopulation(localPopulation) < 1) continue;
                text += $"{socialClass.ToString().Capitalize()}\n";

                text += $"  Workers: {Pop.FromNativePopulation(localPopulation):#,###0} ";
                float percentage = localPopulation/(float)workforce;
                text += $"({percentage:P0})\n";                
                long workersNeed = Math.Max(Pop.FromNativePopulation(requiredWorkers[socialClass]), 0);
                long maxWorkers = Pop.FromNativePopulation(maxJobs[socialClass]);
                text += $"  Employed: {maxWorkers - Math.Max(workersNeed, 0):#,###0}/{maxWorkers:#,###0}\n";
            }   
            */     
        }
        return text;
    }    
}   
public enum Sovereignty
{
    INDEPENDENT = 3,
    PUPPET = 2,
    COLONY = 1,
    PROVINCE = 0
}
public enum GovernmentType {
    REPUBLIC,
    MONARCHY,
    AUTOCRACY,
}
public enum WarType
{
    CONQUEST,
    CIVIL_WAR,
    REVOLT
}