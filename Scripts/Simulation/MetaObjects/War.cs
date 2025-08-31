using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;
using MessagePack.Formatters;
[MessagePackObject(keyAsPropertyName: true, AllowPrivate = true)]
public class War
{
    public string warName { get; set; } = "War";
    [IgnoreMember]
    public List<State> attackers { get; set; } = new List<State>();
    public List<ulong> attackerIds { get; set; }
    [IgnoreMember]
    public List<State> defenders { get; set; } = new List<State>();
    public List<ulong> defendersIds { get; set; }
    [IgnoreMember]
    public List<State> participants { get; set; } = new List<State>();
    [IgnoreMember] public State primaryAgressor;
    public ulong primaryAgressorID;
    [IgnoreMember] public State primaryDefender;
    public ulong primaryDefenderID;
    public WarType warType { get; set; } = WarType.CONQUEST;
    [IgnoreMember]
    static Random rng = new Random();
    [IgnoreMember]
    public static SimManager simManager;
    public uint tickStarted;
    public uint age;
    public uint tickEnded;
    public ulong id;
    public void PrepareForSave()
    {
        defendersIds = defenders.Select(s => s.id).ToList();
        attackerIds = attackers.Select(s => s.id).ToList();
        primaryAgressorID = primaryAgressor.id;
        primaryDefenderID = primaryDefender.id;
    }
    public void LoadFromSave()
    {
        defenders = defendersIds.Select(d => simManager.statesIds[d]).ToList();
        attackers = attackerIds.Select(a => simManager.statesIds[a]).ToList();
        primaryAgressor = simManager.statesIds[primaryAgressorID];
        primaryDefender = simManager.statesIds[primaryDefenderID];
        participants = (List<State>)defenders.Concat(attackers);
    }
    public War(){}
    public War(List<State> atk, List<State> def, WarType warType, State agressorLeader, State defenderLeader)
    {
        id = simManager.getID();
        attackers = atk;
        defenders = def;
        this.warType = warType;
        primaryAgressor = agressorLeader;
        primaryDefender = defenderLeader;
        foreach (State state in attackers)
        {
            state.enemies.AddRange(defenders);
            state.wars.Add(this, true);
            participants.Add(state);
            state.EstablishRelations(defenderLeader, -5);
        }
        foreach (State state in defenders)
        {
            state.enemies.AddRange(attackers);
            state.wars.Add(this, false);
            participants.Add(state);
            state.EstablishRelations(agressorLeader, -5);
        }
        simManager.wars.Add(this);

        switch (warType)
        {
            case WarType.CONQUEST:
                string[] warNames = { "War", "Conflict" };
                warName = $"{agressorLeader.name}-{defenderLeader.name} {warNames.PickRandom()}";
                if (rng.NextSingle() < 0.25f)
                {
                    warNames = ["Invasion of"];
                    warName = $"{NameGenerator.GetDemonym(agressorLeader.name)} {warNames.PickRandom()} {defenderLeader.name}";
                }
                break;
            case WarType.CIVIL_WAR:
                warName = $"{NameGenerator.GetDemonym(defenderLeader.name)} Civil War";
                break;
            case WarType.REVOLT:
                warNames = ["Revolution", "Uprising", "Rebellion", "Revolt"];
                warName = $"{NameGenerator.GetDemonym(agressorLeader.name)} {warNames.PickRandom()}";
                break;
        }        
    }
    public void AddParticipants(List<State> states, bool attacker)
    {
        foreach (State state in states)
        {
            bool isInWar = false;

            if (attacker && !attackers.Contains(state))
            {
                state.enemies.AddRange(attackers);
                isInWar = true;
            }
            else if (!defenders.Contains(state))
            {
                state.enemies.AddRange(attackers);
                isInWar = true;
            }
            if (isInWar)
            {
                state.wars.Add(this, attacker);
                participants.Add(state);
            }
        }
    }
    public void RemoveParticipants(List<State> states)
    {
        foreach (State state in states)
        {
            bool isInWar = false;
            if (participants.Contains(state))
            {
                isInWar = true;
            }
            if (isInWar)
            {
                state.wars.Remove(this);
                participants.Remove(state);
            }
        }
        if (attackers.Count < 1 || defenders.Count < 1)
        {
            EndWar();
        }
    }
    public void EndWar()
    {
        simManager.wars.Remove(this);
        foreach (State state in attackers.Concat(defenders))
        {
            state.wars.Remove(this);
        }
    }
}