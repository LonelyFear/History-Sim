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
    }  

    public float NormalizeNegative(float value) {return (value - 50) / 50f;}
    public float Normalize(float value) {return value / 100f;}

    /*
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
    */
}
