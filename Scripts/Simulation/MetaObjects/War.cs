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
    public void NameWar()
    {
        switch (warType)
        {
            case WarType.CONQUEST:
                string[] warNames = { "War", "Conflict" };
                name = $"{objectManager.GetState(primaryAgressorId).baseName}-{objectManager.GetState(primaryDefenderId).baseName} {warNames.PickRandom()}";
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
    public void AddParticipant(State state, bool isAttacker)
    {
        if (dead) return;
        if (participantIds.Contains(state.id))
        {
            return;
        }

        if (isAttacker)
        {
            attackerIds.Add(state.id);
            state.diplomacy.AddEnemies(defenderIds);

            foreach (ulong defenderId in defenderIds)
            {
                State defender = objectManager.GetState(defenderId);
                defender.diplomacy.AddEnemy(state.id);
            }
        }
        else
        {
            defenderIds.Add(state.id);
            state.diplomacy.AddEnemies(attackerIds);

            foreach (ulong attackerId in attackerIds)
            {
                State attacker = objectManager.GetState(attackerId);
                attacker.diplomacy.AddEnemy(state.id);
            }
        }    
        state.diplomacy.warIds.Add(id, isAttacker);
        participantIds.Add(state.id);
    }
    public void RemoveParticipant(State state)
    {
        if (dead) return;

        // Removes from participants list
        state.diplomacy.warIds.Remove(id);
        participantIds.Remove(state.id);

        // Removes enemies and sided participation
        // Removes enemies of attacker
        if (attackerIds.Remove(state.id))
        {
            state.diplomacy.RemoveEnemies(defenderIds);
            foreach (ulong defenderId in defenderIds)
            {
                State defender = objectManager.GetState(defenderId);
                defender.diplomacy.RemoveEnemy(state.id);
            }
        }
        // Removes enemies of defender
        else if (defenderIds.Remove(state.id))
        {
            state.diplomacy.RemoveEnemies(attackerIds);
            foreach (ulong attackerId in attackerIds)
            {
                State attacker = objectManager.GetState(attackerId);
                attacker.diplomacy.RemoveEnemy(state.id);
            }
        }

        bool warEndConditions = attackerIds.Count < 1 || defenderIds.Count < 1 || primaryAgressorId == state.id || primaryDefenderId == state.id;
        if (warEndConditions)
        {
            objectManager.EndWar(this);
        }
    }
    
}