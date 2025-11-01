using System;
using System.Collections.Generic;
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
    [Key(3)] public uint birthTick;
    [Key(4)] public uint age;
    [Key(5)] public uint deathTick;
    [Key(6)] public ulong? stateId = null;
    [Key(7)] public CharacterRole role = CharacterRole.DEAD;
    [Key(8)] public ulong? parentId = null;
    [Key(9)] public List<ulong?> childIds = new List<ulong?>();
    [Key(10)] public int mood = 100;
    // Health
    [Key(11)] public int health = 100;
    [Key(12)] public float healthDecreaseChance = 0.075f;

    // Character Skills
    [Key(13)] public int charisma;
    [Key(14)] public int intellect;
    [Key(15)] public int military;
    [Key(16)] public int stewardship;
    [Key(17)] public int combat;

    // Character Personality
    [Key(18)] public int sociability;
    [Key(19)] public int greed;
    [Key(20)] public int ambition;
    [Key(21)] public int empathy;
    [Key(22)] public int boldness;
    [Key(23)] public int temperment;

    // Character Modifiers

    // Education

    [Key(24)] public bool dead;
    
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
    public void Die()
    {
        dead = true;
        deathTick = sim.timeManager.ticks;
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
    #region Encyclopedia
    public override string GenerateDescription()
    {
        string desc = $"{name} is a character living in the {GenerateUrlText(sim.GetState(stateId), sim.GetState(stateId).displayName)}. They ";
        if (dead)
        {
            desc = $"{name} is a character who died in {sim.timeManager.GetStringDate(deathTick, true)}";
        }
        switch (role)
        {
            case CharacterRole.LEADER:
                desc += $"are the {sim.GetState(stateId).leaderTitle.ToLower()} of the {GenerateUrlText(sim.GetState(stateId), sim.GetState(stateId).displayName)}.";
                break;
            case CharacterRole.HEIR:
                desc += $"are the heir to the {GenerateUrlText(sim.GetState(stateId), sim.GetState(stateId).displayName)}.";
                break;
            case CharacterRole.COMMANDER:
                desc += $"serve as a commander in the army of the {GenerateUrlText(sim.GetState(stateId), sim.GetState(stateId).displayName)}.";
                break;
            case CharacterRole.POLITICIAN:
                desc += $"are a politician in the {GenerateUrlText(sim.GetState(stateId), sim.GetState(stateId).displayName)}.";
                break;
            case CharacterRole.NOBLE:
                desc += $"are a noble in the {GenerateUrlText(sim.GetState(stateId), sim.GetState(stateId).displayName)}.";
                break;
            default:
                desc += $"have no roles in the {GenerateUrlText(sim.GetState(stateId), sim.GetState(stateId).displayName)}.";
                break;
        }
        desc += $" They were born in {sim.timeManager.GetStringDate(birthTick, true)}, and they are {sim.timeManager.GetYear(sim.timeManager.ticks - birthTick)} years old. ";
        return desc;
    }
    #endregion
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
