using System;
using System.Linq;
using Godot;
using UtilityAi;
public class DeclareWarsAction : IAction
{
    
    public float CalcUtility(AiAgent agent)
    {
        
        StateAIManager manager = (StateAIManager)agent;
        ObjectManager objectManager = StateAIManager.objectManager;
        State state = manager.state;
        Character leader = objectManager.GetCharacter(state.leaderId);

        float leaderFactor = Math.Clamp(leader.personality["agression"] + Utility.RandomRange(-0.1f, 0.1f, manager.rng), 0, 1);

        float relationsFactor = 0;
        foreach (State border in state.independentBorderIds.Select(pair => objectManager.GetState(pair.Key)))
        {
            relationsFactor += state.diplomacy.GetRelationsWithState(border).opinion;
        }
        relationsFactor /= state.independentBorderIds.Count;

        return leaderFactor * relationsFactor;
    }

    public void PerformAction(AiAgent agent)
    {
        StateAIManager manager = (StateAIManager)agent;
    }
}
