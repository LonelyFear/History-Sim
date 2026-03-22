using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using MessagePack;

[MessagePackObject(AllowPrivate = true)]

public class State : Organization, ISaveable
{
    [IgnoreMember] public string baseName = "Nation";
    [IgnoreMember] public string govtName;
    [IgnoreMember] public string leaderTitle { get; set; } = "King";
    [Key(1)] public StateAIManager AIManager;
    [Key(3)] public Color displayColor;
    [Key(4)] public Color capitalColor;
    [Key(5)] public bool capitualated = false;

    [Key(6)] public GovernmentType government { get; set; } = GovernmentType.MONARCHY;
    
    [IgnoreMember] public HashSet<Region> regions { get; set; } = new HashSet<Region>();
    [Key(7)] public HashSet<ulong> regionsIDs { get; set; } = new HashSet<ulong>();
    [IgnoreMember] public Region capital { get; set; }
    [Key(8)] public ulong capitalID;
    
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
    [Key(37)] public List<ulong> allianceIds = new List<ulong>();
    // Diplomacy Managers
    [Key(40)] public StateDiplomacyManager diplomacy = new StateDiplomacyManager();
    [Key(41)] public StateVassalManager vassalManager = new StateVassalManager();
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
        capitalID = capital.id;
        regionsIDs = [.. regions.Select(r => r.id)];
    }
    public void LoadFromSave()
    {
        AIManager.InitAI();
        LoadPopObjectFromSave();
        capital = objectManager.GetRegion(capitalID);
        regions = [.. regionsIDs.Select(r => objectManager.GetRegion(r))];
        diplomacy.Init(this);
        vassalManager.Init(this);
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
        if (capital.occupier != null)
        {
            if (!capitualated)
            {
                foreach (Region region in regions)
                {
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
        foreach (var warPair in diplomacy.warIds)
        {
            if (objectManager.GetWar(warPair.Key).warType == WarType.CIVIL_WAR)
            {
                return false;
            }
        }
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
                displayColor = vassalManager.GetLiege().color;
                break;
            case Sovereignty.PROVINCE:
                displayColor = vassalManager.GetLiege().color;
                break;
            case Sovereignty.PUPPET:
                displayColor = vassalManager.GetLiege().color;
                break;
        }
    }
    public void AddRegion(Region region)
    {
        if (!regions.Contains(region))
        {
            if (region.owner != null)
            {
                region.owner.RemoveRegion(region);
            }
            region.owner = this;
            regions.Add(region);
            foreach (Pop pop in region.pops)
            {
                pops.Add(pop);
            }
            region.conquered = true;
        }
    }
    public void RemoveRegion(Region region)
    {
        if (regions.Contains(region))
        {
            region.owner = null;
            regions.Remove(region);
            foreach (Pop pop in region.pops)
            {
                pops.Remove(pop);
            }
            region.conquered = true;
        }
    }
    public void UpdateStability()
    {
        double stabilityTarget = 1;
        //stabilityTarget -= diplomacy.warIds.Count * 0.05;

        if (largestCultureId != GetRulingCulture().id)
        {
            stabilityTarget -= 0.25;
        }

        Character leader = objectManager.GetCharacter(leaderId);
        if (leader == null)
        {
            stabilityTarget -= 0.25;
        } else
        {
            stabilityTarget += (leader.skills["stewardship"] - 50f) / 200;
            stabilityTarget += (leader.personality["empathy"] - 50f) / 400;
        }

        //stabilityTarget += totalWealth * 0.0001;

        if (rulingPop == null)
        {
            stabilityTarget *= 0.1;
        }
        if (vassalManager.vassalIds.Count > GetMaxVassals())
        {
            stabilityTarget -= (vassalManager.vassalIds.Count / GetMaxVassals()) - 1;
        }
        //stabilityTarget -= (poorTaxRate + middleTaxRate + richTaxRate)/3d * 0.3;

        stabilityTarget = Mathf.Clamp(stabilityTarget, 0, 1);
        stability = Mathf.Lerp(stability, stabilityTarget, 0.05);
    }
    public void UpdateLoyalty()
    {
        double loyaltyTarget = 1;
        State liege = vassalManager.GetLiege();
        if (largestCultureId != liege.GetRulingCulture().id)
        {
            loyaltyTarget -= 0.1;
        }
        if (largestCultureId != liege.largestCultureId)
        {
            loyaltyTarget -= 0.25;
        }
        if (regions.Count > liege.regions.Count)
        {
            if (liege.regions.Count <= 0)
            {
                timeManager.forcePause = true;
                simManager.mapManager.SelectMetaObject(liege);
                GD.Print(liege.name);
            }
            loyaltyTarget -= (regions.Count - liege.regions.Count)/liege.regions.Count * 0.5;
        }
        loyaltyTarget -= liege.tributeRate;
        loyaltyTarget -= Mathf.Min(capital.pos.DistanceTo(liege.capital.pos)/100d, 0.5f);

        loyaltyTarget = Mathf.Clamp(loyaltyTarget, 0, 1);
        loyalty = Mathf.Lerp(loyalty, loyaltyTarget, 0.05);
    }
    public List<State> GatherRebels()
    {
        List<State> rebels = [this];
        foreach (State vassal in vassalManager.GetLiege().vassalManager.GetVassals())
        {
            double joinChance = 0.5;
            if (rng.NextDouble() < joinChance && !rebels.Contains(vassal))
            {
                rebels.Add(vassal);
            }
        }
        return rebels;
    }
    
    public long GetArmyPower(bool realmPower = false)
    {
        if (realmId != null && sovereignty == Sovereignty.INDEPENDENT && realmPower)
        {
            return objectManager.GetAlliance(realmId).GetAllianceArmyPower();
        }
        float interiorArmyPower = GetManpower() / (float)regions.Count;
        return (long)interiorArmyPower;
    }

    public long GetManpower()
    {
        return manpower;
    }
    public int GetSize(bool includeRealm)
    {
        int size = regions.Count;
        if (realmId != null && sovereignty == Sovereignty.INDEPENDENT && includeRealm)
        {
            size = 0;
            Alliance realm = objectManager.GetAlliance(realmId);
            foreach (ulong memberId in realm.memberStateIds)
            {
                State memberState = objectManager.GetState(memberId);
                size += memberState.regions.Count;
            }
        }
        return size;
    }
    public int GetMaxRegionsCount()
    {
        return 20 + (tech.societyLevel * 2);
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
                desc += $"a vassal of the {GenerateUrlText(vassalManager.GetLiege(), vassalManager.GetLiege().name)}";
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