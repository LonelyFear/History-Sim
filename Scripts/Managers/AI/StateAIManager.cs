using System.IO.Compression;
using System.Linq.Expressions;
using Godot;
using MessagePack;

[MessagePackObject(AllowPrivate = true)]
public partial class StateAIManager : AIBase
{
    [Key(0)] public ulong stateId;
    [IgnoreMember] State state;
    [IgnoreMember] StateDiplomacyManager diplomacyManager;
    [IgnoreMember] StateVassalManager vassalManager;
    [Key(1)] StateWeights weights;
    [Key(2)] int aiTicks = 0;

    // Constants
    [IgnoreMember] const int ticksBetweenTickRecalc = 4;
    [IgnoreMember] const float warChanceMultiplier = 0.01f;

    public StateAIManager () {}
    public StateAIManager (State sta)
    {
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
    public void TickAI()
    {
        aiTicks++;
        if (Mathf.PosMod(aiTicks, ticksBetweenTickRecalc) == 0)
        {
            RecalculateWeights();
        }
        DeclareWars();
    }  
    public void RecalculateWeights()
    {
        StateWeights newWeights = weights;
        State state = objectManager.GetState(stateId);
        Character leader = objectManager.GetCharacter(state.leaderId);
        Culture rulingCulture = state.rulingPop.culture;

        // Calculates weights
        newWeights.Agression = 1;
        newWeights.Expansion = 1;

        // Averages "Personality Factor". Lerps between base trait and personality factor so leaders have effect
        if (leader != null)
        {
            float personalityFactor = Utility.CalcWeightedAverage([
            (1 - Normalize(leader.personality["temperment"]), 5), 
            (Normalize(leader.personality["greed"]), 2),
            (NormalizeNegative(leader.personality["boldness"]), 1)]);
            
            newWeights.Agression = Mathf.Lerp(newWeights.Agression, personalityFactor, 0.6f);

            personalityFactor = Utility.CalcWeightedAverage([
            (Normalize(leader.personality["ambition"]), 6), 
            (Normalize(leader.personality["greed"]), 5),
            (NormalizeNegative(leader.personality["boldness"]), 2)]);
            newWeights.Expansion = Mathf.Lerp(newWeights.Expansion, personalityFactor, 0.6f);
        }

        weights = newWeights;
    }

    public float NormalizeNegative(float value) {return (value - 50) / 50f;}
    public float Normalize(float value) {return value / 100f;}

    public void DeclareWars()
    {
        // Wars of Expansion
        // Calculates chance of initiating war based on agression and expansion
        // Cant declare expansion wars if not independent (Will be change later)
        if (rng.NextSingle() < weights.Expansion * weights.Agression && state.vassalManager.sovereignty == Sovereignty.INDEPENDENT)
        {
            foreach (var pair in state.borderingStateIds)
            {
                int borderLength = pair.Value;
                State potentialTarget = objectManager.GetState(pair.Key);

                // Cant declare expansion wars on states in mutual alliances and realms
                // Cant declare expansion wars on vassals
                if (diplomacyManager.GetRelationsWithState(potentialTarget) == null || diplomacyManager.IsAlliedToState(potentialTarget) || potentialTarget.vassalManager.sovereignty != Sovereignty.INDEPENDENT || potentialTarget == state)
                {
                    continue;
                }

                // Now we have potential war target
                // War chance always starts out as 1 and is reduced (multiplied by base war chance to prevent monthly wars ofc)
                float warChance = 1;
                warChance += Normalize(diplomacyManager.GetRelationsWithState(potentialTarget).threat);
                // Over 100% war chances are fine
                // We dont want negative chances for wars
                warChance = Mathf.Max(warChance, 0);

                if (rng.NextSingle() < warChance * warChanceMultiplier)
                {
                    // WAR!!!
                    // TODO
                }
            }
        }
    }
}

[MessagePackObject(AllowPrivate = true)]
internal struct StateWeights
{
    [Key(0)] public float Agression;
    [Key(1)] public float Expansion;
    [Key(2)] public float Development;
    [Key(3)] public float Cooperation;
    [Key(4)] public float Defense;
}