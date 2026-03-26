using System;
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
    [IgnoreMember] public StateVassalManager vassalManager;
    [Key(2)] int ticks = 0;

    // Constants
    [IgnoreMember] const int ticksBetweenTickRecalc = 4;
    [IgnoreMember] const float warChanceMultiplier = 0.01f;

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
        vassalManager = state.vassalManager;
    }  
    public float NormalizeNegative(float value) {return (value - 50) / 50f;}
    public float Normalize(float value) {return value / 100f;}

    public void Tick()
    {
        ticks++;
        if (Mathf.PosMod(ticks, ticksBetweenTickRecalc) == 0)
        {
            TickChangeRelations();
        }
    }
    public void TickChangeRelations()
    {
        foreach (State border in state.independentBorderIds.Select(pair => objectManager.GetState(pair.Key)))
        {
            Relation relations = diplomacyManager.GetRelationsWithState(border);
            Character leader = objectManager.GetCharacter(state.leaderId);
            if (border == null || relations == null || leader == null) continue;
            
            Character otherLeader = objectManager.GetCharacter(border.leaderId);
            if (otherLeader == null) continue;

            float positiveChance = 0f;
            float neutralChance = 1f;

            float diplomacyScore = rng.NextSingle();

            // Agressive leader vs Agressive leader
            TraitLevel otherLeaderAgression = otherLeader.GetPersonalityLevel("agression");

            switch (leader.GetPersonalityLevel("agression"))
            {
                case TraitLevel.HIGH:
                    positiveChance = 0.1f;
                    neutralChance = 0.4f;
                    // agressiveChance = 0.5                
                    if (otherLeaderAgression == TraitLevel.HIGH)
                    {
                        positiveChance = 0.05f;
                        neutralChance = 0.35f;   
                        // agressiveChance = 0.6                     
                    }
                    break;
                case TraitLevel.MEDIUM:
                    positiveChance = 0.2f;
                    neutralChance = 0.4f;
                    // agressiveChance = 0.4
                    break;
                case TraitLevel.LOW:
                    positiveChance = 0.3f;
                    neutralChance = 0.5f;
                    // agressiveChance = 0.2
                    if (otherLeaderAgression == TraitLevel.LOW)
                    {
                        positiveChance = 0.4f;
                        neutralChance = 0.5f;   
                        // agressiveChance = 0.1                     
                    }
                    break;
            }

            if (diplomacyScore < positiveChance)
            {
                // Positive outcome
                if (!relations.rival)
                {
                    relations.ChangeOpinion(0.1f);
                } else
                {
                    if (rng.NextSingle() < 0.1f)
                    {
                        // Ends rivalry
                        relations.rival = false;
                        border.diplomacy.GetRelationsWithState(state).rival = false;
                    };
                }                
            } else if (diplomacyScore < neutralChance + positiveChance)
            {
                // Neutral outcome
                relations.ChangeOpinion(0f);
            } else
            {
                // Negative outcome
                relations.ChangeOpinion(-0.1f);
            }
        }
    }
}
