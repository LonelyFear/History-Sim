using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using MessagePack;
using System.Data.Common;
[MessagePackObject]

public class State : PopObject, ISaveable
{
    [IgnoreMember] public string displayName = "Nation";
    [IgnoreMember] public string govtName;
    [IgnoreMember] public string leaderTitle { get; set; } = "King";
    [Key(2)] public Color color { get; set; }
    [Key(3)] public Color displayColor;
    [Key(4)] public Color capitalColor;
    [Key(5)] public bool capitualated = false;

    [Key(6)] public GovernmentType government { get; set; } = GovernmentType.MONARCHY;
    
    [IgnoreMember] public List<Region> regions { get; set; } = new List<Region>();
    [Key(7)] public List<ulong> regionsIDs { get; set; } = new List<ulong>();
    [IgnoreMember] public Region capital { get; set; }
    [Key(8)] public ulong capitalID;
    [Key(9)] public long manpower { get; set; } = 0;
    [Key(10)] public int occupiedLand { get; set; } = 0;
    // Taxes & Wealth
    [Key(11)] public float totalWealth { get; set; } = 0;
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
    [Key(36)] public ulong? realmId;
    [Key(37)] public List<ulong> allianceIds = new List<ulong>();
    // Diplomacy Managers
    [Key(40)] public StateDiplomacyManager diplomacy = new StateDiplomacyManager();
    [Key(41)] public StateVassalManager vassalManager = new StateVassalManager();
    // Borders & Sovereignty
    [IgnoreMember] public List<State> borderingStates { get; set; } = new List<State>();
    [Key(22)] public List<ulong> borderingStatesIDs { get; set; }
    [Key(23)] public int borderingRegions { get; set; } = 0;
    [Key(24)] public int externalBorderingRegions { get; set; }= 0;
    [Key(25)] public Sovereignty sovereignty { get; set; } = Sovereignty.INDEPENDENT;

    [Key(27)] public Tech tech = new Tech();

