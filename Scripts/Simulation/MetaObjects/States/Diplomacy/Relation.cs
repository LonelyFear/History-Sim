using System;
using Godot;
using MessagePack;

[MessagePackObject]
public class Relation
{
    [Key(0)] public float opinion = 0f;
    [Key(10)] public float threat = 0f;
    [Key(11)] public uint truce = 0;
    [Key(1)] public bool rival = false;
    [Key(2)] public bool enemy = false;
    [Key(3)] public int borderLength = 0;
    [Key(4)] public bool borders = false;
    public Relation() { }
}