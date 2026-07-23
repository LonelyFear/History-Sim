using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using MessagePack;
using PixelHistory.Objects.States.AI;
using PixelHistory.Objects.States.Diplomacy;
using PixelHistory.Objects.Wars;

namespace PixelHistory.Objects.States.Base;
[MessagePackObject(AllowPrivate = true)]
public partial class State : Polity, ISaveable
{
    [Key(25)] public string baseName = "Nation";
    [Key(26)] public string govtName;
    [Key(27)] public string leaderTitle { get; set; } = "King";
    [Key(28)] public StateAIManager AIManager;
    [Key(29)] public Color displayColor;
    [Key(30)] public Color capitalColor;
    [Key(31)] public bool capitualated = false;

    [Key(32)] public GovernmentType government { get; set; } = GovernmentType.MONARCHY;
    
    // Taxes & Wealth
    [Key(33)] public float mobilizationRate { get; set; } = 0.3f;
    [Key(34)] public float taxRate { get; set; } = 0.3f;
    //[Key(37)] public float tributeRate { get; set; } = 0.1f;

    // Alliances
    [Key(38)] public Sovereignty sovereignty = Sovereignty.INDEPENDENT;
    //[Key(39)] public StateDiplomacyManager diplomacy;

    [Key(41)] public int maxSize = 1;

    // Government   
    [Key(42)] public List<ulong?> characterIds = [];
    [Key(43)] public float stability = 1;
    [Key(45)] public uint timeAsVassal = 0;
    [IgnoreMember] const float stabChangeChance = 0.01f;
    [IgnoreMember] const float baseCollapseChance = 0.01f;
    [IgnoreMember] Curve collapseChanceCurve = GD.Load<Curve>("res://Curves/Simulation/CollapseChanceCurve.tres");
    // Reference IDs
    [Key(46)] ulong? lastLeaderId = null;
    [Key(47)] ulong? leaderId = null;
    [Key(48)] ulong? rulingPopId;
    [Key(49)] ulong? capitalId;
    [Key(50)] public List<ulong> relationIds { get; set; } = [];
    [IgnoreMember] public Dictionary<State, DiplomaticRelations> relations { get; set; } = [];
    [Key(51)] public Dictionary<ulong, War.WarSide> warIds { get; set; } = [];
    [IgnoreMember] public Dictionary<War, War.WarSide> wars { get; set; } = [];
    [Key(52)] public ulong? liegeId {get; set; } = null;
    [Key(53)] public List<ulong?> allianceIds = [];
    [IgnoreMember] public List<Alliance> alliances = [];
    [Key(54)] public HashSet<ulong?> vassalIds { get; set; } = [];
    [IgnoreMember] public HashSet<State> vassals = [];

    [Key(56)] public HashSet<ulong?> enemyIds = [];
    [IgnoreMember] public HashSet<State> enemies = [];
    [Key(57)] public HashSet<ulong?> claimIds = [];
    [IgnoreMember] public HashSet<Region> claims = [];
    // References
    [IgnoreMember] public Culture culture;
    [IgnoreMember] Pop _rulingPop;
    [IgnoreMember] public Pop rulingPop { 
        get
        {
            if (_rulingPop == null && rulingPopId != null) 
                _rulingPop = ObjectManager.GetPop(rulingPopId);
            return _rulingPop;
        } 
        set
        {
            rulingPopId = value?.id;
            _rulingPop = value;
        } 
    } 

    [IgnoreMember] Character _leader;
    [IgnoreMember] Character _lastLeader;
    [IgnoreMember] public Character leader { 
        get
        {
            if (_leader == null && leaderId != null) 
                _leader = ObjectManager.GetCharacter(leaderId);
            return _leader;
        } 
        set
        {
            leaderId = value?.id;
            _leader = value;
        } 
    }
    
