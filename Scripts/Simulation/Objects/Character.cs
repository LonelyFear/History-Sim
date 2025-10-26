using System;
using System.Collections.Generic;
using Godot;
using MessagePack;
[MessagePackObject(keyAsPropertyName: true)]
public class Character
{
    public Random rng = new Random();
    public static SimManager sim;
    public ulong id;
    public int significance;
    public string firstName;
    public string lastName;
    public uint birthTick;
    public uint age;
    public uint deathTick;
    public ulong? stateId = null;
    public CharacterRole role = CharacterRole.DEAD;
    public ulong? parentId = null;
    public List<ulong?> childIds = new List<ulong?>();
    public int mood = 100;
    // Health
    public int health = 100;
    float healthDecreaseChance = 0.075f;
    const float hdChanceAnnualGrowth = 0.02f;
    const int agingHealthDecrease = 3;
    // Character Skills
    public int charisma;
    // Makes characters better speakers. Increases positive outcomes in diplomacy, meetings, and provides bonuses at war.
    public int intellect;
    // Makes characters smarter. Boosts learning rate and increases chance to discover a new technology or write books.
    public int military;
    // Makes characters more strategic. Improves combat ability at war
    public int stewardship;
    // Makes characters better rulers. Increases tax income
    public int combat;
    // Makes characters better at fighting. Lowers chance of dying if leading from the front and increases chance of winning duels.

    // Character Personality
    public int sociability;
    // Makes characters more social. Increases chance of meeting and social interactions
    public int greed;
    // Makes characters strive for more wealth. Decreases loyalty
    public int ambition;
    // Makes characters more ambitious. Increases chance of war and makes characters strive for higher positions. Greedy characters will let others take the fall for their success
    public int empathy;
    // Makes characters empathetetic. Makes characters more likeable and benevolent. Raises morale but lowers combat ability at war.
    public int boldness;
    // Makes characters braver. Increases the chance of taking risky matchups in diplomacy or governance and risky strategies at war (Increases effects of crits)
    public int temperment;
    // Makes characters more cool-headed. Decreases chance of negative diplomatic outcomes and is complementary with empathy. Raises morale at war

    // Character Modifiers
    public bool dead;
    public const int dieHealthThreshold = 40;
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


