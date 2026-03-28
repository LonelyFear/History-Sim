using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Godot;
using MessagePack;

[MessagePackObject(AllowPrivate = true)]
public partial class StateDiplomacyManager
{
    [IgnoreMember] public static ObjectManager objectManager;
    [Key(37)] public List<ulong> allianceIds = [];    
    // Diplomacy
    [Key(19)] public Dictionary<ulong?, Relation> relationIds { get; set; } = [];
    [Key(20)] public Dictionary<ulong, bool> warIds { get; set; } = [];
    [Key(21)] public HashSet<ulong> enemyIds { get; private set; } = [];
    [Key(0)] public ulong stateId;
    [IgnoreMember] State state;
    [IgnoreMember] public Random rng = PopObject.rng;

    // Constants
    [IgnoreMember] const float threatAdjustmentRate = 0.001f; // Rate of threat adjustment for lerping threat
    public StateDiplomacyManager(){}
    public StateDiplomacyManager(State selectedState)
    {
        selectedState.diplomacy = this;
        stateId = selectedState.id;
        state = selectedState;
    }
    public void Init(State state)
    {
        this.state = state;
    }
    /*
    public void UpdateEnemies()
    {
        // Adds enemies from wars
        foreach (var pair in warIds)
        {
            War war = objectManager.GetWar(pair.Key);
            bool isAttacker = pair.Value;

            List<ulong> otherSideIds = isAttacker ? war.defenderIds : war.attackerIds;
            if (otherSideIds.Contains(stateId))
            {
                GD.Print("Us: " + state.name);
                GD.Print("Liege: " + objectManager.GetState(state.vassalManager.liegeId).name);
                GD.Print("War Side: " + (isAttacker ? "Attacker" : "Defender"));
                GD.Print("Attacker: " + objectManager.GetState(war.primaryAgressorId).name);
                GD.Print("Defender: " + objectManager.GetState(war.primaryDefenderId).name);                     
            }
       
            AddEnemies(otherSideIds);
        }

        // Updates enemy ids
        HashSet<ulong> newEnemies = [];
        foreach (var pair in relationIds)
        {
            ulong? potentialEnemyId = pair.Key;
            Relation relation = pair.Value;
            if (relation.enemy) newEnemies.Add((ulong)potentialEnemyId);
        }
        enemyIds = newEnemies;
    }
    */

    // Wars
    public void DeclareWar(State target, WarType type = WarType.CONQUEST)
    {
        GD.Print($"Trying to start a war between {state.name} and {target.name}");
        objectManager.StartWar([state], [target], type, state, target);
    }
    public void LeaveWarsWithState(State target)
    {
        if (!relationIds[target.id].enemy) return;

        foreach (ulong warId in warIds.Keys.ToArray())
        {
            War war = objectManager.GetWar(warId);
            if (war.participantIds.Contains(target.id)) war.RemoveParticipant(state);
        }        
    }
    public void LeaveAllWars()
    {
        foreach (ulong warId in warIds.Keys.ToArray())
        {
            War war = objectManager.GetWar(warId);
            war.RemoveParticipant(state);
        }
    }
    public void JoinLiegeWars()
    {
        State liege = state.vassalManager.GetLiege();
        
        foreach (War war in liege.diplomacy.warIds.Keys.Select(id => objectManager.GetWar(id)))
        {
            war.AddParticipant(state, liege.diplomacy.warIds[war.id]);
        }  
    } 
    // Enemy Utility
    public void AddEnemy(ulong stateId)
    {
        EstablishRelations(stateId);
        Relation relation = relationIds[stateId];
        relation.enemy = true;
    }
    public void AddEnemies(IEnumerable<ulong> stateIds)
    {
        foreach (ulong stateId in stateIds)
        {
            AddEnemy(stateId);     
        }
    }
    public void RemoveEnemy(ulong stateId)
    {
        EstablishRelations(stateId);
        Relation relation = relationIds[stateId];
        relation.enemy = false;
    }
    public void RemoveEnemies(IEnumerable<ulong> stateIds)
    {
        foreach (ulong stateId in stateIds)
        {
            RemoveEnemy(stateId);     
        }
    }

