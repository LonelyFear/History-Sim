using Godot;
using MessagePack;
[Union(0, typeof(NewLeaderEvent))]
[Union(1, typeof(CharacterDeathEvent))]
public abstract class HistoricalEvent
{
    [IgnoreMember] public static TimeManager timeManager;
    [Key(0)] public uint tickOccured {get; set;} = timeManager.ticks;
    [Key(1)] public string text {get; set;}
}