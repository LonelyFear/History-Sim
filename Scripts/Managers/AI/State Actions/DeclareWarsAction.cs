using System;
using System.Linq;
using Godot;
using UtilityAi;
public class DeclareWarsAction : IAction
{
    Curve agressionWeightCurve = GD.Load<Curve>("res://Curves/Simulation/AgressionWarWeightCurve.tres");
    Curve threatConfidenceCurve = GD.Load<Curve>("res://Curves/Simulation/ThreatConfidenceCurve.tres");
    public string GetID()
    {
        return "declare_aggro_wars";
    }
    public float CalcUtility(AiAgent agent)
    { 
        StateAIManager manager = (StateAIManager)agent;
        ObjectManager objectManager = StateAIManager.objectManager;
        State state = manager.state;

        Character leader = objectManager.GetCharacter(state.leaderId);

        // Consideration 1
        float leaderFactor = agressionWeightCurve.Sample(Math.Clamp(leader.personality["agression"] + Utility.RandomRange(-0.1f, 0.1f, manager.rng), 0, 1));
        
        // Consideration 2
        float relationsFactor = 0;
        int consideredStates = 0;
        foreach (State border in state.independentBorderIds.Select(pair => objectManager.GetState(pair)))
        {
            Relation relations = state.diplomacy.GetRelationsWithState(border);
            if (relations == null) continue;

            consideredStates++;
            relationsFactor += relations.threat;
        }

        relationsFactor = threatConfidenceCurve.Sample(relationsFactor / consideredStates);

        
        return leaderFactor * relationsFactor;
    }

    public void PerformAction(AiAgent agent)
    {
        StateAIManager manager = (StateAIManager)agent;
    }
}
