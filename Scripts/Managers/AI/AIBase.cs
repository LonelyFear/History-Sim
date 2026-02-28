using System;
using System.Runtime.Serialization;
using MessagePack;

public abstract class AIBase
{
    [IgnoreMember] public static ObjectManager objectManager;
    [IgnoreMember] public static SimManager simManager;
    [IgnoreMember] public static Random rng = new Random();
}