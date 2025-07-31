using Godot;
using System;

public class Relation
{
    public int opinion = 0;
    public bool rivalry = false;
    public uint truce = 0;
    public const int minOpinionValue = -5;
    public const int maxOpinionValue = 5;
    public void ChangeOpinion(int amount)
    {
        opinion += amount;
        opinion = Mathf.Clamp(opinion, minOpinionValue, maxOpinionValue);
    }
}