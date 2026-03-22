using System;
using MessagePack;

namespace  UtilityAi
{
    public abstract class AiAgent
    {
        [IgnoreMember] protected IAction currentAction = null;
        [IgnoreMember] protected IAction[] actions;
        [IgnoreMember] public Random rng = new Random();
        public AiAgent() {}
        public AiAgent(IAction[] aiActions, int rngSeed = int.MinValue)
        {
            actions = aiActions;

            if (rngSeed == int.MinValue)
            {
                rng = new Random();
            } else
            {
                rng = new Random(rngSeed);
            }

        }

        public IAction GetBestAction()
        {
            IAction bestAction = null;
            float highestUtility = -100;
            foreach (IAction action in actions)
            {
                if (action.CalcUtility(this) > highestUtility)
                {
                    bestAction = action;
                }
            }

            return bestAction;
        }
    }

    public interface IAction
    {
        public float CalcUtility(AiAgent agent);
        public void PerformAction(AiAgent agent);
    }    
}
