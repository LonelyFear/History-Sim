using System;
using System.Collections.Generic;
using Godot;
using Microsoft.Win32.SafeHandles;
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

        if (!state.conflicts.Contains(this)){
             state.conflicts.Add(this);
        }
       
        switch (side){
            case Side.AGRESSOR:
                if (primaryAgressor == null){
                    primaryAgressor = state;
                }
                agressors.Add(state);

                // Establishes Conflict Between States
                foreach (State defender in defenders){
                    state.EstablishRelations(defender);
                    state.relations[defender].conflict = this;
                }
                
                break;
            case Side.DEFENDER:
                if (primaryDefender == null){
                    primaryDefender = state;
                }
                defenders.Add(state);

                // Establishes Conflict Between States
                foreach (State agressor in agressors){
                    state.EstablishRelations(agressor);
                    state.relations[agressor].conflict = this;
                }
                break;
        }
    }
    public void RemoveParticipant(State state){

        if (state.conflicts.Contains(this)){
             state.conflicts.Remove(this);
        }

        if (agressors.Contains(state)){
            agressors.Remove(state);

            // Ends conflict between 2 states
            foreach (State defender in defenders){
                state.relations[defender].conflict = null;
            }
        } else if (defenders.Contains(state)){
            defenders.Remove(state);

            // Ends conflict between 2 states
            foreach (State agressor in agressors){
                state.relations[agressor].conflict = null;
            }
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