    [Key(28)] public int maxSize = 1;
    // Government
    [Key(29)] public long wealth;
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
        regionsIDs = regions.Select(r => r.id).ToList();
        borderingStatesIDs = borderingStates.Count > 0 ? borderingStates.Select(r => r.id).ToList() : null;
    }
    public void LoadFromSave()
    {
        LoadPopObjectFromSave();
        capital = objectManager.GetRegion(capitalID);
        regions = regionsIDs.Select(r => objectManager.GetRegion(r)).ToList();
        diplomacy.Init(this);
        vassalManager.Init(this);
        borderingStates = borderingStatesIDs == null ? new List<State>() : borderingStatesIDs.Select(r => simManager.statesIds[r]).ToList();
    }
    public void UpdateCapital()
    {
        if (capital == null)
        {
            capital = regions[0];
        }
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
            if (vassalManager.vassalIds.Count > 0)
            {
                State mainRebel = null;
                foreach (State vassal in vassalManager.GetVassals())
                {
                    if (mainRebel == null || vassal.loyalty < mainRebel.loyalty)
                    {
                        mainRebel = vassal;
                    }
                }

                List<State> fellowRebels = mainRebel.GatherRebels();
                foreach (State rebel in fellowRebels)
                {
                    vassalManager.RemoveVassal(rebel.id);
                }
                objectManager.StartWar(fellowRebels, [this], WarType.CIVIL_WAR, mainRebel.id, id);
            }
            else
            {
                if (rng.NextDouble() < 0.001)
                {
                    return true;
                }
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
            simManager.charactersIds[(ulong)lastLeaderId].role = CharacterRole.FORMER_LEADER;
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
    public override void CountPopulation()
    {
        List<Region> checkedRegions = regions;
        Alliance realm = objectManager.GetAlliance(realmId);
        bool isRealmLeader = realm == null ? false : realm.leadStateId == id;
        if (realm != null && isRealmLeader)
        {
            checkedRegions = realm.GetRegions();
        }

        int aliveCharacters = 0;
        long countedP = 0;
        long countedW = 0;

        List<Pop> countedPops = new List<Pop>();
        List<State> borders = new List<State>();
        Dictionary<SocialClass, long> countedSocialClasss = new Dictionary<SocialClass, long>();
        foreach (SocialClass profession in Enum.GetValues(typeof(SocialClass)))
        {
            countedSocialClasss.Add(profession, 0);
        }

        Dictionary<Culture, long> cCultures = new Dictionary<Culture, long>();
        borderingRegions = 0;
        float countedWealth = 0;
        int occRegions = 0;
        foreach (ulong charId in characterIds)
        {
            if (simManager.charactersIds[charId].role != CharacterRole.DEAD)
            {
                aliveCharacters++;
            }
        }
        // If realm leader uses realm stats
        foreach (Region region in checkedRegions)
        {
            countedP += region.population;
            countedW += region.workforce;
            countedWealth += region.wealth;
            countedPops.AddRange(region.pops);
            if (region.frontier || region.border)
            {
                if (region.occupier != null && regions.Contains(region))
                {
                    occRegions++;
                }
                borderingRegions++;
                bool bordersOtherState = false;
                // Gets the borders of our state
                foreach (Region border in region.borderingRegions)
                {
                    // State
                    State borderState = border.owner;
                    // Makes sure the state is real
                    if (borderState != null && borderState != this)
                    {
                        // Adds the bordering state
                        if (!borders.Contains(borderState))
                        {
                            borders.Add(borderState);
                        }
                        // Adds the bordering realm leader
                        if (!borders.Contains(borderState.vassalManager.GetOverlord(true)))
                        {
                            borders.Add(borderState.vassalManager.GetOverlord(true));
                        }
                        // If we arent in a realm everything is considered outside of us
                        bordersOtherState = true;                        
                        
                        // Checks if the state is in our realm
                        if (realm != null)
                        {
                            bordersOtherState = realm.memberStateIds.Contains(borderState.id);
                        }
                    }
                }
                // External Bordering Regions is the borders on the outside of the realm
                // (NO INTERIOR BORDERS)
                if (bordersOtherState)
                {
                    externalBorderingRegions++;
                }
            }

            foreach (SocialClass profession in region.professions.Keys)
            {
                countedSocialClasss[profession] += region.professions[profession];
            }
            foreach (Culture culture in region.cultures.Keys)
            {
                if (cCultures.ContainsKey(culture))
                {
                    cCultures[culture] += region.cultures[culture];
                }
                else
                {
                    cCultures.Add(culture, region.cultures[culture]);
                }
            }
        }
        occupiedLand = occRegions;
        borderingStates = borders;
        totalWealth = countedWealth;
        professions = countedSocialClasss;
        cultures = cCultures;
        population = countedP + aliveCharacters;
        workforce = countedW;
        pops = countedPops;
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
            pops.AddRange(region.pops);
        }
    }
    public void RemoveRegion(Region region)
    {
        if (regions.Contains(region))
        {
            region.owner = null;
            regions.Remove(region);
            pops.RemoveAll(p => p.region == region);
        }
    }
    public void UpdateStability()
    {
        double stabilityTarget = 1;
        //stabilityTarget -= diplomacy.warIds.Count * 0.05;

        if (largestCulture != GetRulingCulture())
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
        if (largestCulture != liege.GetRulingCulture())
        {
            loyaltyTarget -= 0.1;
        }
        if (largestCulture != liege.largestCulture)
        {
            loyaltyTarget -= 0.25;
        }
        if (regions.Count > liege.regions.Count)
        {
            if (liege.regions.Count <= 0)
            {
                timeManager.forcePause = true;
                simManager.mapManager.selectedMetaObj = liege;
                simManager.mapManager.UpdateRegionColors(simManager.regions);
                GD.Print(liege.displayName);
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
    public long GetArmyPower(bool includeRealm)
    {
        float interiorArmyPower = GetManpower(includeRealm) / (float)GetRegions(includeRealm).Count;
        return (long)interiorArmyPower;
    }
    public List<Region> GetRegions(bool includeRealm)
    {
        List<Region> collectedRegions = [.. regions];
        if (realmId == null || !includeRealm)
        {
            return collectedRegions;
        } else
        {
            return objectManager.GetAlliance(realmId).GetRegions();
        }
    }
    public long GetManpower(bool includeRealm)
    {
        if (includeRealm && realmId != null)
        {
            return objectManager.GetAlliance(realmId).GetAllianceManpower();
        }
        return manpower;
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
        string desc = $"The {displayName} is a {govtName.ToLower()} in the simulation. It is ";
        if ("aeiou".Contains(govtName[0]))
        {
            desc = $"The {displayName} is an {govtName.ToLower()} in the simulation. It is ";
        }
        switch (sovereignty)
        {
            case Sovereignty.INDEPENDENT:
                desc += "an independent state";
                break;
            default:
                desc += $"a vassal of the {GenerateUrlText(vassalManager.GetLiege(), vassalManager.GetLiege().displayName)}";
                break;
        }
        desc += $" lead by {GenerateUrlText(objectManager.GetCharacter(leaderId), objectManager.GetCharacter(leaderId).name)}. "
        + $"It's capital is {GenerateUrlText(capital, capital.name)}, located at {capital.pos.X}, {capital.pos.Y}. ";
        return desc;
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