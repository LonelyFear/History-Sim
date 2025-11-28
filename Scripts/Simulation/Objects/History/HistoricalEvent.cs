using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;
[MessagePackObject(AllowPrivate = true)]
public partial class HistoricalEvent
{
    [IgnoreMember] public static TimeManager timeManager;
    //[IgnoreMember] public static ObjectManager objectManager;
    [Key(401)] public ulong id;
    [Key(402)] public EventType type;
    [Key(403)] public uint tickOccured;
    [Key(404)] public List<string> objIds = new List<string>();
    [Key(405)] public string eventText;
    [Key(406)] List<NamedObject> objects = new List<NamedObject>();
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
                State state = (State)objects[0];
                Character newLeader = (Character)objects[1];
                if (newLeader != null)
                {
                    text += $"{NamedObject.GenerateUrlText(newLeader, newLeader.name)} became the new {state.leaderTitle} of the {NamedObject.GenerateUrlText(state, state.name)}";
                } else
                {
                    text += $"A forgotten leader became the {state.leaderTitle} of the {NamedObject.GenerateUrlText(state, state.name)}";
                }
                break;
            case EventType.WAR_DECLARATION:
                State attacker = (State)objects[0];
                State defender = (State)objects[1];
                text += $"{NamedObject.GenerateUrlText(attacker, attacker.name)} declared war on {NamedObject.GenerateUrlText(defender, defender.name)}.";
                break;
            case EventType.WAR_END:
                attacker = (State)objects[0];
                defender = (State)objects[1];
                text += $"The war between {NamedObject.GenerateUrlText(attacker, attacker.name)} and {NamedObject.GenerateUrlText(defender, defender.name)} ended.";
                break;
            case EventType.DEATH:
                switch (objects[0])
                {
                    case Character:
                        text += $"{NamedObject.GenerateUrlText(objects[0], objects[0].name)} and died at {timeManager.GetYear(objects[0].GetAge())} years old.";
                        break;
                    default:
                        text += $"{NamedObject.GenerateUrlText(objects[0], objects[0].name)} was dissolved.";
                        break;
                }
                break;
        }
        return text;
    }
    public void CloneObjects()
    {
        objects = [.. objIds.Select(id => NamedObject.GetNamedObject(id).Clone())];
        
        foreach (NamedObject obj in objects)
        {
            switch (obj)
            {
                case State:
                    State state = (State)obj;
                    state.borderingStates = null;
                    state.regions = null;
                    break;                
                case PopObject:
                    PopObject popObject = (PopObject)obj;
                    popObject.pops = null;
                    popObject.cultureIds = null;
                    break;
            }
        }
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
    WAR_DECLARATION,
    WAR_END,
    DEATH
}