    // Relations
    public void UpdateRelations()
    {
        // All bordering or enemy states
        List<State> relationStates = [.. state.borderingStateIds.Select(pair => objectManager.GetState(pair.Key)), .. enemyIds.Select(id => objectManager.GetState(id))];
        // Removes unneeded relations
        foreach (var pair in relationIds)
        {
            State target = objectManager.GetState(pair.Key);
            if (target == null/*|| !relationStates.Contains(target)*/)
            {
                RemoveRelations(pair.Key);
                continue;
            }
        }
        // Establishes relations
        foreach (State target in relationStates)
        {
            if (!relationIds.ContainsKey(target.id) && target != null && target.id != stateId)
            {
                EstablishRelations(target.id);
            } 
        }  
        CalculateThreats();
    }
    void CalculateThreats(){
        foreach (var pair in relationIds){
            Relation relation = pair.Value;
            State relationState = objectManager.GetState(pair.Key);

            if (relationState == null) continue;

            if (state.borderingStateIds.TryGetValue(relationState.id, out int borderLength))
            {
                relationIds[relationState.id].borderLength = borderLength;
            } else
            {
                relationIds[relationState.id].borderLength = 0;
            }          

            
            float newThreat = 0.5f;

            // Calc Percieved Threat (0 to 1)
            int stateSize = state.GetSize(true);
            float relativeSizeRatio = (stateSize - relationState.GetSize(true))/(float)stateSize;

            newThreat += relativeSizeRatio * 0.5f;

            if (relationState.sovereignty != Sovereignty.INDEPENDENT)
            {
                newThreat *= 0.5f;
            }
            
            // Moves threat for realistic adjustment
            // Eg: If we feared a nation for a while then we wont just immediatly like them when they fall 
            relation.threat = Mathf.MoveToward(relation.threat, Mathf.Clamp(newThreat, 0, 1), threatAdjustmentRate);
        }
    }
    public void RemoveRelations(ulong? targetId)
    {
        if (relationIds.ContainsKey(targetId))
        {
            relationIds.Remove(targetId);
        }
    }
    public void EstablishRelations(ulong? targetId, float opinion = 0.5f)
    {
        if (relationIds.TryAdd(targetId, new Relation(opinion, false, false)))
        {
            if (state.borderingStateIds.TryGetValue((ulong)targetId, out int borderLength))
            {
                relationIds[targetId].borderLength = borderLength;
            }
        }
    }
    // Relations Utilities
    public void DeclareRivalry(State target)
    {
        EstablishRelations(target.id);
        GetRelationsWithState(target).rival = true;

        target.diplomacy.EstablishRelations(state.id);
        target.diplomacy.GetRelationsWithState(state).rival = true;        
    }

    public void EndRivalry(State target)
    {
        EstablishRelations(target.id);
        GetRelationsWithState(target).rival = true;

        target.diplomacy.EstablishRelations(state.id);
        target.diplomacy.GetRelationsWithState(state).rival = true;        
    }
    // Check utilities
    public bool IsAtWarWithState(State otherState)
    {
        return relationIds[otherState.id].enemy;
    }
    public bool IsAlliedToState(State otherState)
    {
        foreach (ulong allianceId in allianceIds)
        {
            Alliance alliance = objectManager.GetAlliance(allianceId);
            
            if (alliance.type != AllianceType.CUSTOMS_UNION && alliance.memberStateIds.Contains(otherState.id))
            {
                return true;
            }
        }
        return false;
    }
    public Relation GetRelationsWithState(State s)
    {
        try
        {
            Relation relation = null;
            relationIds.TryGetValue(s.id, out relation);
            return relation;            
        } catch
        {
            return null;
        }
    }
}