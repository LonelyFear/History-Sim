using System;
using Godot;
using MessagePack;

[MessagePackObject]
public class Relation
{
    // Constants
    [IgnoreMember] public const float minOpinion = 0;
    [IgnoreMember] public const float maxOpinion = 1;  

    [Key(0)] public float opinion = 0;
    [Key(10)] public float threat = 0;
    [Key(1)] public bool rival = false;
    [Key(2)] public bool enemy = false;
    [Key(3)] public int borderLength = 0;
    [Key(4)] public bool borders = false;

    public Relation() { }
    public Relation(float opinion = 0.5f, bool rival = false, bool enemy = false)
    {
        SetOpinion(opinion);
        this.rival = rival;
        this.enemy = enemy;
        
    }
    public void ChangeOpinion(float amount)
    {
        opinion = Mathf.Clamp(opinion + amount, minOpinion, maxOpinion);
    }
    public void SetOpinion(float amount)
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