using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using Godot;
using MessagePack;

[MessagePackObject(AllowPrivate = true)]
public partial class StateDiplomacyManager
{
    [IgnoreMember] public static ObjectManager objectManager;
    // Diplomacy
    [Key(19)] public Dictionary<ulong?, Relation> relationIds { get; set; } = [];
    [Key(20)] public Dictionary<ulong, War.WarSide> warIds { get; set; } = [];
    [Key(0)] public ulong stateId;
    [Key(1)] public ulong? liegeId {get; private set; } = null;
    //[IgnoreMember] ulong realmId;
    [Key(12)] public List<ulong> allianceIds = new List<ulong>();
    [Key(17)] public List<ulong?> vassalIds { get; set; } = new List<ulong?>();
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

    // Wars
    public void DeclareWar(State target, WarType type = WarType.CONQUEST)
    {
        //GD.Print($"Trying to start a war between {state.name} and {target.name}");
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
        State liege = state.diplomacy.GetLiege();
        
        foreach (War war in liege.diplomacy.warIds.Keys.Select(id => objectManager.GetWar(id)))
        {
            //war.AddParticipant(state, liege.diplomacy.warIds[war.id]);
        }  
    } 
    // Enemy Utility
    public void SetEnemy(ulong targetId, bool isEnemy)
    {
        SetEnemy(objectManager.GetState(targetId), isEnemy);
    }
    public void SetEnemy(State target, bool isEnemy)
    {
        GetMutualRelations(target, out Relation usThem, out Relation themUs);

        usThem.enemy = isEnemy;
        themUs.enemy = isEnemy; 
    }
    public void SetEnemies(IEnumerable<ulong> stateIds, bool isEnemy)
    {
        foreach (ulong stateId in stateIds)
        {
            SetEnemy(stateId, isEnemy);     
        }
    }

    // Relations
    public void UpdateRelations()
    {
        // All bordering or enemy states
        List<State> relationStates = [.. state.borderingStateIds.Select(pair => objectManager.GetState(pair.Key))];

        // Removes unneeded relations
        foreach (var pair in relationIds)
        {
            State target = objectManager.GetState(pair.Key);
            if (target == null || (!IsEnemyWithState(target) && !state.borderingStateIds.ContainsKey(target.id)))
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
        State target = objectManager.GetState(targetId);
        if (target == null) return;

        if (relationIds.Remove(targetId))
        {
            target.diplomacy.RemoveRelations(state.id);
        }     
    }
    public Relation EstablishOrGetRelations(State target)
    {
        Relation relations = GetRelationsWithState(target);
        if (relations == null)
        {
            return EstablishRelations(target.id);
        }
        return relations;
    }
    public Relation EstablishRelations(ulong? targetId)
    {
        State target = objectManager.GetState(targetId);
        if (target == null) return null;

        if (relationIds.TryAdd(targetId, new Relation()))
        {
            if (state.borderingStateIds.TryGetValue((ulong)targetId, out int borderLength))
            {
                relationIds[targetId].borderLength = borderLength;
            }
            target.diplomacy.EstablishRelations(state.id);
        }
        return relationIds[targetId];
    }

    // Relations Utilities
    public void ChangeOpinion(State target, float value)
    {
        GetMutualRelations(target, out Relation usThem, out Relation themUs);

        usThem.opinion = Math.Clamp(usThem.opinion + value, 0, 1);
        themUs.opinion = Math.Clamp(themUs.opinion + value, 0, 1); 
    }
    public void SetRivalry(State target, bool value)
    {
        GetMutualRelations(target, out Relation usThem, out Relation themUs);
        usThem.rival = value;
        themUs.rival = value;      
    }
    public void GetMutualRelations(State target, out Relation usThem, out Relation themUs)
    {
        usThem = EstablishOrGetRelations(target);
        themUs = target.diplomacy.EstablishOrGetRelations(state);
    }    
    // Check utilities
    public bool CanFightState(State target)
    {
        return state.sovereignty == Sovereignty.INDEPENDENT && target.sovereignty == Sovereignty.INDEPENDENT 
        && !IsAlliedToState(target) && GetRelationsWithState(target).truce < 1
        && !IsEnemyWithState(target);
    }
    public bool IsEnemyWithState(State otherState)
    {
        if (relationIds.TryGetValue(otherState.id, out Relation relation))
        {
            return relation.enemy;
        }
        return false;
    }
    public bool IsAlliedToState(State otherState)
    {
        foreach (ulong allianceId in allianceIds)
        {
            Alliance alliance = objectManager.GetAlliance(allianceId);
            
            if (alliance.HasMember(otherState))
            {
                return true;
            }
        }
        return false;
    }
    public Relation GetRelationsWithState(State s)
    {
        if (relationIds.TryGetValue(s.id, out Relation relation))
        {
            return relation;
        }
        return null;          
    }
    // Alliance
    public Alliance GetRealm()
    {
        foreach (ulong allianceId in allianceIds)
        {
            Alliance potentialRealm = objectManager.GetAlliance(allianceId);
            if (potentialRealm.type == OrgType.REALM)
            {
                return potentialRealm;
            }
        }
        return null;
    }
    // Vassalage
    public void UpdateRealm()
    {
        if (state.sovereignty == Sovereignty.INDEPENDENT && GetRealm() == null && vassalIds.Count > 0){
            objectManager.CreateAlliance(state, OrgType.REALM);
        }
        foreach (ulong? vassalId in vassalIds)
        {
            State vassal = objectManager.GetState(vassalId);
            GetRealm().AddMember(vassal);
        }        
    }
    public void AddVassal(State vassal, Sovereignty sovereignty)
    {
        if (sovereignty == Sovereignty.INDEPENDENT || vassalIds.Contains(vassal.id)) return;
        State vassalFormerLiege = vassal.diplomacy.GetLiege();

        vassalFormerLiege?.diplomacy.RemoveVassal(vassal);

        vassal.sovereignty = sovereignty;
        vassal.diplomacy.liegeId = state.id;
        vassalIds.Add(stateId);

        UpdateRealm();
        GetRealm().AddMember(vassal);
    }
    public void RemoveVassal(State vassal)
    {
        if (!vassalIds.Remove(stateId)) return;

        vassal.sovereignty = Sovereignty.INDEPENDENT;
        vassal.diplomacy.liegeId = null;

        GetRealm().RemoveMember(vassal);
        vassal.diplomacy.UpdateRealm();
    }
    public void RemoveAllVassals()
    {
        foreach (ulong? vassalId in vassalIds.ToArray())
        {
            State vassal = objectManager.GetState(vassalId);
            RemoveVassal(vassal);
        }
    }
    public State GetLiege()
    {
        return objectManager.GetState(liegeId);
    }
    public State GetOverlord()
    {
        if (GetRealm() != null)
        {
            return GetRealm().GetAllianceLeader();
        }
        return state;
    }
}