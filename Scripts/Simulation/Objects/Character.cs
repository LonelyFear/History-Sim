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
    public float intelligence;
    public float charisma;
    public float warfare;
    public float stewardship;
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


