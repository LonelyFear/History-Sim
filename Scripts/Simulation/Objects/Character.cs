using System;
using System.Collections.Generic;
using MessagePack;
[MessagePackObject(keyAsPropertyName: true)]
public class Character
{
    public static SimManager sim;
    public ulong id;
    public int significance;
    public string firstName;
    public string lastName;
    public uint birthTick;
    public uint age;
    public uint deathTick;
    public Dictionary<ulong, CharacterRole> statesIds = new Dictionary<ulong, CharacterRole>();
    public ulong? homeState;
    public ulong? parentId = null;
    public List<ulong?> childIds = null;
    public int mood;
    public int health;
    public float intelligence;
    public float charisma;
    public float warfare;
    public float stewardship;
    public bool dead;
    public void JoinState(State state, CharacterRole role = CharacterRole.CIVILIAN)
    {
        state.characterIds.Add(id);
        statesIds.Add(state.id, role);
        SetRoleInState(state, role);
    }
    public void SetRoleInState(State state, CharacterRole role)
    {
        if (statesIds[state.id] == CharacterRole.LEADER)
        {
            state.leaderId = null;
        }

        statesIds[state.id] = role;
        if (role == CharacterRole.LEADER)
        {
            if (state.leaderId != null)
            {
                sim.charactersIds[(ulong)state.leaderId].SetRoleInState(state, CharacterRole.FORMER_LEADER);
            }
            state.leaderId = id;
        }
    }
    public void LeaveState(State state)
    {
        if (state.leaderId == id)
        {
            state.leaderId = null;
        }
        state.characterIds.Remove(id);
        statesIds.Remove(state.id);
    }
    public void SetHomeState(State state) {
        if (!statesIds.ContainsKey(state.id))
        {
            JoinState(state);
        }
        homeState = state.id;
    }
    public void Die()
    {
        dead = true;
        deathTick = sim.timeManager.ticks;
        foreach (ulong stateId in statesIds.Keys)
        {
            SetRoleInState(sim.statesIds[stateId], CharacterRole.DEAD);
        }
        //sim.DeleteCharacter(this);
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


