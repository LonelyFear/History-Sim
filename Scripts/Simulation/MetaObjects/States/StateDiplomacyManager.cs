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
    //[IgnoreMember] const float threatAdjustmentRate = 0.001f; // Rate of threat adjustment for lerping threat

    // Wars
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
            List<State> states = [..state.borderingStates, ..state.enemies];

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
        DiplomaticRelations relations = state.relations[target];
        bool rightSovereignty = state.sovereignty == Sovereignty.INDEPENDENT && target.sovereignty == Sovereignty.INDEPENDENT;
        bool noTruce = state.relations.ContainsKey(target) && relations.truce < 1;

        return rightSovereignty && noTruce && relations.opinion < -0.2f && !IsAlliedToState(state, target) && !IsEnemyWithState(state, target);
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
        Alliance realm = state.GetRealm();
        if (realm == null)
        {
            return state;
        }
        return realm;
    }
    // Vassalage
    public static void UpdateRealm(this State state)
    {
        if (state.vassals.Count < 1) return;

        if (state.sovereignty == Sovereignty.INDEPENDENT && GetRealm(state) == null){
            objectManager.CreateAlliance(state, AllianceType.REALM);
        }

        foreach (State vassal in state.vassals.ToArray())
        {
            if (vassal.sovereignty != Sovereignty.INDEPENDENT)
            {
                GetRealm(state).AddMember(vassal);
            } else
            {
                state.vassals.Remove(vassal);
                GetRealm(state).RemoveMember(vassal);
            }
            vassal.UpdateRealm(); 
        }        
    }
    public static void AddVassal(this State state, State vassal, Sovereignty sovereignty)
    {
        if (sovereignty == Sovereignty.INDEPENDENT || state.vassals.Contains(vassal) || vassal == state) return;

        vassal.GetLiege()?.RemoveVassal(vassal);

        vassal.sovereignty = sovereignty;
        vassal.liegeId = state.id;
        state.vassals.Add(vassal);

        // Removes our vassal's vassals
        vassal.RemoveAllVassals(); 

        // Updates our realm
        state.UpdateRealm();

        // Removes vassal from alliance
        vassal.GetAllianceOfType(AllianceType.ALLIANCE)?.RemoveMember(vassal);
    }
    public static void RemoveVassal(this State state, State vassal)
    {
        if (!state.vassals.Contains(vassal)) return;

        vassal.sovereignty = Sovereignty.INDEPENDENT;
        vassal.liegeId = null;

        state.UpdateRealm();
    }
    public static void RemoveAllVassals(this State state)
    {
        foreach (State vassal in state.vassals.ToArray())
        {
            state.RemoveVassal(vassal);
        }
    }
    public static State GetLiege(this State state)
    {
        return objectManager.GetState(state.liegeId);
    }
    public static State GetOverlord(this State state)
    {
        if (state.GetRealm() != null)
        {
            return state.GetRealm()?.leadState;
        }
        return state;
    }

}