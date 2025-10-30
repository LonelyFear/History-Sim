using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;
using MessagePack.Formatters;
[MessagePackObject]
public class War : NamedObject
{
    [Key(0)] public List<ulong> attackerIds { get; set; } = new List<ulong>();
    [Key(1)] public List<ulong> defenderIds { get; set; } = new List<ulong>();
    [Key(2)] public List<ulong> participantIds = new List<ulong>();
    [Key(3)] public ulong primaryAgressorId;
    [Key(4)] public ulong primaryDefenderId;
    [Key(5)] public WarType warType { get; set; } = WarType.CONQUEST;
    [IgnoreMember] static Random rng = new Random();
    [IgnoreMember] public static SimManager simManager;
    [Key(6)] public uint tickStarted;
    [Key(7)] public uint age;
    [Key(8)] public uint tickEnded;
    public War(){}
    public War(List<State> atk, List<State> def, WarType warType, ulong agressorLeader, ulong defenderLeader)
    {
        if (agressorLeader == defenderLeader || simManager.GetState(agressorLeader) == null || simManager.GetState(defenderLeader) == null)
        {
            return;
        }
        id = simManager.getID();
        foreach (State state in atk)
        {
            if (simManager.GetState(state.id) != null)
            {
                attackerIds.Add(state.id);
            }
        }
        foreach (State state in def)
        {
            if (simManager.GetState(state.id) != null)
            {
                defenderIds.Add(state.id);
            }
        }
        this.warType = warType;        
        primaryAgressorId = agressorLeader;
        primaryDefenderId = defenderLeader;

        simManager.GetState(primaryAgressorId).wars[this] = true;
        simManager.GetState(primaryAgressorId).enemyIds.AddRange(attackerIds);
        participantIds.Add(primaryAgressorId);
        attackerIds.Add(primaryAgressorId);

        simManager.GetState(primaryDefenderId).wars[this] = false;
        simManager.GetState(primaryDefenderId).enemyIds.AddRange(attackerIds);
        participantIds.Add(primaryDefenderId);
        defenderIds.Add(primaryDefenderId);

        simManager.GetState(primaryAgressorId).EstablishRelations(simManager.GetState(primaryDefenderId), -5);
        simManager.GetState(primaryDefenderId).EstablishRelations(simManager.GetState(primaryAgressorId), -5);

        foreach (ulong stateId in attackerIds)
        {
            State state = simManager.GetState(stateId);
            if (!participantIds.Contains(stateId) && !attackerIds.Contains(stateId))
            {
                state.enemyIds.AddRange(defenderIds);
                state.wars[this] = true;
                participantIds.Add(stateId);
                state.EstablishRelations(simManager.GetState(primaryDefenderId), -5);                
            }
        }
        foreach (ulong stateId in defenderIds)
        {
            State state = simManager.GetState(stateId);
            if (!participantIds.Contains(stateId)&& !defenderIds.Contains(stateId))
            {
                state.enemyIds.AddRange(attackerIds);
                state.wars[this] = false;
                participantIds.Add(stateId);
                state.EstablishRelations(simManager.GetState(primaryAgressorId), -5);                
            }
        }
        switch (warType)
        {
            case WarType.CONQUEST:
                string[] warNames = { "War", "Conflict" };
                name = $"{simManager.GetState(agressorLeader).name}-{simManager.GetState(defenderLeader).name} {warNames.PickRandom()}";
                if (rng.NextSingle() < 0.25f)
                {
                    warNames = ["Invasion of"];
                    name = $"{NameGenerator.GetDemonym(simManager.GetState(agressorLeader).name)} {warNames.PickRandom()} {simManager.GetState(defenderLeader).name}";
                }
                break;
            case WarType.CIVIL_WAR:
                name = $"{NameGenerator.GetDemonym(simManager.GetState(defenderLeader).name)} Civil War";
                break;
            case WarType.REVOLT:
                warNames = ["Revolution", "Uprising", "Rebellion", "Revolt"];
                name = $"{NameGenerator.GetDemonym(simManager.GetState(agressorLeader).name)} {warNames.PickRandom()}";
                break;
        }     
        simManager.wars.Add(this);
        simManager.warIds.Add(id, this);   
    }
    public void AddParticipant(ulong stateId, bool attacker)
    {
        bool isInWar = false;
        RemoveParticipant(stateId);
        State state = simManager.GetState(stateId);
        if (attacker && !attackerIds.Contains(stateId))
        {
            state.enemyIds.AddRange(attackerIds);
            isInWar = true;
        }
        else if (!defenderIds.Contains(stateId))
        {
            state.enemyIds.AddRange(attackerIds);
            isInWar = true;
        }
        if (isInWar)
        {
            state.wars.Add(this, attacker);
            participantIds.Add(stateId);
        }
    }
    public void RemoveParticipant(ulong stateId)
    {
        State state = simManager.GetState(stateId);
        if (participantIds.Contains(stateId))
        {
            state.wars.Remove(this);
            participantIds.Remove(stateId);
        }
        if (attackerIds.Contains(stateId))
        {
            attackerIds.Remove(stateId);
        }
        if (defenderIds.Contains(stateId))
        {
            defenderIds.Remove(stateId);
        }
        if (attackerIds.Count < 1 || defenderIds.Count < 1 || primaryAgressorId == stateId || primaryDefenderId == stateId)
        {
            EndWar();
        }
    }
    public void EndWar()
    {
        simManager.wars.Remove(this);
        simManager.warIds.Remove(id);
        foreach (ulong stateId in participantIds)
        {
            State state = simManager.GetState(stateId);
            state.wars.Remove(this);
        }
    }
}