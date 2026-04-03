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
    
    [Key(1)] public ulong? liegeId {get; private set; } = null;
    //[IgnoreMember] ulong realmId;
    [Key(12)] public List<ulong> allianceIds = [];
    [Key(17)] public List<ulong?> vassalIds { get; set; } = [];
    [IgnoreMember] public Random rng = PopObject.rng;

    // Constants
    [IgnoreMember] const float threatAdjustmentRate = 0.001f; // Rate of threat adjustment for lerping threat

    [Key(0)] ulong stateId;
    [IgnoreMember] State _state;
    [IgnoreMember] public State state { 
        get
        {
            if (_state == null) 
                _state = objectManager.GetState(stateId);
            return _state;
        } 
        set
        {
            stateId = value.id;
            _state = value;
        } 
    }

    public StateDiplomacyManager(){}
    public StateDiplomacyManager(State selectedState)
    {
        state = selectedState;
        state.diplomacy = this;
    }

    // Wars
    public void DeclareWar(State target, WarType type = WarType.CONQUEST)
    {
        //GD.Print($"Trying to start a war between {state.name} and {target.name}");
        objectManager.StartWar(type, state, target);
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
        /*
        foreach (War war in liege.diplomacy.warIds.Keys.Select(id => objectManager.GetWar(id)))
        {
            war.AddParticipant(state, liege.diplomacy.warIds[war.id]);
        }
        */  
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
        List<State> relationStates = [..GetPolity().borderingStates];

        // Removes unneeded relations
        foreach (var pair in relationIds)
        {
            State target = objectManager.GetState(pair.Key);

            if (target == null || (!IsEnemyWithState(target) && !relationStates.Contains(target)))
            {
                RemoveRelations(pair.Key);
                continue;                    
            }
        }

        // Establishes relations
        foreach (State target in relationStates)
        {
            if (target != null && !relationIds.ContainsKey(target.id) && target != state)
            {
                EstablishRelations(target);
            } 
        }  
        CalculateThreats();
    }
    void CalculateThreats(){
        foreach (var pair in relationIds){
            Relation relation = pair.Value;
            State relationState = objectManager.GetState(pair.Key);

            if (relationState == null) continue;          
            float newThreat = 0;

            // Calc Percieved Threat (-1 to 1)
            int stateSize = state.diplomacy.GetPolity().regions.Count;
            int targetSize = relationState.diplomacy.GetPolity().regions.Count;

            float relativeSizeRatio = (stateSize - targetSize)/(float)stateSize;

            newThreat += relativeSizeRatio;

            if (relationState.sovereignty != Sovereignty.INDEPENDENT)
            {
                newThreat *= 0.5f;
            }
            
            // Moves threat for realistic adjustment
            // Eg: If we feared a nation for a while then we wont just immediatly like them when they fall 
            relation.threat = Mathf.MoveToward(relation.threat, Mathf.Clamp(newThreat, -1, 1), threatAdjustmentRate);
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
    public Relation EstablishRelations(State target)
    {
        if (relationIds.TryAdd(target.id, new Relation()))
        {
            target.diplomacy.EstablishRelations(state);
        }
        return relationIds[target.id];
    }

    // Relations Utilities
    public void ChangeOpinion(State target, float value)
    {
        GetMutualRelations(target, out Relation usThem, out Relation themUs);

        usThem.opinion = Math.Clamp(usThem.opinion + value, -1, 1);
        themUs.opinion = Math.Clamp(themUs.opinion + value, -1, 1); 
    }
    public void SetRivalry(State target, bool value)
    {
        GetMutualRelations(target, out Relation usThem, out Relation themUs);
        usThem.rival = value;
        themUs.rival = value;      
    }
    public void GetMutualRelations(State target, out Relation usThem, out Relation themUs)
    {
        usThem = EstablishRelations(target);
        themUs = target.diplomacy.EstablishRelations(state);
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
    public bool HasRelations(State target)
    {
        return relationIds.ContainsKey(target.id);
    }
    // Alliance
    public Alliance GetRealm()
    {
        return GetAllianceOfType(AllianceType.REALM);
    }
    public Alliance GetAllianceOfType(AllianceType desiredType)
    {
        foreach (ulong allianceId in allianceIds)
        {
            Alliance potentialResult = objectManager.GetAlliance(allianceId);
            if (potentialResult.type == desiredType)
            {
                return potentialResult;
            }
        }
        return null;
    }
    
    public Polity GetPolity()
    {
        Alliance realm = GetRealm();
        if (realm == null)
        {
            return state;
        }
        return realm;
    }
    // Vassalage
    public void UpdateRealm()
    {
        if (state.sovereignty == Sovereignty.INDEPENDENT && vassalIds.Count > 0 && GetRealm() == null){
            objectManager.CreateAlliance(state, AllianceType.REALM);
        }
        foreach (ulong? vassalId in vassalIds)
        {
            State vassal = objectManager.GetState(vassalId);
            GetRealm().AddMember(vassal);
        }        
    }
    public void AddVassal(State vassal, Sovereignty sovereignty)
    {
        if (sovereignty == Sovereignty.INDEPENDENT || vassalIds.Contains(vassal.id) || vassal == state) return;

        State vassalFormerLiege = vassal.diplomacy.GetLiege();
        vassalFormerLiege?.diplomacy.RemoveVassal(vassal);

        vassal.sovereignty = sovereignty;
        vassal.diplomacy.liegeId = state.id;
        vassalIds.Add(vassal.id);

        // Updates our realm
        UpdateRealm();
        GetRealm().AddMember(vassal);

        // Gives our vassal NO AUTHORITY ehehhe
        vassal.diplomacy.RemoveAllVassals();        
        vassal.diplomacy.UpdateRealm();

        // Removes vassal from alliance
        vassal.diplomacy.GetAllianceOfType(AllianceType.ALLIANCE)?.RemoveMember(vassal);
    }
    public void RemoveVassal(State vassal)
    {
        if (!vassalIds.Remove(vassal.id)) return;

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
            return GetRealm().leadState;
        }
        return state;
    }
}