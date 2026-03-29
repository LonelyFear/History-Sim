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
    [Key(0)] public ulong stateId;
    [IgnoreMember] public State state;
    [IgnoreMember] public StateDiplomacyManager diplomacyManager;
    [Key(2)] int ticks = 0;
    [Key(3)] List<ulong> endedWarIds;

    // Constants
    [IgnoreMember] const int ticksBetweenTickRecalc = 4;
    [IgnoreMember] const float warChanceMultiplier = 0.01f;
    [IgnoreMember] const float diploChangeChance = 0.1f;

    // Curves
    [IgnoreMember] Curve threatConfidenceCurve = GD.Load<Curve>("res://Curves/Simulation/ThreatConfidenceCurve.tres");

    public StateAIManager () {}
    public StateAIManager (UtilityAi.IAction[] aiActions, State sta)
    {
        actions = aiActions;
        stateId = sta.id;
        state = sta;
        InitAI();
    }
    public void InitAI()
    {
        state = objectManager.GetState(stateId);
        diplomacyManager = state.diplomacy;
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

            // Surrender Via Capitulation
            if (state.capitualated && war.warLeaderIds[side] == state.id)
            {
                objectManager.GetState(war.warLeaderIds[enemySide]).AIManager.CalcWarVictory(war, enemySide);
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
                    //GD.Print(war.participantIds.Contains(enemyId));
                    enemyState.diplomacy.RemoveAllVassals();
                    state.diplomacy.AddVassal(enemyState, Sovereignty.PUPPET);
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
            Character leader = objectManager.GetCharacter(state.leaderId);
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
            Character leader = objectManager.GetCharacter(state.leaderId);
            if (target == null || relations == null || leader == null) continue;
            
            Character otherLeader = objectManager.GetCharacter(target.leaderId);
            if (otherLeader == null) continue;

            float positiveChance = 0f;
            float neutralChance = 1f;

            float diplomacyScore = rng.NextSingle();

            // Agressive leader vs Agressive leader
            TraitLevel otherLeaderAgression = otherLeader.GetPersonalityLevel("agression");

            switch (leader.GetPersonalityLevel("agression"))
            {
                case TraitLevel.HIGH:
                    positiveChance = 0.2f;
                    neutralChance = 0.4f;
                    // agressiveChance = 0.4               
                    break;
                case TraitLevel.MEDIUM:
                    positiveChance = 0.3f;
                    neutralChance = 0.4f;
                    // agressiveChance = 0.3
                    break;
                case TraitLevel.LOW:
                    positiveChance = 0.4f;
                    neutralChance = 0.4f;
                    // agressiveChance = 0.2
                    break;
            }

            if (diplomacyScore < positiveChance)
            {
                // Positive outcome
                if (!relations.rival)
                {
                    diplomacyManager.ChangeOpinion(target, 0.05f);
                } else
                {
                    if (rng.NextSingle() < 0.1f)
                    {
                        // Ends rivalry
                        diplomacyManager.SetRivalry(target, false);
                    };
                }                
            } else if (diplomacyScore < neutralChance + positiveChance)
            {
                // Neutral outcome
                // Just nothing :)
            } else
            {
                // Negative outcome
                diplomacyManager.ChangeOpinion(target, -0.05f);
            }
        }
    }
}
