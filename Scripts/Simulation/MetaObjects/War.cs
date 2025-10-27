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
    public List<ulong> attackerIds { get; set; } = new List<ulong>();
    public List<ulong> defenderIds { get; set; } = new List<ulong>();
    public List<ulong> participantIds = new List<ulong>();
    public ulong primaryAgressorId;
    public ulong primaryDefenderId;
    public WarType warType { get; set; } = WarType.CONQUEST;
    [IgnoreMember] static Random rng = new Random();
    [IgnoreMember] public static SimManager simManager;
    public uint tickStarted;
    public uint age;
    public uint tickEnded;
    public ulong id;
    public void PrepareForSave()
    {
        //defendersIds = defenders.Select(s => s.id).ToList();
        //attackerIds = attackers.Select(s => s.id).ToList();
        //primaryAgressorId = primaryAgressor.id;
        //primaryDefenderID = primaryDefender.id;
    }
    public void LoadFromSave()
    {
        //defenders = defendersIds.Select(d => simManager.statesIds[d]).ToList();
        //attackers = attackerIds.Select(a => simManager.statesIds[a]).ToList();
        //primaryAgressor = simManager.statesIds[primaryAgressorId];
        //primaryDefender = simManager.statesIds[primaryDefenderId];
        //participants = participants = [.. defenders, .. attackers];
    }
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
        simManager.wars.Add(this);
        switch (warType)
        {
            case WarType.CONQUEST:
                string[] warNames = { "War", "Conflict" };
                warName = $"{simManager.GetState(agressorLeader).name}-{simManager.GetState(defenderLeader).name} {warNames.PickRandom()}";
                if (rng.NextSingle() < 0.25f)
                {
                    warNames = ["Invasion of"];
                    warName = $"{NameGenerator.GetDemonym(simManager.GetState(agressorLeader).name)} {warNames.PickRandom()} {simManager.GetState(defenderLeader).name}";
                }
                break;
            case WarType.CIVIL_WAR:
                warName = $"{NameGenerator.GetDemonym(simManager.GetState(defenderLeader).name)} Civil War";
                break;
            case WarType.REVOLT:
                warNames = ["Revolution", "Uprising", "Rebellion", "Revolt"];
                warName = $"{NameGenerator.GetDemonym(simManager.GetState(agressorLeader).name)} {warNames.PickRandom()}";
                break;
        }        
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
        foreach (ulong stateId in participantIds)
        {
            State state = simManager.GetState(stateId);
            state.wars.Remove(this);
        }
    }
}