    [IgnoreMember] public Character lastLeader { 
        get
        {
            if (_lastLeader == null && lastLeaderId != null) 
                _lastLeader = ObjectManager.GetCharacter(lastLeaderId);
            return _lastLeader;
        } 
        set
        {
            lastLeaderId = value?.id;
            _lastLeader = value;
        } 
    }
    [IgnoreMember] Region _capital;
    [IgnoreMember] public Region capital { 
        get
        {
            if (_capital == null && capitalId != null) 
                _capital = ObjectManager.GetRegion(capitalId);
            return _capital;
        } 
        set
        {
            capitalId = value?.id;
            _capital = value;
        } 
    }
    public override void PrepareForSave()
    {
        base.PrepareForSave();
        vassalIds = [..vassals.Select(v => v.id)];
        allianceIds = [..alliances.Select(v => v.id)];
        enemyIds = [..enemies.Select(v => v.id)];
        warIds = new Dictionary<ulong, War.WarSide>(wars.Select(pair => new KeyValuePair<ulong, War.WarSide>(pair.Key.id, pair.Value)).ToDictionary());
        relationIds = [..relations.Select(r => r.Value.id)];
    }

    public override void LoadFromSave()
    {
        base.LoadFromSave();
        vassals = [..vassalIds.Select(ObjectManager.GetState)];
        alliances = [..allianceIds.Select(ObjectManager.GetAlliance)];
        enemies = [..enemyIds.Select(ObjectManager.GetState)];
        wars = new Dictionary<War, War.WarSide>(warIds.Select(pair => new KeyValuePair<War, War.WarSide>(ObjectManager.GetWar(pair.Key), pair.Value)).ToDictionary());
        foreach (ulong relationId in relationIds)
        {
            DiplomaticRelations relation = simManager.relationIds[relationId];
            if (relation.initiatorId == id)
            {
                relations.Add(ObjectManager.GetState(relation.recipientId), relation);
            } else
            {
                relations.Add(ObjectManager.GetState(relation.initiatorId), relation); 
            }
        }
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
    public void FindNewRulingPop()
    {
        foreach (Pop pop in capital.pops)
        {
            if (pop.culture == culture)
            {
                rulingPop = pop;
            }
        }
    }
    public void Capitualate()
    {
        capitualated = false; 
        if (capital != null && capital.owner != capital.claimant){
            capitualated = true; 

            capital.owner.GetOverlord().AddVassal(this, Sovereignty.PUPPET);
            foreach (Region claim in claims)
            {
                AddRegion(claim, true);
            }            
        }
    } 
    public bool StateCollapse()
    {
        if (rng.NextSingle() < collapseChanceCurve.Sample(stability) * baseCollapseChance)
        {
            List<State> potentialRebels = GetRebelliousVassals();
            bool inCivilConflict = StateDiplomacyManager.InWarOfType(this, WarType.CIVIL_WAR) || StateDiplomacyManager.InWarOfType(this, WarType.REVOLT);

            if (potentialRebels.Count < 1 && !inCivilConflict)
            {
                return true;
            }

            if (!inCivilConflict)
            {
                // Starts a civil war
                State leadRebel = potentialRebels[0];
                leadRebel.sovereignty = Sovereignty.REBELLIOUS;
                War civilWar = ObjectManager.StartWar( WarType.CIVIL_WAR, leadRebel, this);

                foreach (State rebel in potentialRebels)
                {
                    if (rebel == leadRebel) continue;
                    rebel.sovereignty = Sovereignty.REBELLIOUS;
                    civilWar.AddParticipant(rebel, War.WarSide.AGRESSOR);
                }
                //GD.Print(leadRebel.IsEnemyWithState(this));
            }                

        }
        return false;
    }
    public void UpdateStability()
    {
        if (rng.NextSingle() >= stabChangeChance) return;

        float stabilityScore = rng.NextSingle();

        float positiveChance = 0.2f;
        if (leader != null)
        {
            positiveChance = leader.GetPersonalityLevel("agression") switch
            {
                TraitLevel.HIGH => 0.75f,
                TraitLevel.MEDIUM => 0.5f,
                TraitLevel.LOW => 0.25f,
                _ => 1f,
            };            
        }

        if (stabilityScore < positiveChance)
        {
            stability += 0.05f;
        } else
        {
            stability -= 0.05f;
        }
        stability = Math.Clamp(stability, 0, 1);
    }
    public List<State> GetRebelliousVassals()
    {
        List<State> rebels = [];
        foreach (State vassal in vassals)
        {
            if (relations[vassal].opinion < 0)
            {
                rebels.Add(vassal);
            }
        }
        return rebels;
    }
    public void SuccessionUpdate()
    {
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
                string lastName = lastLeader == null ? NameGenerator.GenerateCharacterName(simManager.rng) : lastLeader.lastName;

                newLeader = ObjectManager.CreateCharacter(NameGenerator.GenerateCharacterName(simManager.rng), lastName, TimeManager.YearsToTicks(rng.Next(18, 25)), this, CharacterRole.LEADER);
                
                break;
            case GovernmentType.AUTOCRACY:
                // Autocracy TODO
                break;
        }
        if (newLeader != null)
        {
            SetLeader(newLeader);
            ObjectManager.CreateHistoricalEvent([this, newLeader], EventType.SUCCESSION);
        }
    }
    public void SetLeader(Character character)
    {
        if (leader != null)
        {
            RemoveLeader();
        }
        character.SetRole(CharacterRole.LEADER);
        leader = character;
    }
    public void RemoveLeader()
    {
        if (leader == null) return;

        leader.SetRole(CharacterRole.FORMER_LEADER);       
        lastLeader = leader;

        leader = null;
    }
    public void UpdateDisplayColor()
    {
        displayColor = color;

        switch (sovereignty)
        {
            case Sovereignty.COLONY:
                displayColor = StateDiplomacyManager.GetOverlord(this).color;
                break;
            case Sovereignty.PROVINCE:
                displayColor = StateDiplomacyManager.GetOverlord(this).color;
                break;
            case Sovereignty.PUPPET:
                displayColor = StateDiplomacyManager.GetOverlord(this).color;
                break;
            case Sovereignty.REBELLIOUS:
                displayColor = Utility.MultiColourLerp([StateDiplomacyManager.GetOverlord(this).color, new Color(0, 0, 0)], 0.5f);
                break;
        }
    }
    public void AddRegion(Region region, bool includeClaimant)
    {
        if (region == null || regions.Contains(region)) return;

        region.owner?.RemoveRegion(region);
        region.owner = this;
        if (includeClaimant) AddClaim(region);

        regions.Add(region);

        foreach (Pop pop in region.pops)
        {
            pops.Add(pop);
        }
        region.conquered = true;
    }
    public void RemoveRegion(Region region)
    {
        if (region == null || !regions.Remove(region)) return;

        region.owner = null;
        //if (region.claimant != this) region.claimant?.AddRegion(region, true);

        foreach (Pop pop in region.pops)
        {
            pops.Remove(pop);
        }
        region.conquered = true;
    }
    public void AddClaim(Region region)
    {
        if (region.claimant != this) region.claimant?.RemoveClaim(region);
        region.claimant = this;
        claims.Add(region);
    }
    public void RemoveClaim(Region region)
    {
        region.claimant = region.owner;
        claims.Remove(region);
    }
    // Gets the power used by regions
    public int GetCombatPower()
    {
        if (sovereignty == Sovereignty.INDEPENDENT)
        {
            return armyPower;
        } 
        else if (sovereignty == Sovereignty.REBELLIOUS)
        {
            return (int)(this.GetWarWithState(ObjectManager.GetState(liegeId))?.GetSideArmyPower(War.WarSide.AGRESSOR));
        } 
        else
        {
            return this.GetLiege().GetCombatPower();
        }
    }
    public override int GetArmyPower()
    {
        float size = regions.Count;
        float wealth = totalWealth;
        foreach (State vassal in vassals)
        {
            if (vassal.sovereignty == Sovereignty.REBELLIOUS) continue;

            wealth += vassal.totalWealth;
            size += vassal.regions.Count;
        }
        return (int)(Math.Log10(wealth * wealth)/size * (tech.militaryLevel + 1));
    }
    public override int GetManpower()
    {
        return (int)(workforce * 0.05f);
    }
    public int GetMaxRegionsCount()
    {
        return 5 + (tech.societyLevel * 2);
    }
    public int GetMaxVassals() {
        return 5;
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
                desc += $"a vassal of the {GenerateUrlText(StateDiplomacyManager.GetLiege(this), StateDiplomacyManager.GetLiege(this).name)}";
                break;
        }
        desc += $" lead by {(leader == null ? "nobody" : GenerateUrlText(leader, leader.name))}. "
        + $"It's capital is {GenerateUrlText(capital, capital.name)}, located at {capital.pos.X}, {capital.pos.Y}. ";
        return desc;
    }  
}   
public enum Sovereignty
{
    REBELLIOUS = 20,
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
