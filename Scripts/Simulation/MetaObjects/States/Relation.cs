using System;
using Godot;
using MessagePack;

[MessagePackObject]
public class Relation
{
    [Key(0)] public float opinion = 0f;
    [Key(1)] public float threat = 0f;
    [Key(2)] public uint truce = 0;
    [Key(3)] public bool rival = false;
    //[Key(4)] public bool enemy = false;
    public Relation() { }
}