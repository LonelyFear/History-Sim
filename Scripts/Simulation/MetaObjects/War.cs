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
    public void InitWarLead(bool isAttacker)
    {
        ulong warLead = isAttacker ? primaryAgressorId : primaryDefenderId;
        ulong enemyWarLead = isAttacker ? primaryDefenderId : primaryAgressorId;

        objectManager.GetState(warLead).diplomacy.EstablishRelations(enemyWarLead, -1000);
        AddParticipant(warLead, isAttacker);   
    }
    public void NameWar()
    {
        switch (warType)
        {
            case WarType.CONQUEST:
                string[] warNames = { "War", "Conflict" };
                name = $"{objectManager.GetState(primaryAgressorId).name}-{objectManager.GetState(primaryDefenderId).name} {warNames.PickRandom()}";
                if (rng.NextSingle() < 0.25f)
                {
                    warNames = ["Invasion of"];
                    name = $"{NameGenerator.GetDemonym(objectManager.GetState(primaryAgressorId).name)} {warNames.PickRandom()} {objectManager.GetState(primaryDefenderId).name}";
                }
                break;
            case WarType.CIVIL_WAR:
                name = $"{NameGenerator.GetDemonym(objectManager.GetState(primaryDefenderId).name)} Civil War";
                break;
            case WarType.REVOLT:
                warNames = ["Revolution", "Uprising", "Rebellion", "Revolt"];
                name = $"{NameGenerator.GetDemonym(objectManager.GetState(primaryAgressorId).name)} {warNames.PickRandom()}";
                break;
        }
    }
    public void AddParticipant(ulong stateId, bool isAttacker)
    {
        State state = objectManager.GetState(stateId);
        if (participantIds.Contains(stateId))
        {
            return;
        }
        if (isAttacker)
        {
            attackerIds.Add(stateId);
        }
        else
        {
            defenderIds.Add(stateId);
        }    
        state.diplomacy.warIds.Add(id, isAttacker);
        participantIds.Add(stateId);
    }
    public void RemoveParticipant(ulong stateId)
    {
        State state = objectManager.GetState(stateId);

        // Removes from participants list
        state.diplomacy.warIds.Remove(id);
        participantIds.Remove(stateId);

        // Removes enemies and sided participation
        // Removes enemies of attacker
        if (attackerIds.Contains(stateId))
        {
            attackerIds.Remove(stateId);
            state.diplomacy.RemoveEnemies(defenderIds);
            foreach (ulong defenderId in defenderIds)
            {
                State defender = objectManager.GetState(defenderId);
                defender.diplomacy.RemoveEnemy(stateId);
            }
        }
        // Removes enemies of defender
        else if (defenderIds.Contains(stateId))
        {
            defenderIds.Remove(stateId);
            state.diplomacy.RemoveEnemies(attackerIds);
            foreach (ulong attackerId in attackerIds)
            {
                State attacker = objectManager.GetState(attackerId);
                attacker.diplomacy.RemoveEnemy(stateId);
            }
        }
        bool warEndConditions = attackerIds.Count < 1 || defenderIds.Count < 1 || primaryAgressorId == stateId || primaryDefenderId == stateId;
        if ( warEndConditions && !dead)
        {
            objectManager.EndWar(this);
        }
    }
    
}