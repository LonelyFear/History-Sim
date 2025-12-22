using System;
using System.Collections.Generic;
using System.Net.Quic;
using Godot;
using MessagePack;
[MessagePackObject]
public class Character : NamedObject
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
    [Key(8)] public List<ulong> parentIds = new List<ulong>();
    [Key(9)] public List<ulong?> childIds = new List<ulong?>();
    [Key(10)] public Dictionary<ulong, int> relationsIds = new Dictionary<ulong, int>();
    // Health
    [Key(11)] public int health = 100;
    [Key(12)] public float healthDecreaseChance = 0.075f;

    // Character Skills
    [Key(31)] public Dictionary<string, int> skills = new Dictionary<string, int>
    {
        {"charisma", 50 },
        {"intellect", 50 },
        {"military", 50 },
        {"empathy", 50 },
        {"stewardship", 50 },
        {"combat", 50 }
    };
    // Character Personality
    [Key(30)]
    public Dictionary<string, int> personality = new Dictionary<string, int>
    {
        {"sociability", 50 },
        {"greed", 50 },
        {"ambition", 50 },
        {"empathy", 50 },
        {"boldness", 50 },
        {"temperment", 50 },
        {"attractiveness", 50}
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
        SetRole(CharacterRole.DEAD);
        objectManager.CreateHistoricalEvent([this], EventType.DEATH);
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
    public TraitLevel IntToTraitLevel(int trait)
    {
        TraitLevel level;

        if (trait > 90)
        {
            level = TraitLevel.EXTREMELY_HIGH;
        }
        else if (trait > 75)
        {
            level = TraitLevel.VERY_HIGH;
        }
        else if (trait > 60)
        {
            level = TraitLevel.HIGH;
        }
        else if (trait > 39)
        {
            level = TraitLevel.MEDIUM;
        }
        else if (trait > 24)
        {
            level = TraitLevel.LOW;
        }
        else if (trait > 9)
        {
            level = TraitLevel.VERY_LOW;
        }
        else
        {
            level = TraitLevel.EXTREMELY_LOW;
        }
        return level;
    }
    public override string GenerateDescription()
    {
        string[] pronouns = ["he", "she", "they"];
        int intGender = (int)gender;

        string[] toBe = ["is", "are"];
        if (dead)
        {
            toBe = ["was", "were"];
        }
        string desc = $"{name} {toBe[0]} a character born in {sim.timeManager.GetStringDate(tickCreated, true)} to ";

        if (parentIds.Count <= 0)
        {
            desc += "unknown parents";
        } else
        {
            for (int i = 0; i < parentIds.Count; i++)
            {
                Character parent = objectManager.GetCharacter(parentIds[i]);
                if (parent != null)
                {
                    desc += GenerateUrlText(parent, parent.name);
                } else
                {
                    desc += "an unknown parent";
                }
                
                if (i + 2 < parentIds.Count)
                {
                    desc += ", ";
                } else if (i + 1 < parentIds.Count)
                {
                    desc += " and ";
                }
            }         
        }

        desc += $". {pronouns[intGender].Capitalize()} {toBe[Mathf.Clamp(intGender - 1, 0, 1)]} ";
        switch (role)
        {
            case CharacterRole.LEADER:
                desc += $"the {objectManager.GetState(stateId).leaderTitle.ToLower()} of the {GenerateUrlText(objectManager.GetState(stateId), objectManager.GetState(stateId).name)}";
                break;
            case CharacterRole.HEIR:
                desc += $"the heir to the {GenerateUrlText(objectManager.GetState(stateId), objectManager.GetState(stateId).name)}";
                break;
            case CharacterRole.COMMANDER:
                desc += $"a commander in the army of the {GenerateUrlText(objectManager.GetState(stateId), objectManager.GetState(stateId).name)}";
                break;
            case CharacterRole.POLITICIAN:
                desc += $"are a politician in the {GenerateUrlText(objectManager.GetState(stateId), objectManager.GetState(stateId).name)}";
                break;
            case CharacterRole.NOBLE:
                desc += $"a noble in the {GenerateUrlText(objectManager.GetState(stateId), objectManager.GetState(stateId).name)}";
                break;
            default:
                desc += $"living in the {GenerateUrlText(objectManager.GetState(stateId), objectManager.GetState(stateId).name)}";
                break;
        }
        desc += $", and {pronouns[intGender]} {toBe[Mathf.Clamp(intGender - 1, 0, 1)]} {sim.timeManager.GetYear(sim.timeManager.ticks - tickCreated)} years old. ";
        // Personality
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
    EXTREMELY_HIGH,
    VERY_HIGH,
    HIGH,
    MEDIUM,
    LOW,
    VERY_LOW,
    EXTREMELY_LOW
}
