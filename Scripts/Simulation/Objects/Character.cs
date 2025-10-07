using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.NetworkInformation;
using Godot;
using MessagePack;
[MessagePackObject(keyAsPropertyName: true)]
public class Character
{
    public static SimManager sim;
    public ulong id;
    public string firstName;
    public string lastName;
    public int mood;
    public int health;
    public ulong age;
    public float intelligence;
    public float charisma;
    public float warfare;
    public float stewardship;
    public ulong? parentId = null;
    public List<ulong?> childIds = null;
    public Dictionary<ulong, CharacterRole> statesIds = new Dictionary<ulong, CharacterRole>();
    ulong? homeState;
    public void JoinState(State state, CharacterRole role = CharacterRole.CIVILIAN)
    {
        state.characterIds.Add(id);
        statesIds.Add(state.id, role);
    }
    public void SetRoleInState(State state, CharacterRole role)
    {
        statesIds[state.id] = role;
        if (role == CharacterRole.LEADER)
        {
            sim.charactersIds[state.leaderId].SetRoleInState(state, CharacterRole.DEMOTED_LEADER);
            state.leaderId = id;
        }
    }
    public void LeaveState(State state)
    {
        state.characterIds.Remove(id);
        statesIds.Remove(state.id);
    }
    public void SetHomeState(State state) {
        if (!statesIds.ContainsKey(state.id))
        {
            JoinState(state)
        }
        homeState = state.id;
    }
    public void Die()
    {
        sim.DeleteCharacter()
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
    DEMOTED_LEADER,
    DEAD,
}


