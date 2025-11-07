using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;

[MessagePackObject]
public class DiplomacyManager
{
    [IgnoreMember] public static ObjectManager objectManager;
    [Key(37)] public List<ulong> allianceIds = new List<ulong>();    
    // Diplomacy
    [Key(19)] public Dictionary<ulong?, int> relationIds { get; set; } = new Dictionary<ulong?, int>();
    [Key(20)] public Dictionary<ulong, bool> warIds { get; set; } = new Dictionary<ulong, bool>();
    [Key(21)] public List<ulong> enemyIds { get; set; } = new List<ulong>();
    [Key(0)] public ulong stateId;
    [IgnoreMember] public State state;
    [IgnoreMember] public Random rng = PopObject.rng;
    public DiplomacyManager(){}
    public DiplomacyManager(State selectedState)
    {
        selectedState.diplomacy = this;
        stateId = selectedState.id;
        state = selectedState;
    }
    public void LoadFromSave()
    {
        state = objectManager.GetState(stateId);
    }
    public void UpdateEnemies()
    {
        List<ulong> atWarWith = new List<ulong>();
        foreach (var pair in warIds)
        {
            War war = objectManager.GetWar(pair.Key);
            bool attacker = pair.Value;
            if (attacker)
            {
                atWarWith.AddRange(war.defenderIds);
            }
            else
            {
                atWarWith.AddRange(war.attackerIds);
            }
        }
        enemyIds = atWarWith;
    }
    #region Relations
    public void RelationsUpdate()
    {
        // All bordering or enemy states
        List<State> relationStates = [.. state.borderingStates, .. enemyIds.Select(id => objectManager.GetState(id))];
        // Removes unneeded relations
        foreach (var pair in relationIds)
        {
            bool isBorderingOrEnemy = relationStates.Contains(objectManager.GetState(pair.Key));
            if (objectManager.GetState(pair.Key) == null || !isBorderingOrEnemy)
            {
                relationIds.Remove(pair.Key);
                continue;
            }
        }
        // Establishes relations
        foreach (State target in relationStates)
        {
            if (target != null && !relationIds.ContainsKey(target.id))
            {
                EstablishRelations(target.id);
            }
        }
    }
    public void EstablishRelations(ulong? targetId, int opinion = 0)
    {
        if (targetId == stateId)
        {
            return;
        }

        if (targetId != null && !relationIds.Keys.Contains(targetId))
        {
            relationIds.Add(targetId, opinion);
        }
        else
        {
            relationIds[targetId] = opinion;
        }
    }
    public void UpdateDiplomacy()
    {
        foreach (var pair in relationIds)
        {
            State target = objectManager.GetState(pair.Key);
            if (state.liege != target && !state.vassals.Contains(target))
            {
                float relationChangeChance = 0.5f;
                float relationDamageChance = 0.5f;
                if (enemyIds.Contains(target.id))
                {
                    relationChangeChance *= 0.75f;
                }
                if (PopObject.rng.NextSingle() < relationChangeChance)
                {
                    relationIds[pair.Key] = -100;
                }
            }
            relationIds[pair.Key] = Mathf.Clamp(relationIds[pair.Key], -100, 100);
        }
    }
    #endregion
    #region Wars
    public void StartWars()
    {
        try
        {
            foreach (var pair in relationIds.ToArray())
            {
                State target = objectManager.GetState(pair.Key);

                if (target == null)
                {
                    GD.PushError("Null Relation: sta" + pair.Key);
                    relationIds.Remove(pair.Key);
                    continue;
                }

                int opinion = pair.Value;
                bool cantStartWar = target == state || enemyIds.Contains(target.id) || target.GetHighestLiege() == state || target.sovereignty != Sovereignty.INDEPENDENT;
                if (cantStartWar)
                {
                    continue;
                }
                // Sovereign Wars
                if (state.sovereignty == Sovereignty.INDEPENDENT && opinion < 0 && state.liege != target)
                {
                    float warDeclarationChance = Mathf.Lerp(0.001f, 0.005f, opinion / -100);
                    if (PopObject.rng.NextSingle() < warDeclarationChance)
                    {
                        //GD.Print("war");
                        //GD.Print("State in realm: " + GetRealmStates().Contains(this));
                        objectManager.StartWar([state], [target], WarType.CONQUEST, stateId, target.id);
                        return;
                    }
                }
                // Rebellions

                if (state.loyalty < State.minRebellionLoyalty && target == state.liege)
                {
                    if (PopObject.rng.NextSingle() < Mathf.Lerp(1 - (state.loyalty / State.minRebellionLoyalty), 0, 0.005))
                    {
                        List<State> fellowRebels = state.GatherRebels();
                        State formerLiege = state.liege;
                        foreach (State rebel in fellowRebels)
                        {
                            formerLiege.RemoveVassal(rebel);
                        }
                        objectManager.StartWar(fellowRebels, formerLiege.GetRealmStates(), WarType.REVOLT, stateId, formerLiege.id);
                        return;
                    }
                }
            }
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }
    } 
  
