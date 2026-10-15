using System;
using Godot;
using MessagePack;

[MessagePackObject(AllowPrivate = true)]
public partial class Culture : PopObject
{
    public override void Die()
    {
        //GD.Print("Culture Dead");
        dead = true;
        tickDestroyed = simManager.timeManager.ticks;
        foreach (Pop pop in pops)
        {
            RemovePop(pop);
        }
    }

    public float GetSimilarity(Culture culture)
    {
        return (Math.Abs(color.R - culture.color.R) + Math.Abs(color.B - culture.color.B) + Math.Abs(color.G - culture.color.G))/3f;
    }
    public float GetSimilarity(Color otherColor)
    {
        return (Math.Abs(color.R - otherColor.R) + Math.Abs(color.B - otherColor.B) + Math.Abs(color.G - otherColor.G))/3f;
    }
}
