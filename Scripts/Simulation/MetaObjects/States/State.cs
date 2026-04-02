using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using MessagePack;

[MessagePackObject(AllowPrivate = true)]
public partial class State : Polity, ISaveable
{
    [Key(88)] public string baseName = "Nation";
    [Key(77)] public string govtName;
    [Key(99)] public string leaderTitle { get; set; } = "King";
    [Key(1)] public StateAIManager AIManager;
    [Key(3)] public Color displayColor;
    [Key(4)] public Color capitalColor;
    [Key(5)] public bool capitualated = false;

    [Key(6)] public GovernmentType government { get; set; } = GovernmentType.MONARCHY;
    
    //[IgnoreMember] public HashSet<Region> regions { get; set; } = new HashSet<Region>();
    //[Key(7)] public HashSet<ulong> regionsIds { get; set; } = new HashSet<ulong>();
    
    
    // Taxes & Wealth
    [Key(12)] public float mobilizationRate { get; set; } = 0.3f;
    [Key(13)] public float poorTaxRate { get; set; } = 0.3f;
    [Key(14)] public float middleTaxRate { get; set; } = 0.1f;
    [Key(15)] public float richTaxRate { get; set; } = 0.05f;
    [Key(16)] public float tributeRate { get; set; } = 0.1f;

    // Alliances
    [Key(21)] public Sovereignty sovereignty = Sovereignty.INDEPENDENT;
    [Key(40)] public StateDiplomacyManager diplomacy;

    [Key(27)] public Tech tech = new Tech();
    [Key(28)] public int maxSize = 1;

    // Government   
    [Key(33)] public List<ulong?> characterIds = [];
    [Key(34)] public double stability = 1;
    [Key(35)] public double loyalty = 1;
    [IgnoreMember] public const double minRebellionLoyalty = 0.25;
    [IgnoreMember] public const double minCollapseStability = 0.75;
    [Key(367)] public uint timeAsVassal = 0;

    // Reference IDs
    [Key(31)] ulong? lastLeaderId = null;
    [Key(32)] ulong? leaderId = null;
    [Key(67)] ulong? rulingPopId;
    [Key(8)] ulong? capitalId;
    // References
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
    public override void LoadFromSave()
    {
        diplomacy.state = this;
        PolityLoad();
        PopObjectLoad();
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
        if (capital == null) return;

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
    public State GetOccupier()
    {
        if (!capitualated) return null;
        return capital.occupier;
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