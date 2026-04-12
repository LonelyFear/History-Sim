using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using MessagePack;
using System.ComponentModel.Design.Serialization;
using System.Security.Cryptography;
using System.Collections.Concurrent;

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
    [Key(37)] public float tributeRate { get; set; } = 0.1f;

    // Alliances
    [Key(38)] public Sovereignty sovereignty = Sovereignty.INDEPENDENT;
    [Key(39)] public StateDiplomacyManager diplomacy;

    [Key(40)] public Tech tech = new Tech();
    [Key(41)] public int maxSize = 1;

    // Government   
    [Key(42)] public List<ulong?> characterIds = [];
    [Key(43)] public float stability = 1;
    [Key(45)] public uint timeAsVassal = 0;
    [IgnoreMember] int timeUntilCapitulation = 12;
    [IgnoreMember] const float stabChangeChance = 0.01f;
    [IgnoreMember] const float baseCollapseChance = 0.01f;
    [IgnoreMember] Curve collapseChanceCurve = GD.Load<Curve>("res://Curves/Simulation/CollapseChanceCurve.tres");
    // Reference IDs
    [Key(46)] ulong? lastLeaderId = null;
    [Key(47)] ulong? leaderId = null;
    [Key(48)] ulong? rulingPopId;
    [Key(49)] ulong? capitalId;
    // References
    [IgnoreMember] public Culture culture;
    [IgnoreMember] Pop _rulingPop;
    [IgnoreMember] public Pop rulingPop { 
        get
        {
            if (_rulingPop == null && rulingPopId != null) 
                _rulingPop = objectManager.GetPop(rulingPopId);
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
                _leader = objectManager.GetCharacter(leaderId);
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
                _lastLeader = objectManager.GetCharacter(lastLeaderId);
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
                _capital = objectManager.GetRegion(capitalId);
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
        diplomacy.vassalIds = [..diplomacy.vassals.Select(v => v.id)];
        diplomacy.allianceIds = [..diplomacy.alliances.Select(v => v.id)];
        diplomacy.warIds = new ConcurrentDictionary<ulong, War.WarSide>(diplomacy.wars.Select(pair => new KeyValuePair<ulong, War.WarSide>(pair.Key.id, pair.Value)).ToDictionary());
        diplomacy.relationIds = new ConcurrentDictionary<ulong, Relation>(diplomacy.relations.Select(pair => new KeyValuePair<ulong, Relation>(pair.Key.id, pair.Value)).ToDictionary());
    }

    public override void LoadFromSave()
    {
        base.LoadFromSave();
        diplomacy.state = this;
        diplomacy.vassals = [..diplomacy.vassalIds.Select(objectManager.GetState)];
        diplomacy.alliances = [..diplomacy.allianceIds.Select(objectManager.GetAlliance)];
        diplomacy.wars = new ConcurrentDictionary<War, War.WarSide>(diplomacy.warIds.Select(pair => new KeyValuePair<War, War.WarSide>(objectManager.GetWar(pair.Key), pair.Value)).ToDictionary());
        diplomacy.relations = new ConcurrentDictionary<State, Relation>(diplomacy.relationIds.Select(pair => new KeyValuePair<State, Relation>(objectManager.GetState(pair.Key), pair.Value)).ToDictionary());
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
        if (capital == null || capital.occupier == null){
            timeUntilCapitulation = 12;
            capitualated = false; 
            return;           
        };

        timeUntilCapitulation--;
        if (timeUntilCapitulation > 0) return;

        if (!capitualated)
        {
            foreach (Region region in regions)
            {
                region.occupier ??= capital.occupier;
            }
            capitualated = true;
        }
    }  
    public State GetOccupier()
    {
        if (!capitualated) return null;
        return capital.occupier;
    }      
    public bool StateCollapse()
    {
        if (rng.NextSingle() < collapseChanceCurve.Sample(stability) * baseCollapseChance)
        {
            List<State> potentialRebels = GetRebelliousVassals();
            bool inCivilConflict = diplomacy.InWarOfType(WarType.CIVIL_WAR) || diplomacy.InWarOfType(WarType.REVOLT);

            if (potentialRebels.Count < 1 && !inCivilConflict)
            {
                return true;
            }

            if (!inCivilConflict)
            {
                // Starts a civil war
                State leadRebel = potentialRebels[0];
                leadRebel.sovereignty = Sovereignty.REBELLIOUS;
                War civilWar = leadRebel.diplomacy.DeclareWar(this, WarType.CIVIL_WAR);

                foreach (State rebel in potentialRebels)
                {
                    if (rebel == leadRebel) continue;
                    rebel.sovereignty = Sovereignty.REBELLIOUS;
                    civilWar.AddParticipant(rebel, War.WarSide.AGRESSOR);
                }
                //GD.Print(leadRebel.diplomacy.IsEnemyWithState(this));
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
        foreach (State vassal in diplomacy.vassals)
        {
            Relation relationsWithUs = vassal.diplomacy.GetRelationsWithState(this);
            if (relationsWithUs.opinion < 0)
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
                string lastName = lastLeader == null ? NameGenerator.GenerateCharacterName() : lastLeader.lastName;

                newLeader = objectManager.CreateCharacter(NameGenerator.GenerateCharacterName(), lastName, TimeManager.YearsToTicks(rng.Next(18, 25)), this, CharacterRole.LEADER);
                
                break;
            case GovernmentType.AUTOCRACY:
                // Autocracy TODO
                break;
        }
        if (newLeader != null)
        {
            SetLeader(newLeader);
            objectManager.CreateHistoricalEvent([this, newLeader], EventType.SUCCESSION);
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
                displayColor = diplomacy.GetOverlord().color;
                break;
            case Sovereignty.PROVINCE:
                displayColor = diplomacy.GetOverlord().color;
                break;
            case Sovereignty.PUPPET:
                displayColor = diplomacy.GetOverlord().color;
                break;
            case Sovereignty.REBELLIOUS:
                displayColor = Utility.MultiColourLerp([diplomacy.GetOverlord().color, new Color(0, 0, 0)], 0.5f);
                break;
        }
    }
    public void AddRegion(Region region)
    {
        if (region == null || regions.Contains(region)) return;

        region.owner?.RemoveRegion(region);
        region.owner = this;

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

        foreach (Pop pop in region.pops)
        {
            pops.Remove(pop);
        }
        region.conquered = true;
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
                desc += $"a vassal of the {GenerateUrlText(diplomacy.GetLiege(), diplomacy.GetLiege().name)}";
                break;
        }
        desc += $" lead by {(leader == null ? "nobody" : GenerateUrlText(leader, leader.name))}. "
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
        }
        return text;
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
public enum WarType
{
    CONQUEST,
    CIVIL_WAR,
    REVOLT
}