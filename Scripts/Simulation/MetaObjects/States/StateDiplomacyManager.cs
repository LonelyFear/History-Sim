using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;

[MessagePackObject(AllowPrivate = true)]
public partial class StateDiplomacyManager
{
    [IgnoreMember] public static ObjectManager objectManager;
    [Key(37)] public List<ulong> allianceIds = [];    
    // Diplomacy
    [Key(19)] Dictionary<ulong?, Relation> relationIds { get; set; } = [];
    [Key(20)] public Dictionary<ulong, bool> warIds { get; set; } = [];
    [Key(21)] public List<ulong> enemyIds { get; private set; } = [];
    [Key(0)] public ulong stateId;
    [IgnoreMember] State state;
    [IgnoreMember] public Random rng = PopObject.rng;
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
    public void UpdateEnemies()
    {
        List<ulong> newEnemies = [];
        foreach (var pair in relationIds)
        {
            ulong? potentialEnemyId = pair.Key;
            Relation relation = pair.Value;
            if (relation.enemy) newEnemies.Add((ulong)potentialEnemyId);
        }
        enemyIds = newEnemies;
    }
    public void AddEnemy(ulong stateId)
    {
        if (!relationIds.ContainsKey(stateId))
        {
            EstablishRelations(stateId);
        }
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
        Relation relation = relationIds[stateId];
        relation.enemy = false;
    }
    public void RelationsUpdate()
    {
        // All bordering or enemy states
        List<State> relationStates = [.. state.borderingStates, .. enemyIds.Select(id => objectManager.GetState(id))];
        // Removes unneeded relations
        foreach (var pair in relationIds)
        {
            State target = objectManager.GetState(pair.Key);
            if (target == null || !relationStates.Contains(target))
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
    public Relation GetRelations(ulong state)
    {
        return relationIds[state];
    }
    public void UpdateDiplomacy()
    {
        foreach (var pair in relationIds)
        {
            State target = objectManager.GetState(pair.Key);
            if (target == null) continue;
            if (state.realmId != target.realmId)
            {
                float relationChangeChance = 0.5f;
                //float relationDamageChance = 0.5f;
                if (enemyIds.Contains(target.id))
                {
                    relationChangeChance *= 0.75f;
                }
                if (PopObject.rng.NextSingle() < relationChangeChance)
                {
                    relationIds[pair.Key].SetOpinion(-100);
                }
            }
        }
    }    
    public void RemoveRelations(ulong? targetId)
    {
        if (targetId != null && !relationIds.Keys.Contains(targetId))
        {
            relationIds.Remove(targetId);
        }
    }
    public void EstablishRelations(ulong? targetId, int opinion = 0)
    {
        if (targetId == stateId || objectManager.GetState(targetId) == null)
        {
            return;
        }

        if (targetId != null && !relationIds.Keys.Contains(targetId))
        {
            relationIds.Add(targetId, new Relation(opinion));
        }
        else
        {
            relationIds[targetId].SetOpinion(opinion);
        }
    }
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

                int opinion = pair.Value.opinion;
                bool cantStartWar = target == state || enemyIds.Contains(target.id) || target.vassalManager.GetOverlord(true) == state || target.sovereignty != Sovereignty.INDEPENDENT;
                if (cantStartWar)
                {
                    continue;
                }
                // Sovereign Wars
                if (state.sovereignty == Sovereignty.INDEPENDENT && opinion < 0 && state.vassalManager.GetLiege() != target)
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

                if (state.loyalty < State.minRebellionLoyalty && target == state.vassalManager.GetLiege())
                {
                    if (PopObject.rng.NextSingle() < Mathf.Lerp(1 - (state.loyalty / State.minRebellionLoyalty), 0, 0.005))
                    {
                        List<State> fellowRebels = state.GatherRebels();
                        State formerLiege = state.vassalManager.GetLiege();
                        foreach (State rebel in fellowRebels)
                        {
                            formerLiege.vassalManager.RemoveVassal(rebel.id);
                        }
                        objectManager.StartWar(fellowRebels, [formerLiege], WarType.REVOLT, stateId, formerLiege.id);
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
                                defender.capital.occupier.vassalManager.AddVassal(defenderId);
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
                                attacker.capital.occupier.vassalManager.AddVassal(attackerId);
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
                            foreach (State vassal in objectManager.GetState(war.primaryDefenderId).vassalManager.GetVassals())
                            {
                                //simManager.GetState(war.primaryDefenderId).RemoveVassal(vassal);
                            }
                        }
                        else
                        {
                            foreach (ulong rebelId in war.attackerIds)
                            {
                                State rebel = objectManager.GetState(rebelId);
                                state.vassalManager.AddVassal(rebelId);
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
                            //objectManager.DeleteState(objectManager.GetState(war.primaryDefenderId));                                
                        }
                        else
                        {
                            foreach (ulong rebelId in war.attackerIds)
                            {
                                State rebel = objectManager.GetState(rebelId);
                                if (rebel != null)
                                {
                                    state.vassalManager.AddVassal(rebelId);
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
}