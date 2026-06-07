using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Godot;
using MessagePack;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.States.Diplomacy;
using PixelHistory.Objects.Wars;

namespace PixelHistory.Objects.States.AI;
[MessagePackObject(AllowPrivate = true)]
public partial class StateAIManager : UtilityAi.AiAgent
{
    [IgnoreMember] public static ObjectManager objectManager;
    [IgnoreMember] public static SimManager simManager;
    [Key(0)] public ulong? stateId { get; set; }  
    [Key(2)] int ticks { get; set; } = 0;

    // Constants
    [IgnoreMember] const int ticksBetweenTickRecalc = 4;
    [IgnoreMember] const float warChanceMultiplier = 0.01f;
    [IgnoreMember] const float allyChanceMultiplier = 0.01f;
    [IgnoreMember] const float diploChangeChance = 0.25f;

    // Curves
    [IgnoreMember] Curve warEndChanceCurve = GD.Load<Curve>("res://Curves/Simulation/WarEndChanceCurve.tres");
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
            if (state.sovereignty == Sovereignty.INDEPENDENT)
            {
                TickDiplomacy();               
            }       
            TickEndWars();
            //ReevaluateCivilWarSiding();
        }
    }
    public void TickEndWars()
    {
        foreach (var pair in state.wars)
        {
            War war = pair.Key;

            War.WarSide side = pair.Value;
            War.WarSide enemySide = War.GetOtherSide(side);

            State enemyWarLead = objectManager.GetState(war.warLeaderIds[enemySide]);
            DiplomaticRelations relations = state.relations[enemyWarLead];

            if (war.warLeaderIds[side] != state.id) continue;

            switch (war.warType)
            {
                case WarType.CONQUEST:
                    // Conquest Wars
                    if (state.capitualated)
                    {
                        CalcWarEnd(war, enemySide);
                    }

                    // Peaceful Ending
                    float warEndChance = warEndChanceCurve.Sample(relations.opinion + 0.5f);
                    if (rng.NextSingle() < warEndChance)
                    {
                        CalcWarEnd(war);
                    }
                    break;
                case WarType.CIVIL_WAR:

                    bool surrender = state.capitualated;
                    if (side == War.WarSide.AGRESSOR)
                    {
                        // Rebel
                        foreach (State rebel in war.sideIds[side].Select(i => objectManager.GetState(i)))
                        {
                            if (!rebel.capitualated) surrender = false;
                            break;
                        }
                        surrender = surrender || state.sovereignty != Sovereignty.REBELLIOUS;
                    }

                    if (surrender)
                    {
                        CalcWarEnd(war, enemySide);
                    }
                    break;
            }

            /*
            // Ends war because we dont even know who we are fightin
            if (state.HasRelations(enemyWarLead) || !state.borderingStates.Contains(enemyWarLead))
            {
                //objectManager.EndWar(war);
            }
            */
        }
    }
    public void CalcWarEnd(War war, War.WarSide? victor = null)
    {
        foreach (War.WarSide side in war.sideIds.Keys)
        {
            if (victor != null && victor != side) continue;

            War.WarSide enemySide = War.GetOtherSide(side);
            List<ulong> enemyIds = [..war.sideIds[enemySide]];

            switch (war.warType)
            {
                case WarType.CONQUEST:
                    foreach (ulong enemyId in enemyIds)
                    {
                        State enemyState = objectManager.GetState(enemyId);
                        State[] enemyVassals = [.. enemyState.vassals];

                        foreach (State enemyVassal in enemyVassals)
                        {
                            enemyVassal.GetOccupier()?.AddVassal(enemyVassal, Sovereignty.PUPPET);
                        }

                        if (enemyState.GetOccupier() != null && victor != null)
                        {
                            enemyState.RemoveAllVassals();
                            enemyState.GetOccupier().AddVassal(enemyState, Sovereignty.PUPPET);                            
                        }
                    }
                break;
                case WarType.CIVIL_WAR:
                    
                    if (side == War.WarSide.AGRESSOR)
                    {
                        // Rebel Perspective
                        foreach (ulong enemyId in enemyIds)
                        {
                            State enemyState = objectManager.GetState(enemyId);
                            enemyState.RemoveAllVassals(); 
                        } 
                    } else
                    {
                        // Government Perspective
                        foreach (ulong enemyId in enemyIds)
                        {
                            State enemyState = objectManager.GetState(enemyId);
                            if (enemyState.sovereignty == Sovereignty.REBELLIOUS) enemyState.sovereignty = Sovereignty.PROVINCE;
                        } 
                        state.stability += 0.3f;
                    }                                        
                break;
            }            
        }
        objectManager.EndWar(war);
    }
    public void TickDiplomacy()
    {
        foreach (var pair in state.relations)
        {
            State target = pair.Key;
            DiplomaticRelations relations = pair.Value;
            Character leader = state.leader;
            if (target == null || relations == null || leader == null || target.sovereignty != Sovereignty.INDEPENDENT) continue;

            if (relations.opinion > 0 && !state.IsEnemyWithState(target))
            {
                // Positive
                float goodwill = relations.opinion;

                Alliance ourAlliance = state.GetAllianceOfType(AllianceType.ALLIANCE);
                Alliance otherAlliance = target.GetAllianceOfType(AllianceType.ALLIANCE);

                if (rng.NextSingle() < goodwill * allyChanceMultiplier)
                {
                    if (ourAlliance == null && otherAlliance == null)
                    {
                        Alliance newAlliance = objectManager.CreateAlliance(state, AllianceType.ALLIANCE);
                        newAlliance.AddMember(target);
                    } 
                    else if (ourAlliance == null)
                    {
                        TryJoinAlliance(otherAlliance);
                    }
                }
            } else
            {
                // Negative
                float animosity = Math.Abs(relations.opinion);

                if (state.CanFightState(target))
                {
                    // Wars
                    float confidence = threatConfidenceCurve.Sample(relations.opinion);

                    if (rng.NextSingle() < animosity * confidence * warChanceMultiplier)
                    {
                        state.DeclareWar(target);
                    }                     
                } else
                {
                    // Other Forms of Agression
                    if (state.IsAlliedToState(target) && rng.NextSingle() < animosity * warChanceMultiplier)
                    {
                        state.GetAllianceOfType(AllianceType.ALLIANCE)?.RemoveMember(state);
                    }
                }
            }            
        }   
    }
    public void TryJoinAlliance(Alliance alliance)
    {
        foreach (State member in alliance.memberStates)
        {
            if (state.HasRelations(member))
            {
                float opinion = Mathf.InverseLerp(state.relations[member].opinion, -1, 1);
                if (rng.NextSingle() > opinion + 0.2f)
                {
                    return;
                }
            }
        }
        alliance.AddMember(state);
    }
    public void TickRelations(DiplomaticRelations relations)
    {
        State target = state == relations.initiator ? relations.recipient : relations.initiator;
        Character leader = state.leader;
        float diplomacyScore = rng.NextSingle();
        float positiveChance = -1;

        if (state.GetLiege() == target)
        {
            // Vassal -> Liege
            positiveChance = leader.GetPersonalityLevel("ambition") switch
            {
                TraitLevel.HIGH => 0.3f,
                TraitLevel.MEDIUM => 0.5f,
                TraitLevel.LOW => 0.7f,
                _ => 1f,
            };
        } 
        else if (target.GetLiege() != state)
        {
            // Anything Else that isnt Liege -> Vassal
            positiveChance = leader.GetPersonalityLevel("agression") switch
            {
                TraitLevel.HIGH => 0.25f,
                TraitLevel.MEDIUM => 0.5f,
                TraitLevel.LOW => 0.75f,
                _ => 1f,
            };            
        }
        if (positiveChance == -1) return;

        if (diplomacyScore < positiveChance) 
            relations.ChangeOpinion(0.1f);
        else 
            relations.ChangeOpinion(-0.1f);
    }
}
