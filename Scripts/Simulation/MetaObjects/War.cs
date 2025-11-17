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
    public void InitializeWar()
    {

    }
    public void InitWarLead(bool isAttacker)
    {
        ulong warLead = isAttacker ? primaryAgressorId : primaryDefenderId;
        ulong enemyWarLead = isAttacker ? primaryDefenderId : primaryAgressorId;

        List<ulong> allies = isAttacker ? attackerIds : defenderIds;
        List<ulong> enemies = isAttacker ? defenderIds : attackerIds;
        objectManager.GetState(warLead).diplomacy.warIds[id] = isAttacker;
        objectManager.GetState(warLead).diplomacy.AddEnemies(enemies);
        participantIds.Add(warLead);
        allies.Add(warLead);  

        objectManager.GetState(warLead).diplomacy.EstablishRelations(enemyWarLead, -1000);      
    }
    public void InitEnemies(bool isAttacker)
    {
        ulong enemyWarLead = isAttacker ? primaryDefenderId : primaryAgressorId;
        List<ulong> targets = isAttacker ? attackerIds : defenderIds;
        List<ulong> enemies = isAttacker ? defenderIds : attackerIds;
        foreach (ulong stateId in targets)
        {
            State state = objectManager.GetState(stateId);
            if (!participantIds.Contains(stateId)&& !targets.Contains(stateId))
            {
                state.diplomacy.AddEnemies(enemies);
                state.diplomacy.warIds[id] = false;
                participantIds.Add(stateId);
                state.diplomacy.EstablishRelations(enemyWarLead, -5);                
            }
        }
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
    public void AddParticipant(ulong stateId, bool attacker)
    {
        bool isInWar = false;
        RemoveParticipant(stateId);
        State state = objectManager.GetState(stateId);
        if (attacker && !attackerIds.Contains(stateId))
        {
            state.diplomacy.AddEnemies(defenderIds);
            isInWar = true;
        }
        else if (!defenderIds.Contains(stateId))
        {
            state.diplomacy.AddEnemies(attackerIds);
            isInWar = true;
        }

        if (isInWar)
        {
            state.diplomacy.warIds.Add(id, attacker);
            participantIds.Add(stateId);
        }
    }
    public void RemoveParticipant(ulong stateId)
    {
        State state = objectManager.GetState(stateId);

        // Removes from participants list
        if (participantIds.Contains(stateId))
        {
            state.diplomacy.warIds.Remove(id);
            participantIds.Remove(stateId);
        }

        // Removes enemies and sided participation
        if (attackerIds.Contains(stateId))
        {
            attackerIds.Remove(stateId);
            foreach (ulong defenderId in defenderIds)
            {
                State defender = objectManager.GetState(defenderId);
                defender.diplomacy.RemoveEnemy(stateId);
            }
        }
        else if (defenderIds.Contains(stateId))
        {
            defenderIds.Remove(stateId);
            foreach (ulong attackerId in attackerIds)
            {
                State attacker = objectManager.GetState(attackerId);
                attacker.diplomacy.RemoveEnemy(stateId);
            }
        }

        if (attackerIds.Count < 1 || defenderIds.Count < 1 || primaryAgressorId == stateId || primaryDefenderId == stateId)
        {
            objectManager.EndWar(this);
        }
    }

}