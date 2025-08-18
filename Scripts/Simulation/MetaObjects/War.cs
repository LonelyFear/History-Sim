using System.Collections.Generic;
using System.Linq;

public class War
{
    public List<State> attackers;
    public List<State> defenders;
    public WarType warType = WarType.CONQUEST;
    public uint tickStarted;
    public uint age;
    public uint tickEnded;

    public War(List<State> atk, List<State> def, WarType warType)
    {
        attackers = atk;
        defenders = def;
        this.warType = warType;

        foreach (State state in attackers)
        {
            state.wars.Add(this, true);
        }
        foreach (State state in defenders)
        {
            state.wars.Add(this, false);
        }
    }
    public void RemoveParticipants(List<State> states)
    {
        foreach (State state in states)
        {
            state.wars.Remove(this);
        }
        if (states.All(attackers.Contains))
        {
            // States are attackers
            states.All(attackers.Remove);
        }
        else
        {
            // States are defenders
            states.All(defenders.Remove);
        }
        if (attackers.Count < 1 || defenders.Count < 1)
        {
            EndWar();
        }
    }
    public void EndWar()
    {
        foreach (State state in attackers.Concat(defenders))
        {
            state.wars.Remove(this);
        }      
    }
}