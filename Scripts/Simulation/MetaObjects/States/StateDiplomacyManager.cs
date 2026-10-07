using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.Wars;

namespace PixelHistory.Objects.States.Diplomacy;
static class StateDiplomacyManager
{ 
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
            target.enemies.Add(state);
        }
        else if (state.enemies.Remove(target)){
            target.enemies.Remove(state);
        };
    }
    public static void SetEnemies(this State state, IEnumerable<State> newEnemies, bool isEnemy)
    {
        foreach (State target in newEnemies)
        {
            state.SetEnemy(target, isEnemy);     
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
                ObjectManager.EstablishRelations(state, target);
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
            if (war.participants.Contains(target))
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
        bool bothIndependent = state.sovereignty == Sovereignty.INDEPENDENT && target.sovereignty == Sovereignty.INDEPENDENT;
        bool noTruce = relations.truce < 1;
        bool fightingTogether = false;

        foreach (var pair in state.wars)
        {
            War war = pair.Key;
            if (war.GetAllies(state).Contains(target))
            {
                fightingTogether = true;
                break;
            }
        }

        return bothIndependent && noTruce && !fightingTogether && relations.opinion < -0.2f && !state.IsAlliedToState(target) && !state.IsEnemyWithState(target) && state.borderingStates.Contains(target);
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
            ObjectManager.CreateAlliance(state, AllianceType.REALM);
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
    public static void AddVassal(this State state, State vassal, Sovereignty sovereignty, bool wartime = false)
    {
        if (sovereignty == Sovereignty.INDEPENDENT || state.vassals.Contains(vassal) || vassal == state) return;

        State lastLiege = vassal.GetLiege();
        lastLiege?.RemoveVassal(vassal);
        if (!wartime)
        {
            vassal.lastLiegeId = lastLiege?.id;
        }
        
        vassal.sovereignty = sovereignty;
        vassal.liegeId = state.id;
        state.vassals.Add(vassal);

        // Returns Territory
        foreach (Region claim in vassal.claims)
        {
            if (claim.owner.GetOverlord() == state)
            {
                vassal.AddRegion(claim, true);
            }
        }
        // Forces vassal to leave wars
        vassal.LeaveAllWars();
        
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
        return ObjectManager.GetState(state.liegeId);
    }
    public static State GetLastLiege(this State state)
    {
        return ObjectManager.GetState(state.liegeId);
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