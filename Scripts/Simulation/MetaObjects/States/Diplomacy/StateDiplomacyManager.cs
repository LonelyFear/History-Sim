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
    [IgnoreMember] const float threatAdjustmentRate = 0.1f; // Rate of threat adjustment for lerping threat
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
            if (relationIds.ContainsKey(target.id) && target != null && target.id != stateId)
            {
                EstablishRelations(target.id);
            } 
        }  
        UpdateRelationValues();
    }
    void UpdateRelationValues(){
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

            
            float newThreat = 0;
            // Calc Percieved Threat (-100 to 100)
            int stateSize = state.GetSize(true);
            newThreat += (stateSize - relationState.GetSize(true))/(float)stateSize * 50;
            
            // Moves threat for realistic adjustment
            // Eg: If we feared a nation for a while then we wont just immediatly like them when they fall 
            relation.threat = Mathf.MoveToward(relation.threat, newThreat, threatAdjustmentRate);
        }
    }
    public void LeaveAllWars()
    {
        foreach (ulong warId in warIds.Keys.ToArray())
        {
            War war = objectManager.GetWar(warId);
            war.RemoveParticipant(stateId);
        }
    }
    public void JoinLiegeWars()
    {
        State liege = state.vassalManager.GetLiege();
        
        foreach (War war in liege.diplomacy.warIds.Keys.Select(id => objectManager.GetWar(id)))
        {
            war.AddParticipant(state.id, liege.diplomacy.warIds[war.id]);
        }  
    } 
    public void RemoveRelations(ulong? targetId)
    {
        if (relationIds.ContainsKey(targetId))
        {
            relationIds.Remove(targetId);
        }
    }
    public void EstablishRelations(ulong? targetId, int opinion = 0)
    {
        if (targetId == null || targetId == stateId || objectManager.GetState(targetId) == null)
        {
            if (objectManager.GetState(targetId) == null)
            {
                GD.PushError("Trying to establish relations with nonexistent state");
            }
            if (targetId == stateId)
            {
                GD.PushError("Trying to establish relations with self");
            }
            return;
        }

        if (!relationIds.ContainsKey(targetId))
        {
            relationIds.Add(targetId, new Relation(opinion, false, false));

            if (state.borderingStateIds.TryGetValue((ulong)targetId, out int borderLength))
            {
                relationIds[targetId].borderLength = borderLength;
            }
        }
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

    public Relation GetRelationsWithState(State state)
    {
        Relation relation = null;
        relationIds.TryGetValue(state.id, out relation);
        return relation;
    }
   
    /*
    public void EndWars()
    {
        foreach (var warPair in warIds)
        {
            War war = objectManager.GetWar(warPair.Key);
            bool isAttacker = warPair.Value;

            if (war.primaryAgressorId != stateId && war.primaryDefenderId != stateId)
            {
                continue;
            }
            // If one of the leading parties are no longer valid war leaders
            if (objectManager.GetState(war.primaryAgressorId).vassalManager.sovereignty != Sovereignty.INDEPENDENT || 
            objectManager.GetState(war.primaryDefenderId).vassalManager.sovereignty != Sovereignty.INDEPENDENT)
            {
                war.RemoveParticipant(stateId);
                continue;
            }
            // Below is if state has authority to end wars
            double warEndChance = 0;
            bool enemyCapitualated = isAttacker ? objectManager.GetState(war.primaryDefenderId).capitualated : objectManager.GetState(war.primaryAgressorId).capitualated;
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
                continue;
            }
            List<ulong> formerDefenderIds = [..war.defenderIds];
            List<ulong> formerAttackerIds = [..war.attackerIds];
            objectManager.EndWar(war); // Just ends the war
            // War end terms, assumes state is the victor (Which it should be)
            switch (war.warType)
            {
                case WarType.CONQUEST:
                    // Wars of conquest
                    try
                    {
                        if (isAttacker)
                        {
                            // Attacker victory
                            foreach (ulong defenderId in formerDefenderIds)
                            {
                                State defender = objectManager.GetState(defenderId);
                                if (!defender.capitualated || defender.capital.occupier == null)
                                {
                                    continue;
                                }
                                //GD.Print("Capitualated Defender Puppeted");
                                defender.capital.occupier.vassalManager.AddVassal(defenderId);
                            }
                        }
                        else
                        {
                            // Defender Victory
                            foreach (ulong attackerId in formerAttackerIds)
                            {
                                State attacker = objectManager.GetState(attackerId);
                                if (!attacker.capitualated || attacker.capital.occupier == null)
                                {
                                    continue;
                                }
                                //GD.Print("Capitualated Attacker Puppeted");
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
    */
}