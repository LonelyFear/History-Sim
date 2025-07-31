using System.Collections.Generic;
using System.Reflection.PortableExecutable;

public class War
{
    public List<State> agressors = new List<State>();
    public List<State> defenders = new List<State>();
    public ulong start;
    public ulong age;
    public ulong end;

    public void AddParticipant(State state, bool attacker)
    {
        if (state.sovereignty == Sovereignty.INDEPENDENT)
        {
            if (!agressors.Contains(state) && !defenders.Contains(state))
            {
                if (attacker)
                {
                    agressors.Add(state);
                }
                else
                {
                    defenders.Add(state);
                }
                //state.wars.Add(this);
                foreach (State vassal in state.vassals)
                {
                    if (!agressors.Contains(vassal) && !defenders.Contains(vassal))
                    {
                        if (attacker)
                        {
                            agressors.Add(state);
                        }
                        else
                        {
                            defenders.Add(state);
                        }
                        //vassal.wars.Add(this);
                    }
                }
            }
        }
    }
    public void RemoveParticipant(State state)
    {
        if (state.sovereignty == Sovereignty.INDEPENDENT)
        {
            if (agressors.Contains(state) || defenders.Contains(state))
            {
                if (agressors.Contains(state))
                {
                    agressors.Remove(state);
                }
                else
                {
                    defenders.Remove(state);
                }     
                //state.wars.Remove(this);  
                foreach (State vassal in state.vassals)
                {
                    if (agressors.Contains(vassal) || defenders.Contains(vassal))
                    {
                        if (agressors.Contains(state))
                        {
                            agressors.Remove(state);
                        }
                        else
                        {
                            defenders.Remove(state);
                        }
                        //vassal.wars.Remove(this);
                    }
                }
            }
        }
    }
}