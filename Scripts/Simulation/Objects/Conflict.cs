using System;
using System.Collections.Generic;
using Godot;
public class Conflict
{
    public State primaryAgressor = null;
    public State primaryDefender = null;
    public List<State> defenders = new List<State>();
    public List<State> agressors = new List<State>();
    public int age = 0;
    public Escalation escalation;
    public Type type;
    public War war;
    public SimManager simManager;

    public void AddParticipant(State state, Side side){
        if (defenders.Contains(state) || agressors.Contains(state)){
            return;
        }
        switch (side){
            case Side.AGRESSOR:
                if (primaryAgressor == null){
                    primaryAgressor = state;
                }
                agressors.Add(state);
                break;
            case Side.DEFENDER:
                if (primaryDefender == null){
                    primaryDefender = state;
                }
                defenders.Add(state);
                break;
        }
    }
    public void RemoveParticipant(State state){
        if (agressors.Contains(state)){
            agressors.Remove(state);
        } else if (defenders.Contains(state)){
            defenders.Remove(state);
        }
    }

    public void EscalateConflict(){
        if (escalation != Escalation.COMBAT){
            escalation += 1;
        }
        if (escalation == Escalation.COMBAT && war == null){

        }

    }
    public enum Side{
        AGRESSOR,
        DEFENDER,
        MEDIATOR
    }
    public enum Type{
        ECONOMIC,
        CONQUEST,
        REBELLION
    }

    public enum Escalation{
        INITIAL = -1,
        THREATS = -5,
        PREPARATION = -10,
        COMBAT = -100,
    }
}   


