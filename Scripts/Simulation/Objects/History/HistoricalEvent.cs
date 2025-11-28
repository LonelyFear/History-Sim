using System.Collections.Generic;
using Godot;
using MessagePack;
[MessagePackObject]
public class HistoricalEvent
{
    [IgnoreMember] public static TimeManager timeManager;
    //[IgnoreMember] public static ObjectManager objectManager;
    [Key(401)] public ulong id;
    [Key(402)] public EventType type;
    [Key(403)] public uint tickOccured;
    [Key(404)] public List<string> objIds = new List<string>();
    [Key(405)] public string eventText;
    public string GetEventText()
    {
        string text = $"{timeManager.GetStringDate(tickOccured, true)}: ";
        switch (type)
        {
            case EventType.SUCCESSION:
                if (objIds.Count < 2)
                {
                    GD.PushError("Succession events require 2 objects, one state and one character");
                    return text;
                }
                State state = (State)NamedObject.GetNamedObject(objIds[0]);
                Character newLeader = (Character)NamedObject.GetNamedObject(objIds[1]);
                if (newLeader != null)
                {
                    GD.Print(newLeader);
                    text += $"{NamedObject.GenerateUrlText(newLeader, newLeader.name)} became the new {state.leaderTitle} of the {NamedObject.GenerateUrlText(state, state.name)}";
                } else
                {
                    text += $"A forgotten leader became the {state.leaderTitle} of the {NamedObject.GenerateUrlText(state, state.name)}";
                }
                
                break;
        }
        return text;
    }
    /*
    public string GenerateEventText()
    {
        string text = $"{timeManager.GetStringDate(tickOccured, true)}: ";
        return text;
    }
    */
}
public enum EventType
{
    SUCCESSION,
}