    public void EndWars()
    {
        if (state.sovereignty != Sovereignty.INDEPENDENT)
        {
            return;
        }
        foreach (var warPair in warIds)
        {
            War war = objectManager.GetWar(warPair.Key);
            bool isAttacker = warPair.Value;

            if (war.primaryAgressorId != stateId && war.primaryDefenderId != stateId)
            {
                continue;
            }
            if (objectManager.GetState(war.primaryAgressorId).sovereignty != Sovereignty.INDEPENDENT || objectManager.GetState(war.primaryDefenderId).sovereignty != Sovereignty.INDEPENDENT)
            {
                EndWar(war);
                continue;
            }
            // Below is if state has authority to end wars
            double warEndChance = 0;
            bool enemyCapitualated = warIds[warPair.Key] ? objectManager.GetState(war.primaryDefenderId).capitualated : objectManager.GetState(war.primaryAgressorId).capitualated;
            switch (war.warType)
            {
                default:
                    //warEndChance = Mathf.Max(relationIds[war.primaryDefenderId], 0) * 0.01;
                    if (enemyCapitualated)
                    {
                        warEndChance = 1;
                    }
                    break;
            }
            // If there isnt a chance of the war ending just skip
            if (rng.NextSingle() >= warEndChance)
            {
                return;
            }
            objectManager.EndWar(war); // Just ends the 
            
            // War end terms, assumes state is the victor (Which it should be)
            switch (war.warType)
            {
                case WarType.CONQUEST:
                    try
                    {
                        if (isAttacker)
                        {
                            foreach (ulong defenderId in war.defenderIds)
                            {
                                State defender = objectManager.GetState(defenderId);
                                if (!defender.capitualated || defender.capital.occupier == null)
                                {
                                    continue;
                                }
                                defender.capital.occupier.AddVassal(defender);
                            }
                        }
                        else
                        {
                            foreach (ulong attackerId in war.attackerIds)
                            {
                                State attacker = objectManager.GetState(attackerId);
                                if (!attacker.capitualated)
                                {
                                    return;
                                }
                                attacker.capital.occupier.AddVassal(attacker);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        GD.PushError(e);
                    }
                    break;
                case WarType.REVOLT:
                    try
                    {
                        if (isAttacker)
                        {
                            foreach (State vassal in objectManager.GetState(war.primaryDefenderId).vassals.ToArray())
                            {
                                //simManager.GetState(war.primaryDefenderId).RemoveVassal(vassal);
                            }
                        }
                        else
                        {
                            foreach (ulong rebelId in war.attackerIds)
                            {
                                State rebel = objectManager.GetState(rebelId);
                                state.AddVassal(rebel);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        GD.PushError(e);
                    }

                    break;
                case WarType.CIVIL_WAR:
                    try
                    {
                        if (isAttacker)
                        {
                            objectManager.DeleteState(objectManager.GetState(war.primaryDefenderId));                                
                        }
                        else
                        {
                            foreach (ulong rebelId in war.attackerIds)
                            {
                                State rebel = objectManager.GetState(rebelId);
                                if (rebel != null)
                                {
                                    state.AddVassal(rebel);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        GD.PushError(e);
                    }

                    break;
            }
        }
    }
    public void EndWar(War war)
    {
        objectManager.EndWar(war);
    }  
    #endregion
}