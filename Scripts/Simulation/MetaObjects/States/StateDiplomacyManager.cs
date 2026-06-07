using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using Godot;
using MessagePack;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.Wars;

namespace PixelHistory.Objects.States.Diplomacy;
static class StateDiplomacyManager
{
    [IgnoreMember] public static ObjectManager objectManager;
    // Diplomacy
    // Constants
    [IgnoreMember] const float threatAdjustmentRate = 0.001f; // Rate of threat adjustment for lerping threat

    // Wars
    public static War DeclareWar(this State initiator, State target, WarType type = WarType.CONQUEST)
    {
        //GD.Print($"Trying to start a war between {state.name} and {target.name}");
        return objectManager.StartWar(type, initiator, target);
    }
    public static void JoinObligateWars(this State state)
    {
        if (state.sovereignty == Sovereignty.REBELLIOUS) return;

        foreach (Alliance alliance in state.alliances)
        {
            foreach (State ally in alliance.memberStates)
            {
                if (ally.sovereignty != Sovereignty.INDEPENDENT) continue;

                foreach (var allyWarPair in ally.wars)
                {
                    if (!state.wars.ContainsKey(allyWarPair.Key))
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
    public static void LeaveAllWars(this State state)
    {
        foreach (War war in state.wars.Keys.ToArray())
        {
            war.RemoveParticipant(state);
        }
    } 
    // Enemy Utility
    public static void SetEnemy(this State state, State target, bool isEnemy)
    {
        if (isEnemy && state.enemies.Add(target)) {
            target.enemies.Add(target);
            
        }
        else if (state.enemies.Remove(target)){
            target.enemies.Remove(target);
            
        };

        //state.relations[target].enemy = isEnemy;
        //target.relations[state].enemy = isEnemy;
    }
    public static void SetEnemies(this State state, IEnumerable<ulong> stateIds, bool isEnemy)
    {
        foreach (ulong stateId in stateIds)
        {
            SetEnemy(state, objectManager.GetState(stateId), isEnemy);     
        }
    }

    // Relations
    public static void UpdateRelations(this State state)
    {
        try
        {
            // All bordering or enemy states
            List<State> states = [..state.borderingStates, ..state.enemies, GetLiege(state)];

            foreach (State target in states)
            {
                if (target == state || target == null || state.relations.ContainsKey(target)) continue;
                lock (objectManager)
                {
                    objectManager.EstablishRelations(state, target);
                }
            }            
        } catch (Exception e)
        {
            GD.PushError(e);
        }
    }
    // Check utilities
    public static War GetWarWithState(this State state, State target)
    {
        foreach (var pair in state.wars)
        {
            War war = pair.Key;
            if (war.participantIds.Contains(target.id))
            {
                return war;
            }
        }
        return null;
    }
    public static bool InWarOfType(this State state, WarType type)
    {
        foreach (var pair in state.wars)
        {
            if (pair.Key.warType == type)
            {
                return true;
            }
        }
        return false;
    }
    public static bool CanFightState(this State state, State target)
    {
        return state.sovereignty == Sovereignty.INDEPENDENT && target.sovereignty == Sovereignty.INDEPENDENT 
        && !IsAlliedToState(state, target) && state.relations[target]?.truce < 1
        && !IsEnemyWithState(state, target);
    }
    public static bool IsEnemyWithState(this State state, State otherState)
    {
        return state.enemies.Contains(otherState);
    }
    public static bool IsAlliedToState(this State state, State otherState)
    {
        foreach (Alliance alliance in state.alliances)
        {
            if (alliance.HasMember(otherState))
            {
                return true;
            }
        }
        return false;
    }
    public static bool HasRelations(this State state, State target)
    {
        return state.relations.ContainsKey(target);
    }
    // Alliance
    public static Alliance GetRealm(this State state)
    {
        return GetAllianceOfType(state, AllianceType.REALM);
    }
    public static Alliance GetAllianceOfType(this State state, AllianceType desiredType)
    {
        foreach (Alliance potentialResult in state.alliances)
        {
            if (potentialResult.type == desiredType)
            {
                return potentialResult;
            }
        }
        return null;
    }
    
    public static Polity GetPolity(this State state)
    {
        Alliance realm = GetRealm(state);
        if (realm == null)
        {
            return state;
        }
        return realm;
    }
    // Vassalage
    public static void UpdateRealm(this State state)
    {
        if (state.sovereignty == Sovereignty.INDEPENDENT && state.vassals.Count > 0 && GetRealm(state) == null){
            objectManager.CreateAlliance(state, AllianceType.REALM);
        }
        foreach (State vassal in state.vassals)
        {
            GetRealm(state).AddMember(vassal);
        }        
    }
    public static void AddVassal(this State state, State vassal, Sovereignty sovereignty)
    {
        if (sovereignty == Sovereignty.INDEPENDENT || state.vassals.Contains(vassal) || vassal == state) return;

        State vassalFormerLiege = GetLiege(vassal);
        if (vassalFormerLiege != null) RemoveVassal(vassalFormerLiege, vassal);

        vassal.sovereignty = sovereignty;
        vassal.liegeId = state.id;
        state.vassals.Add(vassal);

        // Updates our realm
        UpdateRealm(state);
        GetRealm(state).AddMember(vassal);

        // Gives our vassal NO AUTHORITY ehehhe
        RemoveAllVassals(vassal);        
        UpdateRealm(state);

        // Removes vassal from alliance
        GetAllianceOfType(vassal, AllianceType.ALLIANCE)?.RemoveMember(vassal);
    }
    public static void RemoveVassal(this State state, State vassal)
    {
        if (!state.vassals.Remove(vassal)) return;

        vassal.sovereignty = Sovereignty.INDEPENDENT;
        vassal.liegeId = null;

        GetRealm(state).RemoveMember(vassal);
        UpdateRealm(vassal);
    }
    public static void RemoveAllVassals(this State state)
    {
        foreach (State vassal in state.vassals.ToArray())
        {
            RemoveVassal(state, vassal);
        }
    }
    public static State GetLiege(this State state)
    {
        return objectManager.GetState(state.liegeId);
    }
    public static State GetOverlord(this State state)
    {
        if (GetRealm(state) != null)
        {
            return GetRealm(state).leadState;
        }
        return state;
    }

}