using System;
using Godot;
using System.Collections.Generic;
using MessagePack;
[MessagePackObject]
public class Culture : PopObject
{
    public override void Die()
    {
        dead = true;
        tickDestroyed = simManager.timeManager.ticks;
        foreach (Pop pop in pops)
        {
            RemovePop(pop, this);
        }
    }

    public static bool CheckCultureSimilarity(Culture a, Culture b){
        return a == b;
    }
}
