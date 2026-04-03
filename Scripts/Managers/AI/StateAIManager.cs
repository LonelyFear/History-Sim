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
    [Key(0)] public ulong? stateId { get; set; }  
    [IgnoreMember] public StateDiplomacyManager diplomacyManager {
        get
        {
            return state.diplomacy;
        }
    }
    [Key(2)] int ticks { get; set; } = 0;

    // Constants
    [IgnoreMember] const int ticksBetweenTickRecalc = 4;
    [IgnoreMember] const float warChanceMultiplier = 0.01f;
    [IgnoreMember] const float allyChanceMultiplier = 0.01f;
    [IgnoreMember] const float diploChangeChance = 0.25f;

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
            TickDiplomacy();
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
            // Ends war because we dont even know who we are fightin
            if (!state.diplomacy.HasRelations(enemyWarLead) || !state.borderingStates.Contains(enemyWarLead))
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
    public void TickDiplomacy()
    {
        if (state.sovereignty != Sovereignty.INDEPENDENT) return;

        foreach (var pair in diplomacyManager.relationIds)
        {
            State target = objectManager.GetState(pair.Key);
            Relation relations = pair.Value;
            Character leader = state.leader;
            if (target == null || relations == null || leader == null) continue;

            if (relations.opinion > 0 && !diplomacyManager.IsEnemyWithState(target))
            {
                // Positive
                float goodwill = relations.opinion;

                Alliance ourAlliance = diplomacyManager.GetAllianceOfType(AllianceType.ALLIANCE);
                Alliance otherAlliance = target.diplomacy.GetAllianceOfType(AllianceType.ALLIANCE);

                if (rng.NextSingle() < goodwill * allyChanceMultiplier)
                {
                    if (ourAlliance == null && otherAlliance == null)
                    {
                        Alliance newAlliance = objectManager.CreateAlliance(state, AllianceType.ALLIANCE);
                        newAlliance.AddMember(target);
                        GD.Print($"Alliance between {state.baseName} and {target.baseName}");
                    } 
                    else if (ourAlliance == null)
                    {
                        TryJoinAlliance(otherAlliance);
                        GD.Print($"Alliance between {state.baseName} and {target.baseName}");
                    }
                }
            } else
            {
                // Negative
                float animosity = Math.Abs(relations.opinion);

                if (diplomacyManager.CanFightState(target))
                {
                    // Wars
                    float confidence = threatConfidenceCurve.Sample(relations.opinion);

                    if (rng.NextSingle() < animosity * confidence * warChanceMultiplier)
                    {
                        diplomacyManager.DeclareWar(target);
                    }                     
                } else
                {
                    // Other Forms of Agression
                    if (diplomacyManager.IsAlliedToState(target) && rng.NextSingle() < animosity * warChanceMultiplier)
                    {
                        diplomacyManager.GetAllianceOfType(AllianceType.ALLIANCE)?.RemoveMember(state);
                    }
                }
            }
        }     
    }
    public void TryJoinAlliance(Alliance alliance)
    {
        foreach (State member in alliance.memberStates)
        {
            if (diplomacyManager.HasRelations(member))
            {
                float opinion = Mathf.InverseLerp(diplomacyManager.GetRelationsWithState(member).opinion, -1, 1);
                if (rng.NextSingle() > opinion)
                {
                    // We are blocked
                    return;
                }
            }
        }
        alliance.AddMember(state);
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
                diplomacyManager.ChangeOpinion(target, 0.1f);              
            } else {
                diplomacyManager.ChangeOpinion(target, -0.1f);
            }
        }
    }
}
