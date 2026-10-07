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
    [IgnoreMember] private readonly object locker = new object();
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
                _state = ObjectManager.GetState(stateId);
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

    public bool CanTick()
    {
        return Mathf.PosMod(ticks, ticksBetweenTickRecalc) == 0;
    }
    public void Tick()
    {
        ticks++;
        if (CanTick())
        {
            if (state.sovereignty == Sovereignty.INDEPENDENT)
            {
                foreach (var pair in state.relations)
                {
                    UpdateDiplomacy(pair.Value);
                }   
            }  
            try
            {
                TickEndWars();
            } catch (Exception e)
            {
                GD.PushError(e);
            }
        }
    }
    public void TickEndWars()
    {
        foreach (var pair in state.wars)
        {
            War war = pair.Key;
            

            War.WarSide side = pair.Value;
            State enemyWarLead = side == War.WarSide.AGRESSOR ? war.defenderLeader : war.attackerLeader;
            DiplomaticRelations relations = (enemyWarLead == null || !state.relations.TryGetValue(enemyWarLead, out var value)) ? null : value;

            bool diplomaticEnd = relations != null && relations.opinion + (TimeManager.TicksToYears(war.GetAge())/50f) > 0 && rng.NextSingle() < 0.25f;
            if (diplomaticEnd && war.warType != WarType.CIVIL_WAR)
            {
                war.victor = War.WarSide.NEUTRAL;
            }

            if (war.victor == null || !war.IsStateWarLead(state)) continue;

            switch (war.warType)
            {
                case WarType.CONQUEST:
                    // Conquest Wars
                    bool whitePeace = diplomaticEnd && rng.NextSingle() < 0.1f;
                    // White Peace
                    if (whitePeace)
                    {
                        foreach (State participant in war.participants)
                        {
                            participant.RemoveOccupation();
                            participant.AddAllClaims();
                            participant.GetLastLiege()?.AddVassal(participant, Sovereignty.PUPPET);
                        }
                    }
                    // Otherwise the war ends in white peace
                    _ = new EndWarEvent(war, whitePeace);
                    war.EndWar();
                    if (relations != null) relations.truce = TimeManager.YearsToTicks(Defines.TruceLengthYears);
                    break; 
                case WarType.CIVIL_WAR:
                    if (war.victor == War.WarSide.AGRESSOR)
                    {
                        // Rebels Defeat
                        foreach (State rebel in war.attackers)
                        {
                            rebel.sovereignty = Sovereignty.PROVINCE;
                            rebel.ongoingRebellion = false;
                        }         
                        state.stability += Defines.CivilWarStabilityGain;                 
                    } 
                    else
                    {
                        // Government Defeat
                        state.RemoveAllVassals();
                        state.ongoingRebellion = false;
                    }
                    new EndWarEvent(war);  
                    war.EndWar();
                    if (relations != null) relations.truce = TimeManager.YearsToTicks(Defines.TruceLengthYears);
                    break;
            }

        }
    }
    public void UpdateDiplomacy(DiplomaticRelations relations)
    {
        State target = state == relations.initiator ? relations.recipient : relations.initiator;
        Character leader = state.leader;

        if (target == null || relations == null || leader == null || target.sovereignty != Sovereignty.INDEPENDENT || state.IsEnemyWithState(target)) return;

        if (relations.opinion > 0)
        {
            // Positive
            float goodwill = relations.opinion;

            if (rng.NextSingle() < goodwill * allyChanceMultiplier)
            {
                Alliance ourAlliance = state.GetAllianceOfType(AllianceType.ALLIANCE);
                if (ourAlliance == null && target.GetAllianceOfType(AllianceType.ALLIANCE) == null)
                {
                    Alliance newAlliance = ObjectManager.CreateAlliance(target, AllianceType.ALLIANCE);
                    newAlliance.AddMember(state);                        
                } 
                else if (ourAlliance == null)
                {
                    Alliance otherAlliance = target.GetAllianceOfType(AllianceType.ALLIANCE);
                    foreach (State member in otherAlliance.memberStates)
                    {
                        if (state.relations.TryGetValue(member, out relations) && relations.opinion < 0)
                        {
                            return;
                        }
                    }
                    otherAlliance.AddMember(state);                        
                }                    
            }
        } 
        else
        {
            // Agressive Diplomacy
            if (rng.NextSingle() < warChanceMultiplier)
            {
                if (state.CanFightState(target))
                {
                    // Wars
                    float warInitiateChance = state.GetCombatPower()/target.GetCombatPower();
                    warInitiateChance += leader.GetPersonalityLevel("expansionism") switch
                    {
                        TraitLevel.HIGH => 0.5f,
                        TraitLevel.MEDIUM => 0f,
                        TraitLevel.LOW => -0.5f,
                        _ => 0f,
                    };
                    if (rng.NextSingle() < warInitiateChance)
                    {
                        War war = ObjectManager.StartWar(WarType.CONQUEST, state, target);

                        _ = new DeclareWarEvent(state, target, war);
                        return;
                    }
                                   
                } 
                else if (state.IsAlliedToState(target))
                {
                    // Breaks alliance
                    state.GetAllianceOfType(AllianceType.ALLIANCE)?.RemoveMember(state);
                }                 
            }
        }             
    }
    public void UpdateRelations(DiplomaticRelations relations)
    {
        if (rng.NextSingle() > diploChangeChance) return;

        State target = state == relations.initiator ? relations.recipient : relations.initiator;
        Character leader = state.leader;
        if (leader == null) return;

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
