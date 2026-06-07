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
    [Key(5)] public Dictionary<ulong, Relation> relationIds { get; set; } = [];
    [IgnoreMember] public Dictionary<State, Relation> relations { get; set; } = [];
    [Key(4)] public Dictionary<ulong, War.WarSide> warIds { get; set; } = [];
    [IgnoreMember] public Dictionary<War, War.WarSide> wars { get; set; } = [];
    [Key(1)] public ulong? liegeId {get; private set; } = null;
    //[IgnoreMember] ulong realmId;
    [Key(2)] public List<ulong?> allianceIds = [];
    [IgnoreMember] public List<Alliance> alliances = [];
    [Key(3)] public HashSet<ulong?> vassalIds { get; set; } = [];
    [IgnoreMember] public HashSet<State> vassals = [];
    [IgnoreMember] public Random rng = PopObject.rng;

    [Key(6)] public int relationUpdateTime = 12;
    [IgnoreMember] public HashSet<State> contactedStates = [];

    [Key(7)] public HashSet<ulong?> enemyIds = [];
    [IgnoreMember] public HashSet<State> enemies = [];

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
                if (ally.sovereignty != Sovereignty.INDEPENDENT) continue;

                foreach (var allyWarPair in ally.diplomacy.wars)
                {
                    if (!wars.ContainsKey(allyWarPair.Key))
                    {
                        War war = allyWarPair.Key;

                        lock (war)
                        {
                            war.AddParticipant(state, allyWarPair.Value);
                            return;
                        }
                    }
                }
            }            
        }
    }
    public void LeaveWarsWithState(State target)
    {
        if (!IsEnemyWithState(target)) return;

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
    public void SetEnemy(State target, bool isEnemy)
    {
        if (isEnemy && enemies.Add(target)) {
            target.diplomacy.enemies.Add(target);
            
        }
        else if (enemies.Remove(target)){
            target.diplomacy.enemies.Remove(target);
            
        };

        //state.diplomacy.relations[target].enemy = isEnemy;
        //target.diplomacy.relations[state].enemy = isEnemy;
    }
    public void SetEnemies(IEnumerable<ulong> stateIds, bool isEnemy)
    {
        foreach (ulong stateId in stateIds)
        {
            SetEnemy(objectManager.GetState(stateId), isEnemy);     
        }
    }

    // Relations
    public void UpdateRelations()
    {
        try
        {
            // All bordering or enemy states
            if (state.sovereignty == Sovereignty.INDEPENDENT)
            {
                contactedStates = [..GetPolity().borderingStates, ..enemies, GetLiege(), GetOverlord()];
            } else
            {
                contactedStates = [..state.borderingStates, ..enemies, GetLiege(), GetOverlord()];
            }
            contactedStates.Remove(state);
            contactedStates.Remove(null);

            foreach (State target in contactedStates)
            {
                if (relations.ContainsKey(target)) continue;
                relations[target] = new Relation();
                //target.diplomacy.relations.TryAdd(state, new Relation());
            }            
        } catch (Exception e)
        {
            GD.PushError(e);
        }
    }
    public void CalculateThreats(){
        foreach (State target in contactedStates){
            Relation relation = relations[target];

            if (target == null) continue;          
            float newThreat = 0;

            // Calc Percieved Threat (-1 to 1)
            int stateSize = state.diplomacy.GetPolity().regions.Count;
            int targetSize = target.diplomacy.GetPolity().regions.Count;

            float relativeSizeRatio = (stateSize - targetSize)/Mathf.Max(stateSize, 0.001f);

            newThreat += relativeSizeRatio;

            if (target.sovereignty != Sovereignty.INDEPENDENT)
            {
                newThreat *= 0.5f;
            }
            
            // Moves threat for realistic adjustment
            // Eg: If we feared a nation for a while then we wont just immediatly like them when they fall 
            relation.threat = Mathf.MoveToward(relation.threat, Mathf.Clamp(newThreat, -1, 1), threatAdjustmentRate);
        }
    }

    /*
    public void RemoveRelations(State target)
    {
        if (target == null) return;

        if (relations.Remove(target, out Relation _))
        {
            target.diplomacy.RemoveRelations(state);
        }     
    }
    */
    // Relations Utilities
    public void ChangeOpinion(State target, float value)
    {
        Relation relations = state.diplomacy.relations[target];
        relations.opinion = Math.Clamp(relations.opinion + value, -1, 1); 

        Relation targetRelations = target.diplomacy.relations[state];
        targetRelations.opinion = Math.Clamp(targetRelations.opinion + value, -1, 1);
    }
    public void SetRivalry(State target, bool value)
    {
        target.diplomacy.relations[state].rival = value;
        state.diplomacy.relations[target].rival = value;      
    } 
    public void SetTruce(State target, uint value)
    {
        target.diplomacy.relations[state].truce = value;
        state.diplomacy.relations[target].truce = value;      
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
        && !IsAlliedToState(target) && relations[target]?.truce < 1
        && !IsEnemyWithState(target);
    }
    public bool IsEnemyWithState(State otherState)
    {
        if (relations.TryGetValue(otherState, out Relation relation))
        {
            return enemies.Contains(otherState);
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
    public bool HasRelations(State target)
    {
        return relations.ContainsKey(target);
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