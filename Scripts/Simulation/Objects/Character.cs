using System;
using System.Collections.Generic;
using System.Net.Quic;
using Godot;
using MessagePack;
[MessagePackObject]
public partial class Character : NamedObject
{
    // Constants
    [IgnoreMember] const float hdChanceAnnualGrowth = 0.02f;
    [IgnoreMember] const int agingHealthDecrease = 3; 
    [IgnoreMember] const int educationMinAge = 5;
    [IgnoreMember] const int educationMaxAge = 18;
    [IgnoreMember] public const int dieHealthThreshold = 40; 
    
    // Ignored Members   
    [IgnoreMember] public Random rng = new Random();
    [IgnoreMember] public static SimManager sim;

    // Character Stats
    [Key(-1)] public int significance;
    [Key(-2)] public string firstName;
    [Key(-3)] public string lastName;
    [Key(6)] public ulong? stateId = null;
    [Key(7)] public CharacterRole role = CharacterRole.DEAD;
    [Key(8)] public List<ulong> parentIds = [];
    [Key(9)] public List<ulong?> childIds = [];
    [Key(10)] public Dictionary<ulong, int> relationsIds = [];
    // Health
    [Key(11)] public int health = 100;
    [Key(12)] public float healthDecreaseChance = 0.075f;

    // Character Skills
    // Skills provide buffs/debuffs
    // Some skills like charisma and intellect can interact like personality
    /*
    [Key(31)] public Dictionary<string, float> skills {get; private set;} = new Dictionary<string, float>
    {
        {"charisma", 50 },
        {"intellect", 50 },
        {"military", 50 },
        {"empathy", 50 },
        {"stewardship", 50 },
        {"combat", 50 }
    };
    */

    // Character Personality
    // Personality changes interaction/actions
    [Key(30)]
    public Dictionary<string, float> personality {get; private set;} = new Dictionary<string, float>
    {
        {"agression", 0.5f},
    };
    [Key(40)] public Gender gender = Gender.MALE;
    
    public void JoinState(ulong stateJoinId)
    {
        State state = sim.statesIds[stateJoinId];
        if (stateId != null)
        {
            LeaveState();
        }
        stateId = stateJoinId;
        state.characterIds.Add(id);
    }
    public void LeaveState()
    {
        if (stateId == null) return;
        State state = sim.statesIds[(ulong)stateId];
        if (state.leaderId == id)
        {
            state.RemoveLeader();
        }
        state.characterIds.Remove(id);
        stateId = null;
    }    
    public void SetRole(CharacterRole newRole)
    {
        try
        {
            // Makes sure we are changing role
            if (role == newRole)
            {
               return; 
            } 

            // Changing leadership
            if (stateId != null)
            {
                State state = sim.statesIds[(ulong)stateId];
                // Removes the leader of the state if the leader is changing roles
                if (state.leaderId == id)
                {
                    state.RemoveLeader();
                }         
                // Makes character the leader if that is their new role
                if (newRole == CharacterRole.LEADER)
                {
                    state.SetLeader(id);
                }   
            }
            role = newRole;            
        } catch (Exception e)
        {
            GD.Print(e);
        }

    }
    public override void Die()
    {
        dead = true;
        tickDestroyed = sim.timeManager.ticks;
        objectManager.CreateHistoricalEvent([this, role == CharacterRole.LEADER ? objectManager.GetState(stateId) : null], EventType.DEATH);
        SetRole(CharacterRole.DEAD);
        //sim.DeleteCharacter(this);
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
            CharacterRole.LEADER => $"{w} the {objectManager.GetState(stateId).leaderTitle.ToLower()} of the {GenerateUrlText(objectManager.GetState(stateId), objectManager.GetState(stateId).name)}",
            CharacterRole.HEIR => $"{w} the heir to the {GenerateUrlText(objectManager.GetState(stateId), objectManager.GetState(stateId).name)}",
            CharacterRole.COMMANDER => $"{w} a general in the army of the {GenerateUrlText(objectManager.GetState(stateId), objectManager.GetState(stateId).name)}",
            CharacterRole.POLITICIAN => $"{w} a politician in the {GenerateUrlText(objectManager.GetState(stateId), objectManager.GetState(stateId).name)}",
            CharacterRole.NOBLE => $"{w} a noble in the {GenerateUrlText(objectManager.GetState(stateId), objectManager.GetState(stateId).name)}",
            _ => $"{(dead ? "lived" : "is living")} in the {GenerateUrlText(objectManager.GetState(stateId), objectManager.GetState(stateId).name)}",
        };

        desc += $", and {pronoun} {w} {sim.timeManager.GetYear(GetAge())} years old" 
        + (dead ? $" when {pronoun} died. " : ". ");
        // Personality
        string agressionString = GetPersonalityLevel("agression") switch
        {
            TraitLevel.HIGH => "an Agressive stance, seeking to start conflict and expand. ",
            TraitLevel.MEDIUM => "a Neutral stance, wanting to preserve power. ",
            TraitLevel.LOW => "a Passive stance, wanting to avoid conflict and make allies. ",
            _ => "an Arbitrary stance, making desicions on a whim. "
        };
        desc += $"{pronoun.Capitalize()} {(dead ? "had" : "has")} {agressionString}"; 
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
    FORMER_LEADER,
    DEAD,
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
