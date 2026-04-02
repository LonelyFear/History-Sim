using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Godot;
using MessagePack;

[MessagePackObject(AllowPrivate = true)]
public partial class StateAIManager : UtilityAi.AiAgent
{
    [IgnoreMember] public static ObjectManager objectManager;
    [IgnoreMember] public static SimManager simManager;
    [Key(0)] public ulong? stateId;  
    [IgnoreMember] public StateDiplomacyManager diplomacyManager {
        get
        {
            return state.diplomacy;
        }
    }
    [Key(2)] int ticks = 0;
    [Key(3)] List<ulong> endedWarIds;

    // Constants
    [IgnoreMember] const int ticksBetweenTickRecalc = 4;
    [IgnoreMember] const float warChanceMultiplier = 0.01f;
    [IgnoreMember] const float diploChangeChance = 0.01f;

    // Curves
    [IgnoreMember] Curve threatConfidenceCurve = GD.Load<Curve>("res://Curves/Simulation/ThreatConfidenceCurve.tres");

    [IgnoreMember] State _state;
    [IgnoreMember] public State state { 
        get
        {
            if (_state == null && stateId != null) 
                _state = objectManager.GetState(stateId);
            return _state;
        } 
        set
        {
            stateId = value?.id;
            _state = value;
        } 
    }

    public StateAIManager () {}
    public StateAIManager (State sta)
    {
        stateId = sta.id;
        state = sta;
    }
    public float NormalizeNegative(float value) {return (value - 50) / 50f;}
    public float Normalize(float value) {return value / 100f;}

    public void Tick()
    {
        ticks++;
        if (Mathf.PosMod(ticks, ticksBetweenTickRecalc) == 0)
        {
            TickChangeRelations();
            TickDiplomaticAgression();
            TickEndWars();
        }
    }
    public void TickEndWars()
    {
        foreach (var pair in state.diplomacy.warIds)
        {
            War war = objectManager.GetWar(pair.Key);
            War.WarSide side = pair.Value;
            War.WarSide enemySide = War.GetOtherSide(side);
            State enemyWarLead = objectManager.GetState(war.warLeaderIds[enemySide]);
            // Surrender Via Capitulation
            if (state.capitualated && war.warLeaderIds[side] == state.id)
            {
                enemyWarLead.AIManager.CalcWarVictory(war, enemySide);
            }
            // Ends war because we dont even know who we are fighting
            if (!state.diplomacy.HasRelations(enemyWarLead))
            {
                objectManager.EndWar(war);
            }
        }
    }
    public void CalcWarVictory(War war, War.WarSide side)
    {
        War.WarSide enemySide = War.GetOtherSide(side);
        List<ulong> enemyIds = [..war.sideIds[enemySide]];
        switch (war.warType)
        {
            case WarType.CONQUEST:
                foreach (ulong enemyId in enemyIds)
                {
                    State enemyState = objectManager.GetState(enemyId);
                    State[] enemyVassals = [.. enemyState.diplomacy.vassalIds.Select(objectManager.GetState)];

                    foreach (State enemyVassal in enemyVassals)
                    {
                        enemyVassal.GetOccupier()?.diplomacy.AddVassal(enemyVassal, Sovereignty.PUPPET);
                    }
                    
                    if (enemyState.GetOccupier() != null)
                    {
                        enemyState.diplomacy.RemoveAllVassals();
                        enemyState.GetOccupier().diplomacy.AddVassal(enemyState, Sovereignty.PUPPET);
                    }
                }
                break;
        }
        objectManager.EndWar(war);
    }
    public void TickDiplomaticAgression()
    {
        if (state.sovereignty != Sovereignty.INDEPENDENT) return;

        foreach (var pair in diplomacyManager.relationIds)
        {
            State potentialEnemy = objectManager.GetState(pair.Key);
            Relation relations = pair.Value;
            Character leader = state.leader;
            if (potentialEnemy == null || relations == null || leader == null || relations.opinion > 0.5 || !diplomacyManager.CanFightState(potentialEnemy)) continue;

            float animosity = 1f - (relations.opinion/0.5f);
            float confidence = threatConfidenceCurve.Sample(relations.opinion);

            if (rng.NextSingle() < animosity * confidence * warChanceMultiplier)
            {
                diplomacyManager.DeclareWar(potentialEnemy);
            }
        }     
    }
    public void TickChangeRelations()
    {
        foreach (var pair in diplomacyManager.relationIds)
        {
            if (rng.NextSingle() >= diploChangeChance) continue;

            State target = objectManager.GetState(pair.Key);
            Relation relations = pair.Value;
            Character leader = state.leader;
            
            if (target == null || relations == null || leader == null) continue;

            float diplomacyScore = rng.NextSingle();
            float positiveChance = 0f;

            positiveChance = leader.GetPersonalityLevel("agression") switch
            {
                TraitLevel.HIGH => 0.8f,
                TraitLevel.MEDIUM => 0.5f,
                TraitLevel.LOW => 0.2f,
                _ => 1f,
            };

            positiveChance = Mathf.Clamp(positiveChance, 0, 1);
            if (diplomacyScore < positiveChance) {
                diplomacyManager.ChangeOpinion(target, 0.05f);              
            } else {
                diplomacyManager.ChangeOpinion(target, -0.05f);
            }
        }
    }
}
