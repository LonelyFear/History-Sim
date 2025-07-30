using Godot;
using System;

public class Relation
{
    public int opinion = 0;
    public bool rivalry = false;

    public void ChangeOpinion(int amount)
    {
        opinion = Mathf.Clamp(opinion + amount, -3, 3);
    }
}