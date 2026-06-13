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
    [IgnoreMember] public static ObjectManager objectManager;
    [IgnoreMember] public static SimManager simManager;
    [Key(0)] public ulong? stateId { get; set; }  
    [Key(2)] int ticks { get; set; } = 0;

    // Constants
    [IgnoreMember] const int ticksBetweenTickRecalc = 4;
    [IgnoreMember] const float warChanceMultiplier = 0.05f;
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
            War.WarSide enemySide = War.GetOtherSide(side);

            State enemyWarLead = objectManager.GetState(war.warLeaderIds[enemySide]);
            DiplomaticRelations relations = state.relations[enemyWarLead];
           

            if (war.warLeaderIds[side] != state.id) continue;
            bool surrendered = state.capitualated || state.sovereignty != Sovereignty.INDEPENDENT;

            switch (war.warType)
            {
                case WarType.CONQUEST:
                    // Conquest Wars
                    if (surrendered || relations.opinion > 0 && rng.NextSingle() < 0.25f)
                    {
                        war.EndWar();
                        relations.truce = TimeManager.YearsToTicks(5);
                    }
                    break; 
                case WarType.CIVIL_WAR:
                    if (surrendered)
                    {
                        if (side == War.WarSide.AGRESSOR)
                        {
                            // Rebels Defeat
                            foreach (State rebel in war.sideIds[side].Select(id => objectManager.GetState(id)))
                            {
                                rebel.sovereignty = Sovereignty.PROVINCE;
                            }                            
                        } 
                        else
                        {
                            // Government Defeat
                            state.RemoveAllVassals();
                        }
                        war.EndWar();
                        relations.truce = TimeManager.YearsToTicks(5);
                    }
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
                    Alliance newAlliance = objectManager.CreateAlliance(target, AllianceType.ALLIANCE);
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
            if (rng.NextSingle() < warChanceMultiplier)
            {
                if (state.CanFightState(target))
                {
                    // Tries to go to war
                    objectManager.StartWar(WarType.CONQUEST, state, target);                  
                } 
                else if (state.IsAlliedToState(target) && rng.NextSingle() < warChanceMultiplier)
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
