using System.IO.Compression;
using System.Linq.Expressions;
using Godot;

public class StateAIManager : AIBase
{
    public ulong stateId;
    StateDiplomacyManager diplomacyManager;
    StateDiplomacyManager vassalManager;
    StateWeights weights;
    public void RecalculateWeights()
    {
        StateWeights newWeights = weights;
        State state = objectManager.GetState(stateId);
        Character leader = objectManager.GetCharacter(state.leaderId);
        Culture rulingCulture = state.rulingPop.culture;

        // Calculates weights
        newWeights.Agression = 1;

        // Averages "Personality Factor". Lerps between base trait and personality factor so leaders have effect
        if (leader != null)
        {
            float personalityFactor = Utility.CalcWeightedAverage([
            (1 - Normalize(leader.personality["temperment"]), 5), 
            (Normalize(leader.personality["greed"]), 2),
            (NormalizeNegative(leader.personality["boldness"]), 1)]);
            
            newWeights.Agression = Mathf.Lerp(newWeights.Agression, personalityFactor, 0.6f);
        }

        weights = newWeights;
    }
    public float NormalizeNegative(int value) {return (value - 50) / 50f;}
    public float Normalize(int value) {return value / 100f;}
}
internal struct StateWeights
{
    public float Agression;
    public float Expansion;
    public float Development;
    public float Cooperation;
    public float Defense;
}