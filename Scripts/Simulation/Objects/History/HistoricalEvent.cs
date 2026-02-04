using System;
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
    [Key(406)] List<string> objText = new List<string>();
    public void InitEvent()
    {
        switch (type)
        {
            case EventType.SUCCESSION:
                objText.Add(NamedObject.GetNamedObject<State>(objIds[0]).name);    
                objText.Add(NamedObject.GetNamedObject<State>(objIds[0]).leaderTitle);     
                objText.Add(NamedObject.GetNamedObject<Character>(objIds[1]).name);
                break;
            case EventType.LEADER_DEATH:
                objText.Add(NamedObject.GetNamedObject<State>(objIds[0]).name);    
                objText.Add(NamedObject.GetNamedObject<State>(objIds[0]).leaderTitle);     
                objText.Add(NamedObject.GetNamedObject<Character>(objIds[1]).name);
                break;
            case EventType.WAR_DECLARATION:
                objText.Add(NamedObject.GetNamedObject<State>(objIds[0]).name);
                objText.Add(NamedObject.GetNamedObject<State>(objIds[1]).name);               
                break;
            case EventType.WAR_END:
                objText.Add(NamedObject.GetNamedObject<State>(objIds[0]).name);
                objText.Add(NamedObject.GetNamedObject<State>(objIds[1]).name);               
                break;
            case EventType.DEATH:
                objText.Add(NamedObject.GetNamedObject<NamedObject>(objIds[0]).name);
                objText.Add(timeManager.GetYear(NamedObject.GetNamedObject<NamedObject>(objIds[0]).GetAge()).ToString());
                break;
        }
    }
    public string GetEventText()
    {
        string text = $"{timeManager.GetStringDate(tickOccured, true)}: ";
        switch (type)
        {
            case EventType.SUCCESSION:
                /*
                Key:
                0 - Character Name
                1 - Title
                2 - Nation
                */
                text += $"{NamedObject.GenerateUrlText(NamedObject.GetNamedObject(objIds[1]), objText[2])} became the new {objText[1]} of the {NamedObject.GenerateUrlText(NamedObject.GetNamedObject(objIds[0]), objText[0])}";
                break;
            case EventType.WAR_DECLARATION:
                /*
                Key:
                0 - Nation 1
                1 - Nation 2
                */
                text += $"{NamedObject.GenerateUrlText(NamedObject.GetNamedObject(objIds[0]), objText[0])} declared war on {NamedObject.GenerateUrlText(NamedObject.GetNamedObject(objIds[1]), objText[1])}.";
                break;
            case EventType.WAR_END:
                /*
                Key:
                0 - Nation 1
                1 - Nation 2
                */
                text += $"The war between {NamedObject.GenerateUrlText(NamedObject.GetNamedObject(objIds[0]), objText[0])} and {NamedObject.GenerateUrlText(NamedObject.GetNamedObject(objIds[1]), objText[1])} ended.";
                break;
            case EventType.DEATH:
                /*
                Key:
                0 - Name
                1 - Age (in years)
                */
                switch (NamedObject.GetNamedObject<NamedObject>(objIds[0]))
                {
                    case Character:
                        text += $"{NamedObject.GenerateUrlText(NamedObject.GetNamedObject<Character>(objIds[0]), objText[0])} died at {objText[1]} years old.";
                        break;
                    default:
                        text += $"{NamedObject.GenerateUrlText(NamedObject.GetNamedObject(objIds[0]), objText[0])} was dissolved.";
                        break;
                }
                break;           
        }
        return text;
    }
}
public enum EventType
{
    SUCCESSION,
    WAR_DECLARATION,
    WAR_END,
    DEATH,
    LEADER_DEATH
}