using System;
using System.Collections;
using System.Collections.Concurrent;
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
    [Key(5)] public ConcurrentDictionary<ulong?, Relation> relationIds { get; set; } = [];
    [Key(4)] public ConcurrentDictionary<ulong, War.WarSide> warIds { get; set; } = [];
    [IgnoreMember] public ConcurrentDictionary<War, War.WarSide> wars { get; set; } = [];
    [Key(1)] public ulong? liegeId {get; private set; } = null;
    //[IgnoreMember] ulong realmId;
    [Key(2)] public List<ulong?> allianceIds = [];
    [IgnoreMember] public List<Alliance> alliances = [];
    [Key(3)] public HashSet<ulong?> vassalIds { get; set; } = [];
    [IgnoreMember] public HashSet<State> vassals = [];
    [IgnoreMember] public Random rng = PopObject.rng;

    [Key(6)] public int relationUpdateTime = 12;
    // Constants
    [IgnoreMember] const float threatAdjustmentRate = 0.001f; // Rate of threat adjustment for lerping threat

    [Key(0)] ulong stateId;
    [IgnoreMember] State _state;
    [IgnoreMember] public State state { 
        get
        {
            _state ??= objectManager.GetState(stateId);
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
    public War DeclareWar(State target, WarType type = WarType.CONQUEST)
    {
        //GD.Print($"Trying to start a war between {state.name} and {target.name}");
        return objectManager.StartWar(type, state, target);
    }
    public void JoinObligateWars()
    {
        if (state.sovereignty == Sovereignty.REBELLIOUS) return;

        foreach (Alliance alliance in alliances)
        {
            foreach (State ally in alliance.memberStates)
            {
                foreach (var allyWarPair in ally.diplomacy.wars)
                {
                    if (!wars.ContainsKey(allyWarPair.Key))
                    {
                        War war = allyWarPair.Key;

                        lock (war)
                        {
                            war.AddParticipant(state, allyWarPair.Value);
                        }
                    }
                }
            }            
        }
    }
    public void LeaveWarsWithState(State target)
    {
        if (!relationIds[target.id].enemy) return;

        foreach (War war in wars.Keys.ToArray())
        {
            if (war.participantIds.Contains(target.id)) war.RemoveParticipant(state);
        }        
    }
    public void LeaveAllWars()
    {
        foreach (War war in wars.Keys.ToArray())
        {
            war.RemoveParticipant(state);
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
        HashSet<State> relationStates = [..GetPolity().borderingStates];
        if (GetRealm() != null)
        {
            relationStates = [..GetPolity().borderingStates, ..GetRealm().memberStates];
        }
        relationStates.Add(GetLiege());
        relationStates.Add(GetOverlord());

        // Removes unneeded relations
        foreach (var pair in relationIds.ToArray())
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
        foreach (var pair in relationIds.ToArray()){
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

        if (relationIds.Remove(targetId, out Relation _))
        {
            target.diplomacy.RemoveRelations(state.id);
        }     
    }
    public Relation EstablishRelations(State target)
    {
        Relation relation = relationIds.GetOrAdd(target.id, new Relation());
        target.diplomacy.relationIds.GetOrAdd(state.id, new Relation());

        return relation;
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
    public War GetWarWithState(State target)
    {
        foreach (var pair in wars)
        {
            War war = pair.Key;
            if (war.participantIds.Contains(target.id))
            {
                return war;
            }
        }
        return null;
    }
    public bool InWarOfType(WarType type)
    {
        foreach (var pair in wars)
        {
            if (pair.Key.warType == type)
            {
                return true;
            }
        }
        return false;
    }
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
        foreach (Alliance alliance in alliances)
        {
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
        foreach (Alliance potentialResult in alliances)
        {
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
        if (state.sovereignty == Sovereignty.INDEPENDENT && vassals.Count > 0 && GetRealm() == null){
            objectManager.CreateAlliance(state, AllianceType.REALM);
        }
        foreach (State vassal in vassals)
        {
            GetRealm().AddMember(vassal);
        }        
    }
    public void AddVassal(State vassal, Sovereignty sovereignty)
    {
        if (sovereignty == Sovereignty.INDEPENDENT || vassals.Contains(vassal) || vassal == state) return;

        State vassalFormerLiege = vassal.diplomacy.GetLiege();
        vassalFormerLiege?.diplomacy.RemoveVassal(vassal);

        vassal.sovereignty = sovereignty;
        vassal.diplomacy.liegeId = state.id;
        vassals.Add(vassal);

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
        if (!vassals.Remove(vassal)) return;

        vassal.sovereignty = Sovereignty.INDEPENDENT;
        vassal.diplomacy.liegeId = null;

        GetRealm().RemoveMember(vassal);
        vassal.diplomacy.UpdateRealm();
    }
    public void RemoveAllVassals()
    {
        foreach (State vassal in vassals.ToArray())
        {
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