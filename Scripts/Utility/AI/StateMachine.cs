using System;
using System.Collections.Generic;
using MessagePack;

namespace StateMachines
{
    public abstract class StateMachine<EState> where EState : Enum
    {
        [Key(2000)] BaseState<EState> currentState;
        [Key(2001)] Dictionary<EState, BaseState<EState>> states = new Dictionary<EState, BaseState<EState>>();
        public void Tick()
        {
            EState nextState = currentState.GetNextState();
            if (nextState.Equals(currentState.key))
            {
                currentState.Tick();
            } else
            {
                currentState.Exit();
                currentState = states[nextState];
                currentState.Enter();
            }
        }

        public void AddState(EState key, BaseState<EState> state)
        {
            states[key] = state;
        }
    }  

    public abstract class BaseState<EState> where EState : Enum 
    {
        public BaseState(EState key)
        {
            this.key = key;
        }
        [IgnoreMember] public EState key {get; private set;}
        [IgnoreMember] public static ObjectManager objectManager;
        [IgnoreMember] public static SimManager simManager;
        [IgnoreMember] public static Random rng = new Random();
        
        public abstract void Enter();
        public abstract void Tick();
        public abstract void Exit();
        public abstract EState GetNextState();
    }
}
