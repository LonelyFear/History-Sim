using System;
using System.Collections.Generic;
public class Conflict
{
    public State primaryAgressor = null;
    public State primaryDefender = null;
    public List<State> defenders = new List<State>();
    public List<State> agressors = new List<State>();
}
