using System;
using System.Collections.Generic;
using Godot;
using MessagePack;
[MessagePackObject(keyAsPropertyName: true)]
public class Character
{
    public static Curve deathChanceCurve = GD.Load<Curve>("res://Curves/Simulation/DeathChanceCurve.tres");
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
    public int health = 100;
    // Character Personality

    public int greed;
    // Makes characters strive for more wealth. Decreases loyalty
    public int charisma;
    // Makes characters more likeable and sociable. Increases stability and raises morale at war.
    public int intellect;
    // Makes characters more tactical. Increases stability, income, and military ability. Lets characters have some forethought.
    public int ambition;
    // Makes characters more ambitious. Increases chance of war and makes characters strive for higher positions. Greedy characters will let others take the fall for their success
    public int empathy;
    // Makes characters empathetetic. Makes characters more likeable and benevolent. Raises morale but lowers combat ability at war.
    public int boldness;
    // Makes characters more risky. Increases the chance of taking risky matchups in diplomacy or governance and risky strategies at war
    public int temperment;
    // Makes characters more cool-headed. Decreases chance of negative diplomatic outcomes and is complementary with empathy. Raises morale at war
    public bool dead;
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
    public float GetDeathChance()
    {
        float chance = 0.01f;
        chance = deathChanceCurve.Sample(sim.timeManager.GetYear(age));
        return chance;
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


