using System;
using Godot;
using MessagePack;

[MessagePackObject]
public class Relation
{
    // Constants
    [IgnoreMember] public const int minOpinion = -100;
    [IgnoreMember] public const int maxOpinion = 100;  

    [Key(0)] public int opinion = 0;
    [Key(1)] public bool rival = false;
    [Key(2)] public bool enemy = false;

    public Relation() { }
    public Relation(int opinion = 0, bool rival = false, bool enemy = false)
    {
        SetOpinion(opinion);
        this.rival = rival;
        this.enemy = enemy;
    }
    public void ChangeOpinion(int amount)
    {
        opinion = Mathf.Clamp(opinion + amount, minOpinion, maxOpinion);
    }
    public void SetOpinion(int amount)
    {
        opinion = Mathf.Clamp(amount, minOpinion, maxOpinion);
    }
}
public enum Vassalage
{
    LIEGE,
    INDEPENDENT,
    VASSAL
}