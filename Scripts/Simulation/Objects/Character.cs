using System;
using System.Collections.Generic;
using System.Net.Quic;
using Godot;
using MessagePack;
[MessagePackObject(AllowPrivate = true)]
public partial class Character : NamedObject
{
    // Constants
    [IgnoreMember] const float hdChanceAnnualGrowth = 0.02f;
    [IgnoreMember] const int agingHealthDecrease = 3; 
    [IgnoreMember] public const int dieHealthThreshold = 40; 
    
    // Ignored Members   
    [IgnoreMember] public Random rng = new Random();
    [IgnoreMember] public static SimManager sim;

    // Character Stats
    [Key(7)] public string firstName;
    [Key(8)] public string lastName;
    [Key(9)] public ulong? stateId = null;
    [Key(10)] public CharacterRole role {get; private set; } = CharacterRole.CIVILIAN;
    [Key(11)] public List<ulong> parentIds = [];
    [Key(12)] public List<ulong?> childIds = [];
    [Key(13)] public Dictionary<ulong, int> relationsIds = [];
    // Health
    [Key(14)] public int health = 100;
    [Key(15)] public float healthDecreaseChance = 0.075f;

    // Character Personality
    // Personality changes interaction/actions
    [Key(16)]
    public Dictionary<string, float> personality {get; private set;} = new Dictionary<string, float>
    {
        {"agression", 0.5f},
        {"leadership", 0.5f},
        {"ambition", 0.5f},
    };
    [Key(17)] public Gender gender = Gender.MALE;

    // References
    [IgnoreMember] State _state;
    [IgnoreMember] public State state { 
        get
        {
            if (_state == null && stateId != null) 
                _state = objectManager.GetState(stateId);
            return _state;
        } 
        set
        {
            stateId = value?.id;
            _state = value;
        } 
    }   
    
    public void JoinState(State target)
    {
        if (state != null)
        {
            LeaveState();
        }
        state = target;
        state.characterIds.Add(id);
    }
    public void LeaveState()
    {
        if (state == null) return;

        if (state.leader == this)
        {
            state.RemoveLeader();
        }
        state.characterIds.Remove(id);
        state = null;
    }    
    public void SetRole(CharacterRole newRole)
    {
        role = newRole;
    }
    public override void Die()
    {

        dead = true;
        tickDestroyed = sim.timeManager.ticks;
        objectManager.CreateHistoricalEvent([this, role == CharacterRole.LEADER ? state : null], EventType.DEATH);

        if (state == null) return;
        
        if (state.leader == this)
        {
            state.RemoveLeader();
        }
    }
    public void CharacterAging()
    {
        healthDecreaseChance += hdChanceAnnualGrowth;
        if (rng.NextSingle() < healthDecreaseChance)
        {
            health -= agingHealthDecrease;
        }
    }
    public TraitLevel GetPersonalityLevel(string trait)
    {
        TraitLevel level;

        if (personality[trait] > 0.66f)
        {
            level = TraitLevel.HIGH;
        }
        else if (personality[trait] > 0.33f)
        {
            level = TraitLevel.MEDIUM;
        }
        else // traitStrength > 0
        {
            level = TraitLevel.LOW;
        }
        return level;
    }

    public override string GenerateDescription()
    {
        string[] pronouns = ["he", "she"];
        int intGender = (int)gender;

        string pronoun = pronouns[intGender];
        string w = dead ? "was" : "is";

        string desc = $"{name} {w} a character born in {sim.timeManager.GetStringDate(tickCreated, true)} to ";

        if (parentIds.Count <= 0)
        {
            desc += "unknown parents";
        } else
        {
            Character parentOne = objectManager.GetCharacter(parentIds[0]);
            Character parentTwo = objectManager.GetCharacter(parentIds[1]);
            desc += $"{(parentOne == null ? "an unknown parent" : GenerateUrlText(parentOne, parentOne.name))} and {(parentTwo == null ? "an unknown parent" : GenerateUrlText(parentTwo, parentTwo.name))}";      
        }
        desc += $". {pronoun.Capitalize()} ";
        
        // Role
        desc += role switch
        {
            CharacterRole.LEADER => $"{w} the {state.leaderTitle.ToLower()} of the {GenerateUrlText(state, state.name)}",
            CharacterRole.HEIR => $"{w} the heir to the {GenerateUrlText(state, state.name)}",
            CharacterRole.COMMANDER => $"{w} a general in the army of the {GenerateUrlText(state, state.name)}",
            CharacterRole.POLITICIAN => $"{w} a politician in the {GenerateUrlText(state, state.name)}",
            CharacterRole.NOBLE => $"{w} a noble in the {GenerateUrlText(state, state.name)}",
            _ => $"{(dead ? "lived" : "is living")} in the {GenerateUrlText(state, state.name)}",
        };

        desc += $", and {pronoun} {w} {sim.timeManager.GetYear(GetAge())} years old" 
        + (dead ? $" when {pronoun} died. " : ". ");
        // Personality
        string agressionString = GetPersonalityLevel("agression") switch
        {
            TraitLevel.HIGH => "an Agressive stance on affairs",
            TraitLevel.MEDIUM => "a Neutral stance on affairs",
            TraitLevel.LOW => "a Passive stance on affairs",
            _ => "an Arbitrary stance, making desicions on a whim"
        };
        desc += $"{pronoun.Capitalize()} {(dead ? "had" : "has")} {agressionString}, and ";

        string leadershipString = GetPersonalityLevel("leadership") switch
        {
            TraitLevel.HIGH => $"a brilliant leader",
            TraitLevel.MEDIUM => $"a capable leader",
            TraitLevel.LOW => $"a foolish leader",
            _ => "an unpredictable leader"
        }; 
        desc += $"{pronoun} {(dead ? "was known as" : "is known as")} {leadershipString}. ";

        string ambitionString = GetPersonalityLevel("ambition") switch
        {
            TraitLevel.HIGH => $"incredibly ambitious and independent",
            TraitLevel.MEDIUM => $"idk",
            TraitLevel.LOW => $"reserved and loyal",
            _ => "idk"
        };
        if (GetPersonalityLevel("leadership") != TraitLevel.MEDIUM)
        desc += $"{pronoun.Capitalize()} {(dead ? "was" : "is")} also {ambitionString}. ";
        return desc;
    }
    
}
public enum CharacterRole
{
    LEADER,
    HEIR,
    NOBLE,
    COMMANDER,
    POLITICIAN,
    CIVILIAN,
    FORMER_LEADER
}

public enum Gender
{
    MALE,
    FEMALE,
    ANY
}

public enum TraitLevel
{
    HIGH,
    MEDIUM,
    LOW,
}
